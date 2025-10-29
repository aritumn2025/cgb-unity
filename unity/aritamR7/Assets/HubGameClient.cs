using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Cgb.Unity
{
    /// <summary>
    /// WebSocket client that connects the Unity game to the Go hub and
    /// exposes controller states to gameplay scripts.
    /// </summary>
    public sealed class HubGameClient : MonoBehaviour
    {
        private static HubGameClient _instance;

        public static HubGameClient Instance => _instance;

        [SerializeField]
        private string hubUrl = "ws://127.0.0.1:8765/ws";

        [SerializeField, Min(0.1f)]
        private float reconnectInitialDelaySeconds = 1.0f;

        [SerializeField, Min(0.1f)]
        private float reconnectMaxDelaySeconds = 4.0f;

        [SerializeField, Min(0.1f)]
        private float stateStaleAfterSeconds = 3.0f;

        private readonly Dictionary<string, ControllerState> _controllerStates = new Dictionary<string, ControllerState>(StringComparer.OrdinalIgnoreCase);
        private readonly List<string> _pruneBuffer = new List<string>();
        private readonly object _stateLock = new object();

        private readonly List<string> _logQueue = new List<string>();
        private readonly object _logLock = new object();

        private CancellationTokenSource _cts;
        private Task _loopTask;
        private float _currentReconnectDelay;
        private string _resolvedHubUrl;

        private static readonly Stopwatch Uptime = Stopwatch.StartNew();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void EnsureInstance()
        {
            if (Instance != null)
            {
                return;
            }

            var go = new GameObject(nameof(HubGameClient))
            {
                hideFlags = HideFlags.HideInHierarchy | HideFlags.DontSave
            };
            go.AddComponent<HubGameClient>();
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

            _cts = new CancellationTokenSource();
            _currentReconnectDelay = reconnectInitialDelaySeconds;

            var envUrl = Environment.GetEnvironmentVariable("CGB_HUB_URL");
            if (!string.IsNullOrWhiteSpace(envUrl))
            {
                _resolvedHubUrl = envUrl.Trim();
            }
        }

        private void OnEnable()
        {
            if (_loopTask == null)
            {
                _loopTask = RunLoopAsync(_cts.Token);
            }
        }

        private void OnDisable()
        {
            StopClient();
        }

        private void OnDestroy()
        {
            StopClient();
            if (_instance == this)
            {
                _instance = null;
            }
        }

        private void StopClient()
        {
            if (_cts == null)
            {
                return;
            }

            try
            {
                _cts.Cancel();
            }
            catch (ObjectDisposedException)
            {
                // already disposed
            }

            if (_loopTask != null)
            {
                try
                {
                    _loopTask.Wait(TimeSpan.FromSeconds(1));
                }
                catch (AggregateException)
                {
                    // ignored
                }
                _loopTask = null;
            }

            _cts.Dispose();
            _cts = null;
        }

        private async Task RunLoopAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                using var client = new ClientWebSocket();
                try
                {
                    client.Options.KeepAliveInterval = TimeSpan.FromSeconds(15);

                    var hubUri = new Uri(GetHubUrl());
                    await client.ConnectAsync(hubUri, token).ConfigureAwait(false);

                    await SendRegisterAsync(client, token).ConfigureAwait(false);

                    EnqueueLog($"connected to hub {hubUri}");

                    _currentReconnectDelay = reconnectInitialDelaySeconds;

                    await ReceiveLoopAsync(client, token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    EnqueueLog($"hub connection error: {ex.Message}");
                }

                if (token.IsCancellationRequested)
                {
                    break;
                }

                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(_currentReconnectDelay), token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }

                _currentReconnectDelay = Mathf.Min(_currentReconnectDelay * 1.5f, reconnectMaxDelaySeconds);
            }
        }

        private async Task SendRegisterAsync(ClientWebSocket socket, CancellationToken token)
        {
            var payload = Encoding.UTF8.GetBytes("{\"role\":\"game\"}");
            await socket.SendAsync(new ArraySegment<byte>(payload), WebSocketMessageType.Text, true, token).ConfigureAwait(false);
        }

        private async Task ReceiveLoopAsync(ClientWebSocket socket, CancellationToken token)
        {
            var buffer = new byte[4096];
            using var messageBuffer = new System.IO.MemoryStream();

            while (!token.IsCancellationRequested && socket.State == WebSocketState.Open)
            {
                WebSocketReceiveResult result;
                try
                {
                    result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    EnqueueLog($"receive error: {ex.Message}");
                    break;
                }

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    break;
                }

                if (result.Count > 0)
                {
                    messageBuffer.Write(buffer, 0, result.Count);
                }

                if (!result.EndOfMessage)
                {
                    continue;
                }

                if (result.MessageType == WebSocketMessageType.Text)
                {
                    var message = Encoding.UTF8.GetString(messageBuffer.ToArray());
                    try
                    {
                        HandleIncomingMessage(message);
                    }
                    catch (Exception ex)
                    {
                        EnqueueLog($"message parse error: {ex.Message}");
                    }
                }

                messageBuffer.SetLength(0);
            }
        }

        private void HandleIncomingMessage(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            if (MiniJSON.Deserialize(message) is not IDictionary<string, object> parsed)
            {
                return;
            }

            if (!TryGetString(parsed, "type", out var type) || !string.Equals(type, "state", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (!TryGetString(parsed, "id", out var controllerId) || string.IsNullOrEmpty(controllerId))
            {
                return;
            }

            float axisX = 0f;
            float axisY = 0f;

            if (parsed.TryGetValue("axes", out var axesValue) && axesValue is IDictionary<string, object> axes)
            {
                axisX = ToSingle(axes, "x");
                axisY = ToSingle(axes, "y");
            }

            bool buttonA = false;
            if (parsed.TryGetValue("btn", out var btnValue) && btnValue is IDictionary<string, object> buttons)
            {
                buttonA = ToBoolean(buttons, "a");
            }

            long timestamp = 0;
            if (parsed.TryGetValue("t", out var tValue))
            {
                timestamp = ToInt64(tValue);
            }

            UpdateControllerState(controllerId, axisX, axisY, buttonA, timestamp);
        }

        private void UpdateControllerState(string controllerId, float axisX, float axisY, bool buttonA, long timestamp)
        {
            var state = new ControllerState
            {
                AxisX = Mathf.Clamp(axisX, -1f, 1f),
                AxisY = Mathf.Clamp(axisY, -1f, 1f),
                ButtonA = buttonA,
                ControllerTimestamp = timestamp,
                LastUpdateSeconds = Uptime.Elapsed.TotalSeconds
            };

            lock (_stateLock)
            {
                _controllerStates[controllerId] = state;
            }
        }

        public bool TryGetState(string controllerId, out ControllerSnapshot snapshot)
        {
            if (string.IsNullOrEmpty(controllerId))
            {
                snapshot = ControllerSnapshot.Disconnected;
                return false;
            }

            double now = Uptime.Elapsed.TotalSeconds;

            lock (_stateLock)
            {
                if (_controllerStates.TryGetValue(controllerId, out var state))
                {
                    var age = now - state.LastUpdateSeconds;
                    if (age <= stateStaleAfterSeconds)
                    {
                        snapshot = new ControllerSnapshot(state.AxisX, state.AxisY, state.ButtonA, state.ControllerTimestamp, (float)age, true);
                        return true;
                    }
                }
            }

            snapshot = ControllerSnapshot.Disconnected;
            return false;
        }

        private void Update()
        {
            FlushLogs();
            PruneStaleStates();
        }

        private void FlushLogs()
        {
            if (_logQueue.Count == 0)
            {
                return;
            }

            List<string> pending;
            lock (_logLock)
            {
                pending = new List<string>(_logQueue);
                _logQueue.Clear();
            }

            foreach (var message in pending)
            {
                Debug.Log($"[HubGameClient] {message}");
            }
        }

        private void EnqueueLog(string message)
        {
            lock (_logLock)
            {
                _logQueue.Add(message);
            }
        }

        private void PruneStaleStates()
        {
            double now = Uptime.Elapsed.TotalSeconds;
            lock (_stateLock)
            {
                _pruneBuffer.Clear();
                foreach (var pair in _controllerStates)
                {
                    if (now - pair.Value.LastUpdateSeconds > stateStaleAfterSeconds)
                    {
                        _pruneBuffer.Add(pair.Key);
                    }
                }

                if (_pruneBuffer.Count == 0)
                {
                    return;
                }

                foreach (var key in _pruneBuffer)
                {
                    _controllerStates.Remove(key);
                }
            }
        }

        private string GetHubUrl()
        {
            if (!string.IsNullOrEmpty(_resolvedHubUrl))
            {
                return _resolvedHubUrl;
            }

            _resolvedHubUrl = string.IsNullOrWhiteSpace(hubUrl) ? "ws://127.0.0.1:8765/ws" : hubUrl.Trim();
            return _resolvedHubUrl;
        }

        private static bool TryGetString(IDictionary<string, object> dict, string key, out string value)
        {
            if (dict.TryGetValue(key, out var obj) && obj is string str)
            {
                value = str;
                return true;
            }

            value = null;
            return false;
        }

        private static float ToSingle(IDictionary<string, object> dict, string key)
        {
            if (dict.TryGetValue(key, out var value))
            {
                return ToSingle(value);
            }

            return 0f;
        }

        private static float ToSingle(object value)
        {
            switch (value)
            {
                case float f:
                    return f;
                case double d:
                    return (float)d;
                case long l:
                    return l;
                case int i:
                    return i;
                case string s when float.TryParse(s, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var result):
                    return result;
                default:
                    return 0f;
            }
        }

        private static bool ToBoolean(IDictionary<string, object> dict, string key)
        {
            if (dict.TryGetValue(key, out var value))
            {
                return ToBoolean(value);
            }

            return false;
        }

        private static bool ToBoolean(object value)
        {
            switch (value)
            {
                case bool b:
                    return b;
                case double d:
                    return Math.Abs(d) > double.Epsilon;
                case long l:
                    return l != 0;
                case int i:
                    return i != 0;
                case string s when bool.TryParse(s, out var parsed):
                    return parsed;
                default:
                    return false;
            }
        }

        private static long ToInt64(object value)
        {
            switch (value)
            {
                case long l:
                    return l;
                case int i:
                    return i;
                case double d:
                    return (long)d;
                case string s when long.TryParse(s, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var parsed):
                    return parsed;
                default:
                    return 0L;
            }
        }

        private readonly struct ControllerState
        {
            public float AxisX { get; init; }
            public float AxisY { get; init; }
            public bool ButtonA { get; init; }
            public long ControllerTimestamp { get; init; }
            public double LastUpdateSeconds { get; init; }
        }

        public readonly struct ControllerSnapshot
        {
            public static readonly ControllerSnapshot Disconnected = new ControllerSnapshot(0f, 0f, false, 0L, float.PositiveInfinity, false);

            public ControllerSnapshot(float axisX, float axisY, bool buttonA, long timestamp, float ageSeconds, bool connected)
            {
                AxisX = axisX;
                AxisY = axisY;
                ButtonA = buttonA;
                Timestamp = timestamp;
                AgeSeconds = ageSeconds;
                IsConnected = connected;
            }

            public float AxisX { get; }
            public float AxisY { get; }
            public bool ButtonA { get; }
            public long Timestamp { get; }
            public float AgeSeconds { get; }
            public bool IsConnected { get; }
        }
    }
}
