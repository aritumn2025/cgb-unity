using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class Scorebar : MonoBehaviour{

    private Image scoreGage;
    public GameObject m_scr;
    private ScoreText m_tex;
    public float bar = 0.0f;

    void Start(){
        scoreGage = GetComponent<Image>();
        m_tex = m_scr.GetComponent<ScoreText>();
    }

    
    void FixedUpdate(){
        bar = (m_tex.pre_score * 1.0f) / 1024.0f;
        scoreGage.fillAmount = bar;
    }
}
