using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Text;
using System.Collections;

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

    public void SendDataToServer(string str)
    {
        string url = "http://localhost:3000/receive-string";
        DataObject d = new DataObject();
        d.data = str;
        string mjson = JsonUtility.ToJson(d);
        StartCoroutine(Post(url, mjson));
    }

    [System.Serializable]
    public class DataObject
    {
        public string data;
    }

    IEnumerator Post(string url, string dataString)
    {
        byte[] bodyRaw = Encoding.UTF8.GetBytes(dataString);
        var request = new UnityWebRequest(url, "POST");
        request.uploadHandler = (UploadHandler)new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Error: " + request.error);
        }
        else
        {
            Debug.Log("Data sent successfully!");
        }
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
