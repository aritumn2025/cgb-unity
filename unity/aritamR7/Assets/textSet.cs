using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;

public class textSet : MonoBehaviour{

    public TMP_Text scoreText;
    public GameObject Ptarget;
    public int num;
    private target Pscr;

    void Start(){
        scoreText = GetComponent<TMP_Text>();
        scoreText.text = "score:0";
        Pscr = Ptarget.GetComponent<target>();
    }

    void FixedUpdate(){
        string slotId = "p" + num.ToString();
        string displayName = HubGameService.GetDisplayName(slotId, "player" + num.ToString());
        scoreText.text = displayName + "\n" + Pscr.playerScore.ToString();
    }
}
