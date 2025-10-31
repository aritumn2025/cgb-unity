using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEngine.GraphicsBuffer;

public class target : MonoBehaviour{

    //属性
    public enum TYPE { A,B,C,D};
    [Header("MBTI")] public TYPE type = TYPE.A;
    private int num = 0; //攻撃回数
    public enum Ptype { Player1, Player2, Player3, Player4,DebugMode,CPU};
    [Header("プレイヤー")] public Ptype ptype = Ptype.Player1;
    //スコアについて
    public int playerFind = 1;
    public int playerScore = 0;
    public bool fewScore = false;
    //オブジェクト取得
    [Header("自機")]public GameObject player;
    [Header("銃弾")]public GameObject []bullet = new GameObject[5];

    public float speed = 0.05f;
    private int dig = 0;
    private int[] digSet_def = new int[4] {  70, 120, 180, 150 };
    private int[] digSet_sup = new int[4] { 100, 150, 180, 210 };
    //効果音について
    public AudioClip []sound = new AudioClip[5];
    AudioSource audioSource;

    //画面サイズ考慮
    private float width = 17.0f;
    private float height = 9.0f;

    //CPUモード
    private float preX = 0f;
    private float preY = 0f;
    private bool cpuBack = false;

    //ゲームマネージャー
    private GameManager gm;

    //スコアマネージャー
    private ScoreManager sm;

    void Start(){

        //コンポーネントの取得
        sm = FindFirstObjectByType<ScoreManager>();
        gm = FindFirstObjectByType<GameManager>();
        audioSource = GetComponent<AudioSource>();
        preX = transform.position.x;
        preY = transform.position.y;

        switch (ptype){
            case Ptype.Player1:
                if (sm.user_ID[0] > 0){
                    bulletSet(sm.personality[0]);
                }
                else{
                    type = TYPE.A;
                    ptype = Ptype.CPU;
                }
                break;
            case Ptype.Player2:
                if (sm.user_ID[1] > 0){
                    bulletSet(sm.personality[1]);
                }
                else{
                    type = TYPE.B;
                    ptype = Ptype.CPU;
                }
                break;
            case Ptype.Player3:
                if (sm.user_ID[2] > 0){
                    bulletSet(sm.personality[2]);
                }
                else{
                    type = TYPE.C;
                    ptype = Ptype.CPU;
                }
                break;
            case Ptype.Player4:
                if (sm.user_ID[3] > 0){
                    bulletSet(sm.personality[3]);
                }
                else{
                    type = TYPE.D;
                    ptype = Ptype.CPU;
                }
                break;
            //これらは書かない
            //DebugMode
            //CPU
        }

    }

    
    void FixedUpdate(){
        if (gm.townLife <= 0) return;
        if (dig==0)moving();
        rotation();
        //下を触らないで
        if (type==TYPE.C && (num % 3 == 0||fewScore)){
            if (dig % 20 == 0 && dig > 20) shotC();
        }
    }

    private void moving(){
        switch (ptype){
            case Ptype.Player1:
                if (Input.GetKey(KeyCode.Alpha1)) KeySettings(0);
                if (Input.GetKey(KeyCode.Alpha2)) KeySettings(1);
                if (Input.GetKey(KeyCode.Alpha3)) KeySettings(2);
                if (Input.GetKey(KeyCode.Alpha4)) KeySettings(3);
                if (Input.GetKey(KeyCode.Alpha5)) KeySettings(4);
                break;
            case Ptype.Player2:
                if (Input.GetKey(KeyCode.Q)) KeySettings(0);
                if (Input.GetKey(KeyCode.W)) KeySettings(1);
                if (Input.GetKey(KeyCode.E)) KeySettings(2);
                if (Input.GetKey(KeyCode.R)) KeySettings(3);
                if (Input.GetKey(KeyCode.T)) KeySettings(4);
                break;
            case Ptype.Player3:
                if (Input.GetKey(KeyCode.A)) KeySettings(0);
                if (Input.GetKey(KeyCode.S)) KeySettings(1);
                if (Input.GetKey(KeyCode.D)) KeySettings(2);
                if (Input.GetKey(KeyCode.F)) KeySettings(3);
                if (Input.GetKey(KeyCode.G)) KeySettings(4);
                break;
            case Ptype.Player4:
                if (Input.GetKey(KeyCode.Z)) KeySettings(0);
                if (Input.GetKey(KeyCode.X)) KeySettings(1);
                if (Input.GetKey(KeyCode.C)) KeySettings(2);
                if (Input.GetKey(KeyCode.V)) KeySettings(3);
                if (Input.GetKey(KeyCode.B)) KeySettings(4);
                break;
            case Ptype.DebugMode:
                if (Input.GetKey(KeyCode.Return))KeySettings(0);
                if (Input.GetKey(KeyCode.W))     KeySettings(1);
                if (Input.GetKey(KeyCode.S))     KeySettings(2);
                if (Input.GetKey(KeyCode.A))     KeySettings(3);
                if (Input.GetKey(KeyCode.D))     KeySettings(4);
                break;
            case Ptype.CPU:
                cpuMode();
                break;
        }
        
    }

    private void KeySettings(int i){
        float pX = transform.position.x;
        float pY = transform.position.y;
        switch (i){
            case 0: //攻撃
                shotSettings();
                break;
            case 1: //上
                if(pY <  height)transform.Translate( 0, speed, 0);
                break;
            case 2: //下
                if(pY > -height)transform.Translate(0, -speed, 0);
                break;
            case 3: //左
                if(pX >  -width)transform.Translate(-speed, 0, 0);
                break;
            case 4: //右
                if(pX <   width)transform.Translate( speed, 0, 0);
                break;
        }

    }

    private void shotSettings(){
        num++;
        if (num % 3 == 0 || fewScore) superShot(); else shot();
        switch (type){
            case TYPE.A:
                dig = num%3==0 ? digSet_def[0] : digSet_sup[0];
                break;
            case TYPE.B:
                dig = num%3==0 ? digSet_def[1] : digSet_sup[1];
                break;
            case TYPE.C:
                dig = num%3==0 ? digSet_def[2] : digSet_sup[2];
                break; 
            case TYPE.D:
                dig = num%3==0 ? digSet_def[3] : digSet_sup[3];
                break;
        }
    }

    private void rotation(){
        if(dig>0)dig-=10;
        if(dig<0)dig=0;
        transform.rotation = Quaternion.Euler(0,0,dig);
        float s = dig * 0.01f + 0.75f;
        transform.localScale = new Vector3(s,s,s);
    }

    //射撃
    private void shot(){
        audioSource.PlayOneShot(sound[0]);
        float pX = player.transform.position.x;
        float pY = player.transform.position.y;
        float destX = transform.position.x - pX;
        float destY = transform.position.y - pY;
        float rad = Mathf.Atan2(destY,destX) * Mathf.Rad2Deg;
        GameObject nB = Instantiate(bullet[0], player.transform.position, Quaternion.identity);
        nB.AddComponent<bullet>().Init(rad, playerFind);
    }
    //特別な射撃
    void superShot(){
        float pX = player.transform.position.x;
        float pY = player.transform.position.y;
        float destX = transform.position.x - pX;
        float destY = transform.position.y - pY;
        float rad = Mathf.Atan2(destY, destX) * Mathf.Rad2Deg;
        switch (type){

            case TYPE.A: //貫通弾
                audioSource.PlayOneShot(sound[1]);
                GameObject nBa = Instantiate(bullet[1], player.transform.position, Quaternion.identity);
                nBa.AddComponent<bullet>().Init(rad, playerFind);
                break;

            case TYPE.B: //散弾
                audioSource.PlayOneShot(sound[2]);
                GameObject nBb = Instantiate(bullet[2], player.transform.position, Quaternion.identity);
                nBb.AddComponent<bullet>().Init(rad, playerFind);
                break;

            case TYPE.C: //連射
                audioSource.PlayOneShot(sound[3]);
                break;

            case TYPE.D: //炸裂弾
                audioSource.PlayOneShot(sound[4]);
                GameObject nBd = Instantiate(bullet[4], player.transform.position, Quaternion.identity);
                nBd.AddComponent<bullet>().Init(rad, playerFind);
                break;

        }
    }

    void shotC(){
        float pX = player.transform.position.x;
        float pY = player.transform.position.y;
        float destX = transform.position.x - pX;
        float destY = transform.position.y - pY;
        float rad = Mathf.Atan2(destY, destX) * Mathf.Rad2Deg;
        rad += Random.Range( -20f, 20.1f);
        Instantiate(bullet[3], player.transform.position, Quaternion.identity).AddComponent<bullet>().Init(rad, playerFind);
    }
    //end

    void cpuMode(){
        //自分座標
        float px = transform.position.x;
        float py = transform.position.y;
        //検索用オブジェクト
        GameObject []cpuFind;
        GameObject closeEnemy;
        float closeDist = 1000;
        float tx = 0;
        float ty = 0;
        //敵がいるかどうか判定
        cpuFind = GameObject.FindGameObjectsWithTag("Enemy");
        if (cpuFind.Length == 0){
            if (py < preY) KeySettings(1);
            if (py > preY) KeySettings(2);
            if (px > preX) KeySettings(3);
            if (px < preX) KeySettings(4);
            return;
        }
        //距離計算
        foreach (GameObject t in cpuFind){
            float tDist = Vector3.Distance(transform.position, t.transform.position);
            if (closeDist > tDist ){
                closeDist = tDist;
                closeEnemy = t;
                tx = t.transform.position.x; 
                ty = t.transform.position.y;
            }
        }
        float pDist = Vector3.Distance(transform.position, player.transform.position);
        if (pDist > 10.0f) cpuBack = true;
        if (closeDist < 2.0f) KeySettings(0);
        //離れすぎてると探索をやめる
        if (closeDist < 10.0f || !cpuBack){            
            if (py < ty) KeySettings(1);
            if (py > ty) KeySettings(2);
            if (px > tx) KeySettings(3);
            if (px < tx) KeySettings(4);
        }
        else{
            if (py < preY) KeySettings(1);
            if (py > preY) KeySettings(2);
            if (px > preX) KeySettings(3);
            if (px < preX) KeySettings(4);
            if(pDist < 2.0)cpuBack = false;
        }
    }

    public void getScore(int t,int s){
        if (gm.townLife <= 0) return;
        if ( t == playerFind) {
            playerScore += type==TYPE.D ? s*2 : s;
        }
    }

    //性格診断の値が引数
    private void bulletSet(int personal){
        //if EF(Passionate)
        //type = TYPE.A;
        //if ET(Active)
        //type = TYPE.B
        //if IF(Calm)
        //type = TYPE.C
        //if IT(Thinker)
        //type = TYPE.D
    }
}
