using UnityEngine;

public class EnemySpowner : MonoBehaviour{

    //ゲームオブジェクト
    public GameObject []enemy;
    private GameManager gm;

    //時間
    [Header("最低時間"), Range(20.0f, 240.0f)] public float minTime = 20.0f;
    [Header("最大時間"), Range(20.0f, 240.0f)] public float maxTime = 240.0f;
    [Header("クールタイム")] public float coolTime = 3.0f;
    private float spownTime = 0.1f;

    void Start(){
        gm = FindFirstObjectByType<GameManager>();
    }

    // Update is called once per frame
    void FixedUpdate(){
        if (gm.GameTime >= minTime) spowner();
        if (gm.GameTime >= maxTime) Destroy(this.gameObject);
    }

    private void spowner(){
        spownTime -= Time.deltaTime;
        if (spownTime <= 0){
            int rnd = Random.Range(0, enemy.Length);
            Instantiate(enemy[rnd], transform.position, Quaternion.identity);
            spownTime = coolTime;
        }
    }
}
