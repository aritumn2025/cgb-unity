using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class HubGameSceneBridge
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialise()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "Gamemain")
        {
            HubGameService.ResetSession();
            HubGameService.NotifyGameStart();
        }
        else if (scene.name == "Result")
        {
            SubmitResultsFromScoreManager();
        }
    }

    private static void SubmitResultsFromScoreManager()
    {
        var manager = ScoreManager.instance ?? Object.FindObjectOfType<ScoreManager>();
        if (manager == null)
        {
            return;
        }

        var scores = new Dictionary<string, int>(4);
        AppendScore(scores, "p1", manager.after_Score, 0);
        AppendScore(scores, "p2", manager.after_Score, 1);
        AppendScore(scores, "p3", manager.after_Score, 2);
        AppendScore(scores, "p4", manager.after_Score, 3);

        HubGameService.SubmitResults(scores);
    }

    private static void AppendScore(IDictionary<string, int> dest, string key, int[] source, int index)
    {
        if (source == null || index < 0 || index >= source.Length)
        {
            dest[key] = 0;
            return;
        }

        dest[key] = source[index];
    }
}
