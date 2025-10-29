using NUnit.Framework;
using NUnit.Framework.Internal;
using System.Collections.Generic;
using System.Numerics;
using Unity.VisualScripting;
using UnityEngine;

public class Enemy : MonoBehaviour{

    private GameManager gm;

    public AudioClip sound;
    AudioSource audioSource;
    //行動パターンについて
    private enum MOVE { up,down,left,right};
    private MOVE move = MOVE.down;
    public enum ACT { straight,curve,wave,roop,cheet };
    [Header("行動パターン")] public ACT act;
    [Header("目的地Object")] private GameObject[]dest = new GameObject[8];
    private int action = 0;
    private float bomdelay = 0;

    //エネミー性能
    [Header("体力")] public int hp = 1;
    [Header("攻撃力")]public int atk = 2;
    [Header("召喚術")] public bool sponer = false;
    [Header("ボス悪")] public bool boss = false;
    private GameObject smdEnemy;

    //座標系
    [Header("召喚場所")] public int summon = 0;
    [Header("移動速度")] public float speed = 1.0f;
    private List<int> wayList = new List<int>();
    private float r;

    public GameObject death;

    void Start(){
        audioSource = GetComponent<AudioSource>();
        audioSource.volume = 0.25f;
        //乱数
        r = rnd();
        //ゲームマネージャーから座標を貰う
        gm = FindFirstObjectByType<GameManager>();
        dest = gm.place;
        smdEnemy = gm.summonedEnemy;
        if (summon == 0 || summon == 1){
            transform.position = dest[summon].transform.position;
            transform.Translate(rnd(), 0, 0);
        }
        else{
            summon = transform.position.x < 0 ? 0 : 1;
        }
        //移動処理
        switch (act) {
            case ACT.straight:
                for(int i=1;i<=3;i++)
                    wayList.Add(summon==0?2*i:2*i+1);
                break;
            case ACT.curve:
                if ( Random.Range(1,11)%2==0 ){
                    wayList.Add(summon==0?2:3);
                    wayList.Add(summon==0?3:2);
                    wayList.Add(summon==0?5:4);
                    wayList.Add(summon==0?7:6);
                }else{
                    wayList.Add(summon==0?2:3);
                    wayList.Add(summon==0?4:5);
                    wayList.Add(summon==0?5:4);
                    wayList.Add(summon==0?7:6);
                }
                break;
            case ACT.wave:
                wayList.Add(summon==0?2:3);
                wayList.Add(summon==0?3:2);
                wayList.Add(summon==0?5:4);
                wayList.Add(summon==0?4:5);
                wayList.Add(summon==0?6:7);
                break;
            case ACT.roop:
                wayList.Add(summon==0?2:3);
                wayList.Add(summon==0?3:5);
                wayList.Add(summon==0?5:4);
                wayList.Add(summon==0?4:2);
                wayList.Add(summon==0?2:3);
                wayList.Add(summon==0?3:5);
                wayList.Add(summon==0?5:4);
                wayList.Add(summon==0?7:6);
                break;
            case ACT.cheet:
                wayList.Add(Random.Range(6,8));
                break;
        }
        
    }

    void FixedUpdate(){
        search();
        moving();
        if (bomdelay > 0) bomdelay -= Time.deltaTime;
    }

    void search(){
        //座標計算
        float destX = dest[wayList[action]].transform.position.x - transform.position.x;
        float destY = dest[wayList[action]].transform.position.y - transform.position.y;
        bool check = Mathf.Sqrt(destX*destX+destY*destY)<r;
        if (check){
            if (wayList[action] == 6 || wayList[action] == 7){
                townDamage();
            }
            else{
                action++;
                r = rnd();
            }
        }
    }

    //オブジェクトの移動について
    void moving(){
        float destX = dest[wayList[action]].transform.position.x - transform.position.x;
        float destY = dest[wayList[action]].transform.position.y - transform.position.y;
        float rad = Mathf.Atan2(destY, destX);
        float PI4 = Mathf.PI / 4;
        if (PI4 <= rad && rad <= PI4 * 3) move = MOVE.up;
        if (-3 * PI4 <= rad && rad <= -PI4) move = MOVE.down;
        if (-PI4 < rad && rad < PI4) move = MOVE.right;
        if (-3 * PI4 > rad || rad > 3 * PI4) move = MOVE.left;
        //移動処理
        switch (move){
            case MOVE.up:
                transform.Translate(0, speed,0);
                break;
            case MOVE.down:
                transform.Translate(0,-speed,0);
                break;
            case MOVE.left:
                transform.Translate(-speed,0,0);
                break;
            case MOVE.right:
                transform.Translate( speed,0,0);
                break;
        }
    }

    //街へのダメージ
    void townDamage(){
        gm.townLife-=atk;
        Destroy(this.gameObject);
    }

    float rnd(){
        return Random.Range(0.25f, 2.50f);
    }

    public void takeDamage(int damage,int p){
        //被ダメージ処理
        hp -= damage;
        //プレイヤーとの共鳴
        if (p > 0){
            GameObject []pt = GameObject.FindGameObjectsWithTag("Player");
            foreach (GameObject scrP in pt){
                target tgt = scrP.GetComponent<target>();
                tgt.getScore(p,damage);
            }
        }
        //生存判定
        if (hp <= 0){
            Instantiate(death, transform.position, UnityEngine.Quaternion.identity);
            Destroy(this.gameObject);
        }
        else{
            if(sound!=null&&audioSource!=null)audioSource.PlayOneShot(sound);
            if (hp%5==0&&sponer) Instantiate(smdEnemy, this.transform.position, UnityEngine.Quaternion.identity);
            if (boss) speed += 0.002f;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision){
        if (collision.CompareTag("Bomn")&&bomdelay<=0.1f){
            Bom bom = collision.GetComponent<Bom>();
            if (bom != null){
                takeDamage(3, bom.pfind);
                bomdelay = 0.4f;
            }
        }
        //end
    }
}
