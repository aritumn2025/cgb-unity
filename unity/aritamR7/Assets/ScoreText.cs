using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class ScoreText : MonoBehaviour{

    private ScoreManager sm;
    public int pre_score = 0;
    public int playerKind = 0;
    public TMP_Text scoreText;
    //ƒQ[ƒ€I—¹‚Ì”»’è
    public bool all = false;

    public bool waiting = false;

    void Start(){
        sm = FindFirstObjectByType<ScoreManager>();
        scoreText = GetComponent<TMP_Text>();
    }

    // Update is called once per frame
    void FixedUpdate(){
        int gamescore = all ? sm.after_Score[playerKind] : sm.allScore;
        if (gamescore - pre_score > 400)     pre_score+=5;
        else if (gamescore - pre_score > 320)pre_score+=4;
        else if (gamescore - pre_score > 240)pre_score+=3;
        else if (gamescore - pre_score > 80) pre_score+=2;
        else if (gamescore - pre_score >  0) pre_score++;
        waiting = pre_score == gamescore;
        scoreText.text = "Player - Score :" + pre_score.ToString();
    }
}
