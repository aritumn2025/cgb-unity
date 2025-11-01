using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Meeting : MonoBehaviour
{
    public float waitTime = 99.5f;
    private TMP_Text timeText;
    private bool forceStartedExternally;

    void Start()
    {
        timeText = GetComponent<TMP_Text>();
    }

    void FixedUpdate()
    {
        float delta = Time.deltaTime;

        if (!forceStartedExternally)
        {
            HubGameClient.GameStartSignal signal;
            if (HubGameClient.TryConsumeGameStartSignal(out signal) && signal.Forced)
            {
                forceStartedExternally = true;
                SceneManager.LoadScene("Gamemain");
                return;
            }
        }

        waitTime -= delta;

        if (Input.GetKey(KeyCode.Alpha1)) waitTime -= 0.1f;
        if (Input.GetKey(KeyCode.Q)) waitTime -= 0.1f;
        if (Input.GetKey(KeyCode.A)) waitTime -= 0.1f;
        if (Input.GetKey(KeyCode.Z)) waitTime -= 0.1f;

        var assignments = HubGameService.GetAssignments();
        bool accelerate = false;
        int connectedCount = 0;
        foreach (var assignment in assignments)
        {
            if (!assignment.Connected || !assignment.HasUser)
            {
                continue;
            }

            connectedCount++;

            string slotId = assignment.SlotId;
            if (string.IsNullOrWhiteSpace(slotId))
            {
                continue;
            }

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

        if (accelerate)
        {
            waitTime -= delta * 9f;
        }

        bool allReady = connectedCount >= 4;

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
