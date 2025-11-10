using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class HubGameClient : MonoBehaviour
{
    private readonly ConcurrentDictionary<string, ControllerState> latestStates = new ConcurrentDictionary<string, ControllerState>();

    [SerializeField] private string hubUrl = "wss://game.rayfiyo.com/ws";
    // [SerializeField] private string hubUrl = "ws://localhost:8765/ws";
    [SerializeField] private string httpBaseUrl = "https://game.rayfiyo.com";
    // [SerializeField] private string httpBaseUrl = "http://localhost:8765";
    [SerializeField] private float reconnectDelaySeconds = 3f;

    private static HubGameClient instance;
    private CancellationTokenSource loopToken;
    private Task loopTask;
    private readonly object taskLock = new object();
    private string apiBaseUrl = "https://game.rayfiyo.com";
    private readonly object signalLock = new object();
    private GameStartSignal latestGameStartSignal;
    private int gameStartSignalVersion;
    private int consumedGameStartSignalVersion;
    private int centerCursorRequestVersion;
    private int processedCenterCursorRequestVersion;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void EnsureInstance()
    {
        if (instance != null)
        {
            return;
        }

        var obj = new GameObject(nameof(HubGameClient));
        instance = obj.AddComponent<HubGameClient>();
        UnityEngine.Object.DontDestroyOnLoad(obj);
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

        apiBaseUrl = NormalizeHttpBase(httpBaseUrl);

        string envUrl = Environment.GetEnvironmentVariable("CGB_HUB_URL");
        if (!string.IsNullOrWhiteSpace(envUrl))
        {
            hubUrl = envUrl.Trim();
        }

        string envApi = Environment.GetEnvironmentVariable("CGB_HUB_HTTP_URL");
        if (!string.IsNullOrWhiteSpace(envApi))
        {
            apiBaseUrl = NormalizeHttpBase(envApi);
        }

        if (string.IsNullOrWhiteSpace(apiBaseUrl))
        {
            apiBaseUrl = NormalizeHttpBase(DeriveHttpBaseFromHubUrl(hubUrl));
        }

        if (string.IsNullOrWhiteSpace(apiBaseUrl))
        {
            apiBaseUrl = "https://game.rayfiyo.com";
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

    private void Update()
    {
        ProcessPendingCursorCenterRequest();
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
            if (loopTask != null)
            {
                loopTask.Wait(TimeSpan.FromSeconds(1));
            }
        }
        catch (AggregateException)
        {
        }
        finally
        {
            lock (taskLock)
            {
                if (loopToken != null)
                {
                    loopToken.Dispose();
                }
                loopToken = null;
                loopTask = null;
            }
        }
    }

    private async Task ConnectionLoopAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            using (var socket = new ClientWebSocket())
            {
                try
                {
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
                    Debug.LogWarning("[HubGameClient] connection error: " + ex.Message);
                }
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
        var stream = new MemoryStream();
        try
        {
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
        finally
        {
            stream.Dispose();
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
            ServerEventPayload serverEvent = JsonUtility.FromJson<ServerEventPayload>(json);
            if (serverEvent != null && !string.IsNullOrWhiteSpace(serverEvent.type) && !serverEvent.type.Equals("state", StringComparison.OrdinalIgnoreCase))
            {
                HandleServerEvent(serverEvent);
                return;
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[HubGameClient] failed to parse server event: " + ex.Message);
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

            ControllerState state = new ControllerState
            {
                Axes = new Vector2(axes.x, axes.y),
                ButtonA = btn.a,
                Timestamp = payload.t
            };

            latestStates[payload.id] = state;
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[HubGameClient] failed to parse message: " + ex.Message);
        }
    }

    private void HandleServerEvent(ServerEventPayload payload)
    {
        if (payload == null)
        {
            return;
        }

        string type = payload.type != null ? payload.type.Trim() : string.Empty;
        if (string.Equals(type, "game_start", StringComparison.OrdinalIgnoreCase))
        {
            HandleGameStartEvent(payload);
            return;
        }

        if (string.Equals(type, "center_cursor", StringComparison.OrdinalIgnoreCase))
        {
            HandleCenterCursorEvent();
            return;
        }
    }

    private void HandleGameStartEvent(ServerEventPayload payload)
    {
        string[] slots = payload.slots ?? Array.Empty<string>();
        string[] copy = new string[slots.Length];
        Array.Copy(slots, copy, slots.Length);

        GameStartSignal signal = new GameStartSignal
        {
            Forced = payload.forced,
            Slots = copy,
            Timestamp = payload.timestamp,
            Connected = payload.connected,
        };

        lock (signalLock)
        {
            latestGameStartSignal = signal;
            gameStartSignalVersion++;
        }

        Debug.Log("[HubGameClient] received game start event (forced: " + signal.Forced + ", connected: " + signal.Connected + ")");
    }

    private void HandleCenterCursorEvent()
    {
        Interlocked.Increment(ref centerCursorRequestVersion);
        Debug.Log("[HubGameClient] received center cursor event");
    }

    public static bool TryGetState(string controllerId, out ControllerState state)
    {
        state = default(ControllerState);

        if (instance == null || string.IsNullOrWhiteSpace(controllerId))
        {
            return false;
        }

        return instance.latestStates.TryGetValue(controllerId, out state);
    }

    public static bool TryConsumeGameStartSignal(out GameStartSignal signal)
    {
        signal = default(GameStartSignal);

        if (instance == null)
        {
            return false;
        }

        lock (instance.signalLock)
        {
            if (instance.gameStartSignalVersion == instance.consumedGameStartSignalVersion)
            {
                return false;
            }

            instance.consumedGameStartSignalVersion = instance.gameStartSignalVersion;
            signal = instance.latestGameStartSignal;
            return true;
        }
    }

    private void ProcessPendingCursorCenterRequest()
    {
        int pending = Volatile.Read(ref centerCursorRequestVersion);
        if (pending == processedCenterCursorRequestVersion)
        {
            return;
        }

        processedCenterCursorRequestVersion = pending;
        CenterCursorOnMainThread();
    }

    private void CenterCursorOnMainThread()
    {
        CursorLockMode previousLockState = Cursor.lockState;
        bool previousVisibility = Cursor.visible;

        if (previousLockState == CursorLockMode.Locked)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.lockState = CursorLockMode.Locked;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.lockState = previousLockState;
        }

        Cursor.visible = previousVisibility;

        Debug.Log("[HubGameClient] centered cursor on request");
    }

    [Serializable]
    private class ServerEventPayload
    {
        public string type;
        public string[] slots;
        public bool forced;
        public long timestamp;
        public int connected;
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

    public struct GameStartSignal
    {
        public bool Forced;
        public string[] Slots;
        public long Timestamp;
        public int Connected;
    }

    public static string HttpBaseUrl
    {
        get { return instance != null ? instance.apiBaseUrl : "https://game.rayfiyo.com"; }
    }

    private static string NormalizeHttpBase(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        value = value.Trim();
        Uri uri;
        if (!Uri.TryCreate(value, UriKind.Absolute, out uri))
        {
            return value.TrimEnd('/');
        }

        var builder = new UriBuilder(uri);
        builder.Path = string.Empty;
        builder.Query = string.Empty;
        builder.Fragment = string.Empty;
        return builder.Uri.ToString().TrimEnd('/');
    }

    private static string DeriveHttpBaseFromHubUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return string.Empty;
        }

        Uri uri;
        if (!Uri.TryCreate(url.Trim(), UriKind.Absolute, out uri))
        {
            return string.Empty;
        }

        var builder = new UriBuilder(uri);
        builder.Path = string.Empty;
        builder.Query = string.Empty;
        builder.Fragment = string.Empty;

        if (builder.Scheme.Equals("wss", StringComparison.OrdinalIgnoreCase))
        {
            builder.Scheme = "https";
        }
        else if (builder.Scheme.Equals("ws", StringComparison.OrdinalIgnoreCase))
        {
            builder.Scheme = "http";
        }

        return builder.Uri.ToString().TrimEnd('/');
    }
}
