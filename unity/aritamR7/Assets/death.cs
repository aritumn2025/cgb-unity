using UnityEngine;

public class death : MonoBehaviour{
    public AudioClip sound;
    AudioSource audioSource;
    private float mLength;
    private float mCur;

    // Use this for initialization
    void Start(){
        Animator animOne = GetComponent<Animator>();
        AnimatorStateInfo infAnim = animOne.GetCurrentAnimatorStateInfo(0);
        mLength = infAnim.length;
        mCur = 0;
        audioSource = GetComponent<AudioSource>();
        audioSource.PlayOneShot(sound);
    }

    // Update is called once per frame
    void FixedUpdate(){
        mCur += Time.deltaTime;
        if (mCur > mLength){
            GameObject.Destroy(gameObject);
        }
    }
}
