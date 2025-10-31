using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MeetingText : MonoBehaviour{

    private ScoreManager sm;
    public int playerSet = 0;
    private TMP_Text Tex;
    private bool nuller = false;//ƒ†[ƒUID‚ª•‰‚Ì”‚Ìfalse

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start(){
        sm = FindFirstObjectByType<ScoreManager>();
        Tex = GetComponent<TMP_Text>();
    }

    // Update is called once per frame
    void FixedUpdate(){
        nuller = sm.user_ID[playerSet] > 0;
        Tex.text = "player "+playerSet.ToString()+" : "+(nuller?"OK!":"Ready...");
    }
}
