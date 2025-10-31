using UnityEngine;
using TMPro;

public class textSet : MonoBehaviour
{
    public TMP_Text scoreText;
    public GameObject Ptarget;
    public int num;
    private target Pscr;
    private ScoreManager sm;

    void Start()
    {
        scoreText = GetComponent<TMP_Text>();
        Pscr = Ptarget.GetComponent<target>();
        sm = FindFirstObjectByType<ScoreManager>();
    }

    void FixedUpdate()
    {
        if (scoreText == null || Pscr == null)
        {
            return;
        }

        int slotIndex = Mathf.Clamp(num - 1, 0, 3);
        string fallback = "player" + (slotIndex + 1);
        string displayName = sm != null ? sm.GetUserName(slotIndex, fallback) : fallback;
        scoreText.text = displayName + "\n" + Pscr.playerScore.ToString();
    }
}
