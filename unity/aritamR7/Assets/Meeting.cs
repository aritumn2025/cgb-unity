using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Meeting : MonoBehaviour
{

    private ScoreManager sm;
    public float waitTime = 99.5f;
    private TMP_Text timeText;
    void Start(){
        sm = FindFirstObjectByType<ScoreManager>();
        timeText = GetComponent<TMP_Text>();
    }

    // Update is called once per frame
    void FixedUpdate(){
        waitTime -= Time.deltaTime;

        //ƒ{ƒ^ƒ“‰Ÿ‚·‚ÆŠÔ‰Á‘¬
        if (Input.GetKey(KeyCode.Alpha1)) waitTime -= 0.1f;
        if (Input.GetKey(KeyCode.Q)) waitTime -= 0.1f;
        if (Input.GetKey(KeyCode.A)) waitTime -= 0.1f;
        if (Input.GetKey(KeyCode.Z)) waitTime -= 0.1f;

        /*‰æ–Ê‘JˆÚğŒ*/
        //ŠÔØ‚ê‚É‚æ‚é‘JˆÚ
        if (waitTime <= 0) SceneManager.LoadScene("Gamemain");
        //l”W‚Ü‚è‚É‚æ‚é‘JˆÚ
        int k = 0;
        for(int i = 0; i < 4; i++) if (sm.user_ID[i] > 0)k++;
        if(k==4)SceneManager.LoadScene("Gamemain");

        //•¶š‚Ì•`‰æ
        timeText.text = "PLAYER WAITING...\r\n" + ((int)waitTime).ToString();
    }
}
