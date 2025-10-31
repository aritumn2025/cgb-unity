using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class HubGameService : MonoBehaviour
{
    [Serializable]
    public struct Assignment
    {
        public string SlotId;
        public string UserId;
        public string Name;
        public string Personality;
        public bool Connected;

        public bool HasUser => !string.IsNullOrWhiteSpace(UserId);
    }

    private static HubGameService instance;

    [SerializeField] private float assignmentPollInterval = 1.0f;

    private readonly Dictionary<string, Assignment> assignments =
        new Dictionary<string, Assignment>(StringComparer.OrdinalIgnoreCase);

    private Coroutine pollRoutine;
    private string apiBaseUrl = "http://localhost:8765";
    private bool gameStartSubmitted;
    private bool gameStartInFlight;
    private bool resultSubmitted;
    private bool resultSubmissionInFlight;
    private DateTime? gameStartTimeUtc;

    public static event Action AssignmentsUpdated;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void EnsureInstance()
    {
        if (instance != null)
        {
            return;
        }

        var obj = new GameObject(nameof(HubGameService));
        instance = obj.AddComponent<HubGameService>();
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
        apiBaseUrl = BuildApiBaseUrl();
    }

    private void OnEnable()
    {
        if (pollRoutine == null)
        {
            pollRoutine = StartCoroutine(PollAssignmentsLoop());
        }
    }

    private void OnDisable()
    {
        if (pollRoutine != null)
        {
            StopCoroutine(pollRoutine);
            pollRoutine = null;
        }
    }

    private string BuildApiBaseUrl()
    {
        var baseUrl = HubGameClient.HttpBaseUrl;
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            baseUrl = "http://localhost:8765";
        }
        return baseUrl.TrimEnd('/');
    }

    private IEnumerator PollAssignmentsLoop()
    {
        while (true)
        {
            yield return FetchAssignmentsOnce();
            var delay = Mathf.Max(0.5f, assignmentPollInterval);
            yield return new WaitForSeconds(delay);
        }
    }

    private IEnumerator FetchAssignmentsOnce()
    {
        if (string.IsNullOrEmpty(apiBaseUrl))
        {
            yield break;
        }

        var url = BuildUrl("/api/controller/assignments");
        using (var request = UnityWebRequest.Get(url))
        {
            request.timeout = 5;
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"[HubGameService] assignment fetch failed: {request.error}");
                yield break;
            }

            var json = request.downloadHandler.text;
            if (string.IsNullOrWhiteSpace(json))
            {
                yield break;
            }

            AssignmentListResponse payload = null;
            try
            {
                payload = JsonUtility.FromJson<AssignmentListResponse>(json);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[HubGameService] assignment parse failed: {ex.Message}");
            }

            if (payload?.assignments != null)
            {
                ApplyAssignments(payload.assignments);
            }
        }
    }

    private void ApplyAssignments(AssignmentRecord[] records)
    {
        var remaining = new HashSet<string>(assignments.Keys, StringComparer.OrdinalIgnoreCase);
        var changed = false;

        if (records != null)
        {
            foreach (var record in records)
            {
                if (record == null)
                {
                    continue;
                }

                if (!TryNormalizeSlot(record.slotId, out var slotId))
                {
                    continue;
                }

                var assignment = new Assignment
                {
                    SlotId = slotId,
                    UserId = SafeTrim(record.userId),
                    Name = SafeTrim(record.name),
                    Personality = SafeTrim(record.personality),
                    Connected = record.connected,
                };

                remaining.Remove(slotId);
                if (!assignments.TryGetValue(slotId, out var previous) || !AssignmentEquals(previous, assignment))
                {
                    assignments[slotId] = assignment;
                    changed = true;
                }
            }
        }

        foreach (var key in remaining)
        {
            assignments.Remove(key);
            changed = true;
        }

        if (changed)
        {
            AssignmentsUpdated?.Invoke();
        }
    }

    private static bool AssignmentEquals(Assignment left, Assignment right)
    {
        return string.Equals(left.SlotId, right.SlotId, StringComparison.OrdinalIgnoreCase)
            && string.Equals(left.UserId, right.UserId, StringComparison.Ordinal)
            && string.Equals(left.Name, right.Name, StringComparison.Ordinal)
            && string.Equals(left.Personality, right.Personality, StringComparison.Ordinal)
            && left.Connected == right.Connected;
    }

    private static string SafeTrim(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

    private string BuildUrl(string path)
    {
        var baseUrl = HubGameClient.HttpBaseUrl;
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            baseUrl = apiBaseUrl;
        }
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            baseUrl = "http://localhost:8765";
        }
        baseUrl = baseUrl.TrimEnd('/');

        if (string.IsNullOrEmpty(path))
        {
            return baseUrl;
        }

        if (path.StartsWith("/", StringComparison.Ordinal))
        {
            return baseUrl + path;
        }

        return $"{baseUrl}/{path}";
    }

    public static Assignment[] GetAssignments()
    {
        if (instance == null)
        {
            return Array.Empty<Assignment>();
        }

        return instance.GetAssignmentsInternal();
    }

    private Assignment[] GetAssignmentsInternal()
    {
        if (assignments.Count == 0)
        {
            return Array.Empty<Assignment>();
        }

        var buffer = new Assignment[assignments.Count];
        assignments.Values.CopyTo(buffer, 0);
        return buffer;
    }

    public static bool TryGetAssignment(string slotId, out Assignment assignment)
    {
        if (instance == null)
        {
            assignment = default;
            return false;
        }

        return instance.TryGetAssignmentInternal(slotId, out assignment);
    }

    private bool TryGetAssignmentInternal(string slotId, out Assignment assignment)
    {
        assignment = default;
        if (!TryNormalizeSlot(slotId, out var canonical))
        {
            return false;
        }

        return assignments.TryGetValue(canonical, out assignment);
    }

    public static string GetDisplayName(string slotId, string fallback)
    {
        return TryGetAssignment(slotId, out var assignment) && !string.IsNullOrWhiteSpace(assignment.Name)
            ? assignment.Name
            : fallback;
    }

    public static void ResetSession()
    {
        instance?.ResetSessionInternal();
    }

    private void ResetSessionInternal()
    {
        gameStartSubmitted = false;
        gameStartInFlight = false;
        resultSubmitted = false;
        resultSubmissionInFlight = false;
        gameStartTimeUtc = null;
    }

    public static void NotifyGameStart()
    {
        instance?.QueueGameStart();
    }

    private void QueueGameStart()
    {
        if (gameStartSubmitted || gameStartInFlight)
        {
            return;
        }

        gameStartTimeUtc = DateTime.UtcNow;
        gameStartInFlight = true;
        StartCoroutine(SendGameStartRequest());
    }

    private IEnumerator SendGameStartRequest()
    {
        var url = BuildUrl("/api/game/start");
        using (var request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST))
        {
            request.uploadHandler = new UploadHandlerRaw(Array.Empty<byte>());
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.timeout = 5;

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                gameStartSubmitted = true;
            }
            else
            {
                Debug.LogWarning($"[HubGameService] failed to notify game start: {request.error}");
                gameStartTimeUtc = null;
            }
        }

        gameStartInFlight = false;
    }

    public static void SubmitResults(IReadOnlyDictionary<string, int> scores)
    {
        if (instance == null || scores == null || scores.Count == 0)
        {
            return;
        }

        instance.QueueSubmitResults(scores);
    }

    private void QueueSubmitResults(IReadOnlyDictionary<string, int> scores)
    {
        if (resultSubmissionInFlight || resultSubmitted)
        {
            return;
        }

        var payload = BuildResultPayload(scores);
        if (payload.results == null || payload.results.Length == 0)
        {
            Debug.LogWarning("[HubGameService] no eligible scores to submit");
            return;
        }

        var json = JsonUtility.ToJson(payload);
        resultSubmissionInFlight = true;
        StartCoroutine(SendGameResultRequest(json));
    }

    private GameResultRequest BuildResultPayload(IReadOnlyDictionary<string, int> scores)
    {
        var entries = new List<ResultEntry>(scores.Count);

        foreach (var kvp in scores)
        {
            if (!TryNormalizeSlot(kvp.Key, out var slotId))
            {
                continue;
            }

            if (!assignments.TryGetValue(slotId, out var assignment) || !assignment.HasUser)
            {
                continue;
            }

            entries.Add(new ResultEntry
            {
                slotId = slotId,
                score = kvp.Value,
                name = assignment.Name,
            });
        }

        var payload = new GameResultRequest
        {
            startTime = (gameStartTimeUtc ?? DateTime.UtcNow)
                .ToUniversalTime()
                .ToString("o", CultureInfo.InvariantCulture),
            results = entries.Count > 0 ? entries.ToArray() : Array.Empty<ResultEntry>(),
        };

        return payload;
    }

    private IEnumerator SendGameResultRequest(string json)
    {
        var url = BuildUrl("/api/game/result");
        var body = Encoding.UTF8.GetBytes(json);

        using (var request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST))
        {
            request.uploadHandler = new UploadHandlerRaw(body);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.timeout = 8;

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                resultSubmitted = true;
                var responseText = request.downloadHandler.text;
                if (!string.IsNullOrWhiteSpace(responseText))
                {
                    Debug.Log($"[HubGameService] game result submitted: {responseText}");
                }
            }
            else
            {
                Debug.LogWarning($"[HubGameService] failed to submit game result: {request.error}");
            }
        }

        resultSubmissionInFlight = false;
    }

    private static bool TryNormalizeSlot(string value, out string slotId)
    {
        slotId = string.Empty;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        value = value.Trim();
        if (value.StartsWith("p", StringComparison.OrdinalIgnoreCase))
        {
            value = value.Substring(1);
        }

        if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var number))
        {
            return false;
        }

        if (number < 1 || number > 4)
        {
            return false;
        }

        slotId = "p" + number.ToString(CultureInfo.InvariantCulture);
        return true;
    }

    [Serializable]
    private class AssignmentListResponse
    {
        public AssignmentRecord[] assignments;
    }

    [Serializable]
    private class AssignmentRecord
    {
        public string slotId;
        public string userId;
        public string name;
        public string personality;
        public bool connected;
    }

    [Serializable]
    private class GameResultRequest
    {
        public string startTime;
        public ResultEntry[] results;
    }

    [Serializable]
    private class ResultEntry
    {
        public string slotId;
        public int score;
        public string name;
    }
}
