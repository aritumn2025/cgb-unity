using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

public class Yajirusi : MonoBehaviour{

    private GameManager gm;
    private float minTime = 6.0f;
    private float maxTime = 20.0f;
    Image image;

    void Start(){
        gm = FindFirstObjectByType<GameManager>();
        image = GetComponent<Image>();
    }

    
    void Update(){
        image.enabled = (minTime <= gm.GameTime && gm.GameTime <= maxTime);
        if (gm.GameTime > maxTime) Destroy(this.gameObject);
    }
}
