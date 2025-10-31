using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;


public class Shaberu : MonoBehaviour{

    //お時間頂きます
    [Header("最低時間"), Range(2.0f, 240.0f)] public float minTime = 5.0f;
    [Header("最大時間"), Range(2.0f, 240.0f)] public float maxTime = 240.0f;

    //字幕付ける奴
    private bool jimaku = false;
    private bool sw = false;

    //コンポーネント
    public AudioClip sound;
    AudioSource audioSource;
    Image image;

    //男と男は拳でしか語り合えない
    private GameManager gm;


    void Start(){
        gm = FindFirstObjectByType<GameManager>();
        audioSource = GetComponent<AudioSource>();
        image = GetComponent<Image>();
    }

    void FixedUpdate(){
        jimaku = (minTime <= gm.GameTime && gm.GameTime <= maxTime);
        image.enabled = jimaku;
        if(gm.GameTime>maxTime)Destroy(this.gameObject);
        if (jimaku && !sw){
            sw = true;
            audioSource.PlayOneShot(sound);
        }
    }
}
