using UnityEngine;
using UnityEngine.SceneManagement;

public class NextArea : MonoBehaviour{
    
    void Start(){
        
    }

    void FixedUpdate(){
        if(Input.GetKey(KeyCode.Alpha1))SceneManager.LoadScene("Gamemain");
        if(Input.GetKey(KeyCode.Q))     SceneManager.LoadScene("Gamemain");
        if(Input.GetKey(KeyCode.A))     SceneManager.LoadScene("Gamemain");
        if(Input.GetKey(KeyCode.Z))     SceneManager.LoadScene("Gamemain");
    }
}
