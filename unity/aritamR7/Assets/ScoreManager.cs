using Unity.VisualScripting;
using UnityEngine;

public class ScoreManager : MonoBehaviour{

    public static ScoreManager instance = null;

    public int[]befor_Score = new int[4];
    public int[]after_Score = new int[4];
    public int allScore = 0;

    private void Awake(){
        if (instance == null){
            instance = this;
            DontDestroyOnLoad(this.gameObject);
        }
        else{
            Destroy(this.gameObject);
        }
    }


    void Start(){
        
    }

    
    void FixedUpdate(){
        for(int i=0;i<4;i++){
            if(after_Score[i]>1024)
               after_Score[i]=1024;
        }
    }


    public void ScoreReset(){
        for (int i = 0; i < 4; i++){
            befor_Score[i] = 0;
            allScore = 0;
            after_Score[i] = 0;
        }
    } 
}
