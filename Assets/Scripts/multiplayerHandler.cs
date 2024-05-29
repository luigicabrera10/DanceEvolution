using UnityEngine;
using UnityEngine.Networking;

public class multiplayerHandler : MonoBehaviour
{
    public BodySourceView kinectBody;

    // Start is called before the first frame update
    void Start()
    {
        // Init TCP
    }

    public void broadcastData(string myBodydata)
    {
        // Broadcast my data to all players
        // Format: "x1;y1;z1 x2;y2;z2 x3;y3;z3"

        // Debug.Log("My body coords: " + myBodydata);

        SendDataToServer(myBodydata);
    }

    void SendDataToServer(string data)
    {
        string url = "http://localhost:3000/receive-string";
        UnityWebRequest www = UnityWebRequest.Post(url, data);
        www.SendWebRequest();

        while (!www.isDone)
        {
            // Wait until the request is done
        }

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Failed to send data: " + www.error);
        }
        else
        {
            Debug.Log("Data sent successfully!");
        }

        www.Dispose(); // Dispose to free up memory
    }

    public string getData()
    {
        string url = "http://localhost:3000/get-data"; // Adjust the URL to your server endpoint
        UnityWebRequest www = UnityWebRequest.Get(url);
        www.SendWebRequest();

        while (!www.isDone)
        {
            // Wait until the request is done
        }

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Failed to get data: " + www.error);
            return null;
        }
        else
        {
            return www.downloadHandler.text;
        }

        //return data;
    }

    // Update is called once per frame
    void Update()
    {
    }
}
