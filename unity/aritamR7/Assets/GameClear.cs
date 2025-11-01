using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
public class GameClear : MonoBehaviour{

    private GameManager gm;
    public AudioClip sound;
    AudioSource audioSource;
    Image image;

    private bool sw_bf = false;
    private bool sw_af = false;

    private bool noEnemy = false;
    private float next = 0.0f;

    void Start(){
        gm = FindFirstObjectByType<GameManager>();
        audioSource = GetComponent<AudioSource>();
        image = GetComponent<Image>();
        image.enabled = false;
    }

    // Update is called once per frame
    void FixedUpdate(){

        if (gm.GameTime > 245.0f && gm.townLife > 0){
            GameObject[] enemy_num = GameObject.FindGameObjectsWithTag("Enemy");
            if (enemy_num.Length == 0) noEnemy = true;
            sw_bf = noEnemy;
        }
            
        if (sw_bf != sw_af){
            image.enabled = true;
            if (sound != null && audioSource != null) audioSource.PlayOneShot(sound);
            sw_af = sw_bf;
        }

        if (sw_af) next += Time.deltaTime;
        if (next > 10.0f) SceneManager.LoadScene("Result");
    }
}
