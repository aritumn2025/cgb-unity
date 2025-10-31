using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Meeting : MonoBehaviour
{
    private ScoreManager sm;
    public float waitTime = 99.5f;
    private TMP_Text timeText;

    void Start()
    {
        sm = FindFirstObjectByType<ScoreManager>();
        timeText = GetComponent<TMP_Text>();
    }

    void FixedUpdate()
    {
        float delta = Time.deltaTime;
        waitTime -= delta;

        if (Input.GetKey(KeyCode.Alpha1)) waitTime -= 0.1f;
        if (Input.GetKey(KeyCode.Q)) waitTime -= 0.1f;
        if (Input.GetKey(KeyCode.A)) waitTime -= 0.1f;
        if (Input.GetKey(KeyCode.Z)) waitTime -= 0.1f;

        bool accelerate = false;
        if (sm != null)
        {
            for (int i = 0; i < 4; i++)
            {
                if (string.IsNullOrEmpty(sm.GetUserId(i)))
                {
                    continue;
                }

                string slotId = "p" + (i + 1);
                HubGameClient.ControllerState state;
                if (HubGameClient.TryGetState(slotId, out state))
                {
                    if (Mathf.Abs(state.Axes.x) > 0.1f || Mathf.Abs(state.Axes.y) > 0.1f || state.ButtonA)
                    {
                        accelerate = true;
                        break;
                    }
                }
            }
        }

        if (accelerate)
        {
            waitTime -= delta * 9f;
        }

        bool allReady = true;
        if (sm != null)
        {
            for (int i = 0; i < 4; i++)
            {
                if (string.IsNullOrEmpty(sm.GetUserId(i)))
                {
                    allReady = false;
                    break;
                }
            }
        }
        else
        {
            allReady = false;
        }

        if (waitTime <= 0f || allReady)
        {
            SceneManager.LoadScene("Gamemain");
        }

        if (timeText != null)
        {
            timeText.text = "PLAYER WAITING...\r\n" + ((int)waitTime).ToString();
        }
    }
}
