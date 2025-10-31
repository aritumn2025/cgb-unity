using Unity.VisualScripting;
using UnityEngine;

public class ScoreManager : MonoBehaviour{

    public static ScoreManager instance = null;

    //ユーザ設定
    public int[]user_ID = new int[4]; //0の時CPU操作
    public int[]personality = new int [4]; //性格

    //スコア設定
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
            user_ID[i] = 0;
            personality[i] = 0;
            befor_Score[i] = 0;
            allScore = 0;
            after_Score[i] = 0;
        }
    }


    //Setting画面で呼び出し
    public void userInput(){
        //user_ID[]に入力
        //personality[]に入力
    }


    //ゲーム終了時呼び出し
    public void userOutput(){

        /*サーバーに情報を送る*/
        //user_IDがユーザのID
        //after_Scoreが最終スコア

        //user_ID=0の時は除外を推奨


    }
}
