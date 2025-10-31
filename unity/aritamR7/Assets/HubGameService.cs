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

        public bool HasUser
        {
            get { return !string.IsNullOrWhiteSpace(UserId); }
        }
    }

    private static HubGameService instance;

    [SerializeField] private float assignmentPollInterval = 1.0f;

    private readonly Dictionary<string, Assignment> assignments = new Dictionary<string, Assignment>(StringComparer.OrdinalIgnoreCase);

    private Coroutine pollRoutine;
    private string apiBaseUrl = "https://game.rayfiyo.com";
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
        string baseUrl = HubGameClient.HttpBaseUrl;
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            baseUrl = "https://game.rayfiyo.com";
        }
        return baseUrl.TrimEnd('/');
    }

    private IEnumerator PollAssignmentsLoop()
    {
        while (true)
        {
            yield return FetchAssignmentsOnce();
            float delay = Mathf.Max(0.5f, assignmentPollInterval);
            yield return new WaitForSeconds(delay);
        }
    }

    private IEnumerator FetchAssignmentsOnce()
    {
        if (string.IsNullOrEmpty(apiBaseUrl))
        {
            yield break;
        }

        string url = BuildUrl("/api/controller/assignments");
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            request.timeout = 5;
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning("[HubGameService] assignment fetch failed: " + request.error);
                yield break;
            }

            string json = request.downloadHandler.text;
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
                Debug.LogWarning("[HubGameService] assignment parse failed: " + ex.Message);
            }

            if (payload != null && payload.assignments != null)
            {
                ApplyAssignments(payload.assignments);
            }
        }
    }

    private void ApplyAssignments(AssignmentRecord[] records)
    {
        var remaining = new HashSet<string>(assignments.Keys, StringComparer.OrdinalIgnoreCase);
        bool changed = false;

        if (records != null)
        {
            foreach (AssignmentRecord record in records)
            {
                if (record == null)
                {
                    continue;
                }

                string slotId;
                if (!TryNormalizeSlot(record.slotId, out slotId))
                {
                    continue;
                }

                Assignment assignment = new Assignment
                {
                    SlotId = slotId,
                    UserId = SafeTrim(record.userId),
                    Name = SafeTrim(record.name),
                    Personality = SafeTrim(record.personality),
                    Connected = record.connected,
                };

                remaining.Remove(slotId);
                Assignment previous;
                if (!assignments.TryGetValue(slotId, out previous) || !AssignmentEquals(previous, assignment))
                {
                    assignments[slotId] = assignment;
                    changed = true;
                }
            }
        }

        foreach (string key in remaining)
        {
            assignments.Remove(key);
            changed = true;
        }

        if (changed)
        {
            SyncScoreManager();
            AssignmentsUpdated?.Invoke();
        }
    }

    private void SyncScoreManager()
    {
        ScoreManager manager = ScoreManager.instance ?? UnityEngine.Object.FindObjectOfType<ScoreManager>();
        if (manager == null)
        {
            return;
        }

        for (int i = 1; i <= 4; i++)
        {
            string slotId = "p" + i;
            Assignment assignment;
            if (assignments.TryGetValue(slotId, out assignment) && assignment.HasUser && assignment.Connected)
            {
                int persona = ParsePersonality(assignment.Personality);
                manager.ApplyAssignment(i - 1, assignment.UserId, assignment.Name, persona);
            }
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
        string baseUrl = HubGameClient.HttpBaseUrl;
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            baseUrl = apiBaseUrl;
        }
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            baseUrl = "https://game.rayfiyo.com";
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

        return baseUrl + "/" + path;
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

        Assignment[] buffer = new Assignment[assignments.Count];
        assignments.Values.CopyTo(buffer, 0);
        return buffer;
    }

    public static bool TryGetAssignment(string slotId, out Assignment assignment)
    {
        if (instance == null)
        {
            assignment = default(Assignment);
            return false;
        }

        return instance.TryGetAssignmentInternal(slotId, out assignment);
    }

    private bool TryGetAssignmentInternal(string slotId, out Assignment assignment)
    {
        assignment = default(Assignment);
        string canonical;
        if (!TryNormalizeSlot(slotId, out canonical))
        {
            return false;
        }

        return assignments.TryGetValue(canonical, out assignment);
    }

    public static string GetDisplayName(string slotId, string fallback)
    {
        Assignment assignment;
        if (TryGetAssignment(slotId, out assignment) && assignment.HasUser && assignment.Connected && !string.IsNullOrWhiteSpace(assignment.Name))
        {
            return assignment.Name;
        }
        return fallback;
    }

    public static void ResetSession()
    {
        if (instance != null)
        {
            instance.ResetSessionInternal();
        }
    }

    private void ResetSessionInternal()
    {
        gameStartSubmitted = false;
        gameStartInFlight = false;
        resultSubmitted = false;
        resultSubmissionInFlight = false;
        gameStartTimeUtc = null;
        ScoreManager manager = ScoreManager.instance ?? UnityEngine.Object.FindObjectOfType<ScoreManager>();
        if (manager != null)
        {
            manager.ScoreReset();
        }
        SyncScoreManager();
        AssignmentsUpdated?.Invoke();
    }

    public static void NotifyGameStart()
    {
        if (instance != null)
        {
            instance.QueueGameStart();
        }
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
        string url = BuildUrl("/api/game/start");
        using (UnityWebRequest request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST))
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
                Debug.LogWarning("[HubGameService] failed to notify game start: " + request.error);
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

        GameResultRequest payload = BuildResultPayload(scores);
        if (payload.results == null || payload.results.Length == 0)
        {
            Debug.LogWarning("[HubGameService] no eligible scores to submit");
            return;
        }

        string json = JsonUtility.ToJson(payload);
        resultSubmissionInFlight = true;
        StartCoroutine(SendGameResultRequest(json));
    }

    private GameResultRequest BuildResultPayload(IReadOnlyDictionary<string, int> scores)
    {
        List<ResultEntry> entries = new List<ResultEntry>(scores.Count);
        ScoreManager manager = ScoreManager.instance ?? UnityEngine.Object.FindObjectOfType<ScoreManager>();

        foreach (KeyValuePair<string, int> kvp in scores)
        {
            string slotId;
            if (!TryNormalizeSlot(kvp.Key, out slotId))
            {
                continue;
            }

            Assignment assignment;
            if (!assignments.TryGetValue(slotId, out assignment) || !assignment.HasUser)
            {
                continue;
            }

            string entryName = assignment.Name;
            string userId = assignment.UserId;
            int slotIndex;
            if (slotId.Length > 1 && int.TryParse(slotId.Substring(1), out slotIndex))
            {
                slotIndex -= 1;
            }
            else
            {
                slotIndex = -1;
            }
            if (manager != null && slotIndex >= 0 && slotIndex < 4)
            {
                string resolvedId = manager.GetUserId(slotIndex);
                if (!string.IsNullOrWhiteSpace(resolvedId))
                {
                    userId = resolvedId.Trim();
                }
                string resolvedName = manager.GetUserName(slotIndex, entryName ?? (userId ?? string.Empty));
                if (!string.IsNullOrWhiteSpace(resolvedName))
                {
                    entryName = resolvedName;
                }
            }
            if (string.IsNullOrWhiteSpace(userId))
            {
                userId = assignment.UserId;
            }
            if (string.IsNullOrWhiteSpace(userId))
            {
                continue;
            }
            if (string.IsNullOrWhiteSpace(entryName))
            {
                entryName = userId;
            }

            entries.Add(new ResultEntry
            {
                slotId = slotId,
                userId = userId,
                score = kvp.Value,
                name = entryName,
            });
        }

        GameResultRequest payload = new GameResultRequest
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
        string url = BuildUrl("/api/game/result");
        byte[] body = Encoding.UTF8.GetBytes(json);

        using (UnityWebRequest request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST))
        {
            request.uploadHandler = new UploadHandlerRaw(body);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.timeout = 8;

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                resultSubmitted = true;
                string responseText = request.downloadHandler.text;
                if (!string.IsNullOrWhiteSpace(responseText))
                {
                    Debug.Log("[HubGameService] game result submitted: " + responseText);
                }
            }
            else
            {
                Debug.LogWarning("[HubGameService] failed to submit game result: " + request.error);
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

        int number;
        if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out number))
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

    private static int ParsePersonality(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return 0;
        }

        int result;
        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out result))
        {
            return result;
        }

        return 0;
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
        public string userId;
        public int score;
        public string name;
    }
}
