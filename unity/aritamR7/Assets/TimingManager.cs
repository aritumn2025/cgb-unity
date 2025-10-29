using UnityEngine;
using UnityEngine.SceneManagement;

public class TimingManager : MonoBehaviour
{

    public GameObject [] m_scr = new GameObject[4];
    private ScoreText []m_tex = new ScoreText[4];

    public AudioClip sound;
    public AudioClip sound2;
    AudioSource audioSource;
    private float sndTime = 0.08f;

    public float wol = 0.0f;
    public int mode = 0;
    void Start(){
        audioSource= GetComponent<AudioSource>();
        for (int i=0;i<4;i++)
             m_tex[i] = m_scr[i].GetComponent<ScoreText>();
    }

    void FixedUpdate(){
        int k=0;
        for(int i=0;i<4;i++)if(m_tex[i].waiting)k++;
        if (k==4&&wol<=0){
            switch (mode){
                case 0:
                    wol = 1.5f;
                    mode++;
                    break;
                case 1:
                    for(int i=0;i<4;i++){
                        m_tex[i].waiting = false;
                        m_tex[i].all = true;
                    }
                    mode++;
                    break;
                case 2:
                    audioSource.PlayOneShot(sound2);
                    wol = 5.0f;
                    mode++;
                    break;
                case 3:
                    SceneManager.LoadScene("Title");
                    break;
            }
        }
        wol -=Time.deltaTime;
        if(sndTime<0&&k!=4)audioSource.PlayOneShot(sound);
        sndTime = sndTime<=0 ? 0.08f : sndTime - Time.deltaTime;
    }
}
