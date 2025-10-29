using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour{

    [Header("地点")]public GameObject []place = new GameObject [8];
    [Header("召喚物")] public GameObject summonedEnemy;

    [Header("プレイヤー")] public GameObject[] Mplayer = new GameObject[4]; 
    private target []tgt = new target[4];
    private ScoreManager SCM;
    //ゲームタイム
    public float GameTime = 0.0f;
    public float townLife = 4;
    
    public int AllScore = 0;

    private float retime = 7.0f;
    private bool redirect = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start(){
        SCM = FindFirstObjectByType<ScoreManager>();
        for (int i=0;i<4; i++)
            tgt[i] = Mplayer[i].GetComponent<target>();
    }

    // Update is called once per frame
    void FixedUpdate(){
        GameTime += Time.deltaTime;
        if (townLife > 0){
            AllScore = GameTime > 240.0f ? 240 : 2 * (int)GameTime;
            scoreFewCheck();
            scoreInputer();
        }
        if (townLife <= 0)redirect = true;
        if (redirect){
            retime -= Time.deltaTime;
            if(retime<=0)SceneManager.LoadScene("Result");
        }
    }

    void scoreFewCheck(){
        int minScore = 41000;
        for (int i=0;i<4;i++)
            minScore = tgt[i].playerScore < minScore
                     ? tgt[i].playerScore : minScore;
        for (int i=0;i<4;i++)
            tgt[i].fewScore = tgt[i].playerScore <= minScore && tgt[i].playerScore > 5;
    }

    void scoreInputer(){
        for (int i = 0; i < 4; i++){
            SCM.befor_Score[i] = tgt[i].playerScore;
            SCM.allScore = AllScore;
            SCM.after_Score[i] = SCM.befor_Score[i] + AllScore;
        }
    }
}
