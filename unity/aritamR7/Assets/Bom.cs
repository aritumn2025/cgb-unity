using UnityEngine;

public class Bom : MonoBehaviour{

    public AudioClip sound;
    AudioSource audioSource;
    Collider2D col;
    private float time = 0.283f;
    public int pfind = 0;

    public void Init(int p){
        pfind = p;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start(){
        audioSource = GetComponent<AudioSource>();
        //audioSource.PlayOneShot(sound);
        col = GetComponent<Collider2D>();
    }

    // Update is called once per frame
    void FixedUpdate(){
        time -= Time.deltaTime;
        if(time <= 0) Destroy(this.gameObject);
    }
}
