using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class multiplayerHandler : MonoBehaviour {

    public BodySourceView kinectBody;

    // Start is called before the first frame update
    void Start() {
        // Init TCP
    }

    public void broadcastData(string myBodydata){
        // broadcast my data to all players
        // Formato: "x1;y1;z1 x2;y2;z2 x3;y3;z3"

        // Debug.Log("My body coords: " + myBodydata);
        
    }

    public string getData(){
        // Get the data from all other players
        // string data = "";
        string data = "2,56;8.14;-9,45 -56,8;0,5;-2,5"; // example



        return data;
    }

    // Update is called once per frame
    void Update() {
        // ?
    }

}
