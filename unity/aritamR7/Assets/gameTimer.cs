using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class gameTimer : MonoBehaviour{

    private Image timerGage;
    private GameManager gm;

    void Start(){
        timerGage = GetComponent<Image>();
        gm = FindFirstObjectByType<GameManager>();
    }

    void FixedUpdate(){
        float tgPer = (240.0f - 1.0f * gm.GameTime) / 240.0f;
        if (tgPer <= 0) tgPer = 0;
        timerGage.fillAmount = tgPer;
    }
}
