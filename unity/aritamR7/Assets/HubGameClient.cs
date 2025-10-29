using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Hub からの入力をゲーム内に橋渡しする常駐クライアントです。
/// </summary>
public class HubGameClient : MonoBehaviour
{
    // Hub と常時通信して各プレイヤーの入力を保持するための辞書です。
    private readonly ConcurrentDictionary<string, ControllerState> latestStates = new();

    // 接続先ハブの URL（環境変数で差し替え可能）です。
    [SerializeField] private string hubUrl = "ws://localhost:8765/ws";

    // 再接続間隔を Inspector から調整できるようにするための値です。
    [SerializeField] private float reconnectDelaySeconds = 3f;

    private static HubGameClient instance;
    private CancellationTokenSource loopToken;
    private Task loopTask;
    private readonly object taskLock = new();

    // ゲーム開始時に自動で常駐オブジェクトを生成するための初期化です。
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void EnsureInstance()
    {
        if (instance != null)
        {
            return;
        }

        var obj = new GameObject(nameof(HubGameClient));
        instance = obj.AddComponent<HubGameClient>();
        DontDestroyOnLoad(obj);
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        // 実行環境ごとに Hub の場所を変えられるよう環境変数を見に行きます。
        string envUrl = Environment.GetEnvironmentVariable("CGB_HUB_URL");
        if (!string.IsNullOrWhiteSpace(envUrl))
        {
            hubUrl = envUrl;
        }
    }

    private void OnEnable()
    {
        StartLoopIfNeeded();
    }

    private void Start()
    {
        StartLoopIfNeeded();
    }

    private void OnDisable()
    {
        StopLoop();
    }

    private void OnApplicationQuit()
    {
        StopLoop();
    }

    private void StartLoopIfNeeded()
    {
        lock (taskLock)
        {
            if (loopTask != null && !loopTask.IsCompleted)
            {
                return;
            }

            loopToken = new CancellationTokenSource();
            loopTask = ConnectionLoopAsync(loopToken.Token);
        }
    }

    private void StopLoop()
    {
        lock (taskLock)
        {
            if (loopToken == null)
            {
                return;
            }

            loopToken.Cancel();
        }

        try
        {
            loopTask?.Wait(TimeSpan.FromSeconds(1));
        }
        catch (AggregateException)
        {
            // キャンセル時の例外は無視します。
        }
        finally
        {
            lock (taskLock)
            {
                loopToken?.Dispose();
                loopToken = null;
                loopTask = null;
            }
        }
    }

    private async Task ConnectionLoopAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            using var socket = new ClientWebSocket();

            try
            {
                // Hub との接続を試み、成功したら game ロールを登録します。
                await socket.ConnectAsync(new Uri(hubUrl), token);
                await SendRegisterAsync(socket, token);

                Debug.Log("[HubGameClient] connected to hub");
                await ReceiveLoopAsync(socket, token);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[HubGameClient] connection error: {ex.Message}");
            }

            if (token.IsCancellationRequested)
            {
                break;
            }

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(reconnectDelaySeconds), token);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private static async Task SendRegisterAsync(ClientWebSocket socket, CancellationToken token)
    {
        byte[] payload = Encoding.UTF8.GetBytes("{\"role\":\"game\"}");
        await socket.SendAsync(new ArraySegment<byte>(payload), WebSocketMessageType.Text, true, token);
    }

    private async Task ReceiveLoopAsync(ClientWebSocket socket, CancellationToken token)
    {
        byte[] buffer = new byte[4096];
        using MemoryStream stream = new();

        while (!token.IsCancellationRequested && socket.State == WebSocketState.Open)
        {
            WebSocketReceiveResult result;

            try
            {
                result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), token);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            if (result.MessageType == WebSocketMessageType.Close)
            {
                await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "closing", CancellationToken.None);
                break;
            }

            stream.Write(buffer, 0, result.Count);

            if (!result.EndOfMessage)
            {
                continue;
            }

            string message = Encoding.UTF8.GetString(stream.ToArray());
            stream.SetLength(0);

            ProcessMessage(message);
        }
    }

    private void ProcessMessage(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return;
        }

        try
        {
            ControllerPayload payload = JsonUtility.FromJson<ControllerPayload>(json);
            if (payload == null || string.IsNullOrWhiteSpace(payload.id))
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(payload.type) && payload.type != "state")
            {
                return;
            }

            ControllerPayload.Axes axes = payload.axes ?? new ControllerPayload.Axes();
            ControllerPayload.Buttons btn = payload.btn ?? new ControllerPayload.Buttons();

            var state = new ControllerState
            {
                Axes = new Vector2(axes.x, axes.y),
                ButtonA = btn.a,
                Timestamp = payload.t
            };

            latestStates[payload.id] = state;
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[HubGameClient] failed to parse message: {ex.Message}");
        }
    }

    // target スクリプトから入力状態を取得できるようにするためのアクセサです。
    public static bool TryGetState(string controllerId, out ControllerState state)
    {
        state = default;

        if (instance == null || string.IsNullOrWhiteSpace(controllerId))
        {
            return false;
        }

        return instance.latestStates.TryGetValue(controllerId, out state);
    }

    [Serializable]
    private class ControllerPayload
    {
        public string type;
        public string id;
        public Axes axes;
        public Buttons btn;
        public long t;

        [Serializable]
        public class Axes
        {
            public float x;
            public float y;
        }

        [Serializable]
        public class Buttons
        {
            public bool a;
        }
    }

    public struct ControllerState
    {
        public Vector2 Axes;
        public bool ButtonA;
        public long Timestamp;
    }
}
