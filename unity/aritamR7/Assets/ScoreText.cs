using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class ScoreText : MonoBehaviour{

    private ScoreManager sm;
    public int pre_score = 0;
    public int playerKind = 0;
    public TMP_Text scoreText;
    //ÉQÅ[ÉÄèIóπéûÇÃîªíË
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
        scoreText.text = ResolveDisplayName() + " - Score :" + pre_score.ToString();
    }

    private string ResolveDisplayName()
    {
        int slotIndex = playerKind + 1;
        if (slotIndex < 1)
        {
            slotIndex = 1;
        }
        else if (slotIndex > 4)
        {
            slotIndex = 4;
        }
        string slotId = "p" + slotIndex.ToString();
        return HubGameService.GetDisplayName(slotId, "Player " + slotIndex.ToString());
    }
}
