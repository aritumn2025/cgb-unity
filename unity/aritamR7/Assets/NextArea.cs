using UnityEngine;
using UnityEngine.SceneManagement;

public class NextArea : MonoBehaviour{
    
    void Start(){
        
    }

    void FixedUpdate(){
        if(Input.GetKey(KeyCode.Alpha1)) nextGo();
        if (Input.GetKey(KeyCode.Q))     nextGo();
        if (Input.GetKey(KeyCode.A))     nextGo();
        if (Input.GetKey(KeyCode.Z))     nextGo();
    }

    void nextGo(){
        SceneManager.LoadScene("Setting");
    }
}
