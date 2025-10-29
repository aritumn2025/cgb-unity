using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class GameOver : MonoBehaviour
{

    private GameManager gm;
    public AudioClip sound;
    AudioSource audioSource;
    Image image;

    private bool sw_bf = false;
    private bool sw_af = false;

    void Start(){
        gm = FindFirstObjectByType<GameManager>();
        audioSource = GetComponent<AudioSource>();
        image = GetComponent<Image>();
        image.enabled = false;
    }

    // Update is called once per frame
    void FixedUpdate(){
        if(gm.townLife <= 0)sw_bf = true;
        if (sw_bf != sw_af) {
            image.enabled = true;
            if (sound!=null&&audioSource!=null)audioSource.PlayOneShot(sound);
            sw_af = sw_bf;
        }
    }
}
