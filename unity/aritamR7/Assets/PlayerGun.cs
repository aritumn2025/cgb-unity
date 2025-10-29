using UnityEngine;
using UnityEngine.UIElements;

public class PlayerGun : MonoBehaviour{

    public GameObject target;

    void Start(){
        
    }

    
    void FixedUpdate(){
        float destX = target.transform.position.x - transform.position.x;
        float destY = target.transform.position.y - transform.position.y;
        float dig = Mathf.Atan2(destY,destX) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, dig);
    }
}
