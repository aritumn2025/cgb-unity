using UnityEngine;

public class TitleMoving : MonoBehaviour{

    private float rad = 0.0f;
    private float posX, posY;
    void Start()
    {
        rad = Random.Range(0.0f,3.0f);
        posX = transform.position.x;
        posY = transform.position.y;
    }

    // Update is called once per frame
    void Update()
    {
        rad += Time.deltaTime;
        transform.position = new Vector3(posX, posY+40*Mathf.Cos(rad), 0);
    }
}
