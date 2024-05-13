using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // Import the UI namespace

public class ScoreUpdate : MonoBehaviour
{

    public Text scoreText;
    public BodySourceView kinectBody;
    
    // Start is called before the first frame update
    void Start()
    {
        scoreText = GetComponent<Text>();
    }

    // Update is called once per frame
    void Update()
    {

        double lastScore = kinectBody.localEuclidean;

        if (lastScore < 2.5){
            scoreText.text = "Perfect!";
        }
        else if (lastScore < 3.0){
            scoreText.text = "Nice!";
        }
        else{
            scoreText.text = "Bad!";
        }
        
    }
}
