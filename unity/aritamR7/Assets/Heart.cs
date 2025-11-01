using UnityEngine;

public class Heart : MonoBehaviour{

    //position
    private float prePosX = 0;
    private float prePosY = 0;
    private float prePosZ = 0;
    private float rad = 0;
    private GameManager gm;
    public float sw_Life = 1;
    private bool down = false;
    private float dwt = 0.0f;
    AudioSource audioSource;
    public AudioClip sound;
    private bool wakeup = false;
    void Start(){
        prePosX = transform.position.x;
        prePosY = transform.position.y;
        prePosZ = transform.position.z;
        rad = Random.Range(0.0f,3.0f);
        gm = FindFirstObjectByType<GameManager>();
        audioSource = GetComponent<AudioSource>();
    }

    void FixedUpdate(){
        rad += Time.deltaTime;
        float nextX = prePosX + ( down ? 10.0f * Mathf.Sin(rad*20.0f) : 0);
        float nextY = prePosY + 20 * Mathf.Cos(rad) - dwt * dwt * 10.0f + dwt * 60.0f;
        transform.position = new Vector3(nextX,nextY, prePosZ);
        down = gm.townLife < sw_Life;
        if (dwt > 10.0f) Destroy(this.gameObject);
        if (down) dwt += 3.0f * Time.deltaTime;
        if (wakeup != down){
            wakeup = down;
            audioSource.PlayOneShot(sound);
        }
    }
}
