using UnityEngine;

public class strike : MonoBehaviour
{
    public GameObject Bom;
    private float speed = 0.75f;
    private float life = 0.5f;

    private int pfind = 0;
    public void Init(float dig,int p){
        transform.rotation = Quaternion.Euler(0, 0, dig);
        pfind = p;
    }

    void Start()
    {
        
    }

    // Update is called once per frame
    void FixedUpdate(){
        transform.Translate(speed, 0, 0);
        if (life <= 0){
            Instantiate(Bom, transform.position, Quaternion.identity).AddComponent<Bom>().Init(pfind);
            Destroy(gameObject);
        }
        else life-=Time.deltaTime;
    }

    private void OnTriggerEnter2D(Collider2D collision){
        if (collision.gameObject.tag == "Bullet")
        {
            Instantiate(Bom, transform.position, Quaternion.identity).AddComponent<Bom>().Init(pfind);
            Destroy(gameObject);
        }
        if (collision.CompareTag("Enemy"))
        {
            Instantiate(Bom, transform.position, Quaternion.identity).AddComponent<Bom>().Init(pfind);
            Destroy(gameObject);
        }
        //end
    }
}
