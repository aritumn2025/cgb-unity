using TMPro;
using UnityEngine;

public class ScoreText : MonoBehaviour
{
    private ScoreManager sm;
    public int pre_score = 0;
    public int playerKind = 0;
    public TMP_Text scoreText;
    // ゲーム終了時の表示切替
    public bool all = false;

    public bool waiting = false;

    void Start()
    {
        sm = FindFirstObjectByType<ScoreManager>();
        scoreText = GetComponent<TMP_Text>();
    }

    void FixedUpdate()
    {
        if (sm == null || scoreText == null)
        {
            return;
        }

        int gamescore = all ? sm.after_Score[playerKind] : sm.allScore;
        if (gamescore - pre_score > 400) pre_score += 5;
        else if (gamescore - pre_score > 320) pre_score += 4;
        else if (gamescore - pre_score > 240) pre_score += 3;
        else if (gamescore - pre_score > 80) pre_score += 2;
        else if (gamescore - pre_score > 0) pre_score++;
        waiting = pre_score == gamescore;

        int slotIndex = Mathf.Clamp(playerKind, 0, 3);
        string fallback = $"Player {slotIndex + 1}";
        string displayName = sm.GetUserName(slotIndex, fallback);
        scoreText.text = displayName + " - Score :" + pre_score.ToString();
    }
}
