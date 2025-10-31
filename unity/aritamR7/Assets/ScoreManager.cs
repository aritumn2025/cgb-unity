using Unity.VisualScripting;
using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager instance = null;

    // ユーザー設定
    public int[] user_ID = new int[4]; // 0 の時は CPU 扱い
    public int[] personality = new int[4];
    public string[] userIdString = new string[4];
    public string[] userName = new string[4];

    // スコア設定
    public int[] befor_Score = new int[4];
    public int[] after_Score = new int[4];
    public int allScore = 0;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
    }

    void FixedUpdate()
    {
        for (int i = 0; i < 4; i++)
        {
            if (after_Score[i] > 1024)
            {
                after_Score[i] = 1024;
            }
        }
    }

    public void ScoreReset()
    {
        for (int i = 0; i < 4; i++)
        {
            user_ID[i] = 0;
            personality[i] = 0;
            userIdString[i] = string.Empty;
            userName[i] = string.Empty;
            befor_Score[i] = 0;
            after_Score[i] = 0;
        }
        allScore = 0;
    }

    public void ApplyAssignment(int index, string userId, string name, int personaId)
    {
        if (index < 0 || index >= 4)
        {
            return;
        }

        bool assigned = !string.IsNullOrWhiteSpace(userId);
        user_ID[index] = assigned ? 1 : 0;
        userIdString[index] = assigned ? userId.Trim() : string.Empty;
        userName[index] = assigned ? (name ?? string.Empty).Trim() : string.Empty;
        personality[index] = assigned ? personaId : 0;
    }

    public string GetUserId(int index)
    {
        if (index < 0 || index >= 4)
        {
            return string.Empty;
        }
        return userIdString[index] ?? string.Empty;
    }

    public string GetUserName(int index, string fallback)
    {
        if (index < 0 || index >= 4)
        {
            return fallback;
        }
        var name = userName[index];
        return !string.IsNullOrWhiteSpace(name) ? name : fallback;
    }

    // Setting シーンからの入力を受け取るプレースホルダー
    public void userInput()
    {
        // user_ID[] / personality[] に設定する
    }

    // ゲーム終了時にサーバーへ送るプレースホルダー
    public void userOutput()
    {
        // userIdString / after_Score を送信する
    }
}
