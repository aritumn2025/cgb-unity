using TMPro;
using UnityEngine;

public class MeetingText : MonoBehaviour
{
    private ScoreManager sm;
    public int playerSet = 0;
    private TMP_Text Tex;

    void Start()
    {
        sm = FindFirstObjectByType<ScoreManager>();
        Tex = GetComponent<TMP_Text>();
    }

    void FixedUpdate()
    {
        if (sm == null || Tex == null)
        {
            return;
        }

        int slotIndex = Mathf.Clamp(playerSet, 0, 3);
        bool assigned = !string.IsNullOrEmpty(sm.GetUserId(slotIndex));
        string display = assigned ? sm.GetUserName(slotIndex, "PLAYER" + (slotIndex + 1)) : "Ready...";
        Tex.text = string.Format("player {0} : {1}", slotIndex + 1, display);
    }
}
