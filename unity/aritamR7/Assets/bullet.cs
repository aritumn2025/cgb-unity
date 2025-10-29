using Unity.VisualScripting;
using UnityEngine;

public class bullet : MonoBehaviour
{

    public float life = 0.65f;
    public float speed = 0.05f;
    public int damage = 1;

    private int parent = 0;
    public bool pierce = false;

    public void Init(float dig,int p){
        transform.rotation = Quaternion.Euler(0, 0, dig);
        parent = p;
    }

    void Start(){
        
    }


    void FixedUpdate(){
        transform.Translate(speed,0,0);
        if (life <= 0)Destroy(gameObject);
        else life -= Time.deltaTime;
    }

    //Õ“Ë”»’è
    private void OnTriggerEnter2D(Collider2D collision){
        if (!pierce){
            if (collision.gameObject.tag == "Bullet")
            {
                Destroy(this.gameObject);
            }
        }
        if (collision.CompareTag("Enemy")){
            Enemy enemy = collision.GetComponent<Enemy>();
            if (enemy != null) enemy.takeDamage(damage,parent);
            else Debug.Log("ERR");
            Destroy(this.gameObject);
        }
        //end
    }
    //end
}
