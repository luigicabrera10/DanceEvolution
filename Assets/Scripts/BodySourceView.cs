using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Kinect = Windows.Kinect;

using System.IO;

public class BodySourceView : MonoBehaviour
{
    public Material BoneMaterial;
    public GameObject BodySourceManager;

    public List<bool> handsState;
    public List<Vector3> handsCoords; // Coordenadas de las manos
    private GameObject leftCursor; // Cursor object for the left hand
    private GameObject rightCursor; // Cursor object for the right hand

    private Dictionary<ulong, GameObject> _Bodies = new Dictionary<ulong, GameObject>();
    private BodySourceManager _BodyManager;

    public bool saveFrameJoints;
    public bool evaluateDance;
    public bool multiplayer;

    public multiplayerHandler multiplayer_handler;


    public int recordOffset = 20, recordCounter = 0, frameCounter = 0;
    public List<List<Vector3>> recordJoints = new List<List<Vector3>>();

    public double totalEuclidean = 0.0;
    public double localEuclidean = 0.0;

    public GameObject fbxModel; // Reference to the GameObject containing the FBX model
    private Animator animator; 

    private bool animationStart = false;

    public AudioClip musicClip; // Reference to the audio clip
    private AudioSource musicSource; // Reference to the AudioSource component

    private int framesPerSecond = 1;
    private float timeSinceLastFrame = 0f;

    private List<Vector3> secondPlayerCoords;

    private LineRenderer[] skeletonRenderer;

    private string myRawJoints;

    private Dictionary<Kinect.JointType, Kinect.JointType> _BoneMap = new Dictionary<Kinect.JointType, Kinect.JointType>()
    {
        { Kinect.JointType.FootLeft, Kinect.JointType.AnkleLeft },
        { Kinect.JointType.AnkleLeft, Kinect.JointType.KneeLeft },
        { Kinect.JointType.KneeLeft, Kinect.JointType.HipLeft },
        { Kinect.JointType.HipLeft, Kinect.JointType.SpineBase },

        { Kinect.JointType.FootRight, Kinect.JointType.AnkleRight },
        { Kinect.JointType.AnkleRight, Kinect.JointType.KneeRight },
        { Kinect.JointType.KneeRight, Kinect.JointType.HipRight },
        { Kinect.JointType.HipRight, Kinect.JointType.SpineBase },

        { Kinect.JointType.HandTipLeft, Kinect.JointType.HandLeft },
        { Kinect.JointType.ThumbLeft, Kinect.JointType.HandLeft },
        { Kinect.JointType.HandLeft, Kinect.JointType.WristLeft },
        { Kinect.JointType.WristLeft, Kinect.JointType.ElbowLeft },
        { Kinect.JointType.ElbowLeft, Kinect.JointType.ShoulderLeft },
        { Kinect.JointType.ShoulderLeft, Kinect.JointType.SpineShoulder },

        { Kinect.JointType.HandTipRight, Kinect.JointType.HandRight },
        { Kinect.JointType.ThumbRight, Kinect.JointType.HandRight },
        { Kinect.JointType.HandRight, Kinect.JointType.WristRight },
        { Kinect.JointType.WristRight, Kinect.JointType.ElbowRight },
        { Kinect.JointType.ElbowRight, Kinect.JointType.ShoulderRight },
        { Kinect.JointType.ShoulderRight, Kinect.JointType.SpineShoulder },

        { Kinect.JointType.SpineBase, Kinect.JointType.SpineMid },
        { Kinect.JointType.SpineMid, Kinect.JointType.SpineShoulder },
        { Kinect.JointType.SpineShoulder, Kinect.JointType.Neck },
        { Kinect.JointType.Neck, Kinect.JointType.Head },
    };


    void Start(){

        if (evaluateDance){
            LoadReferenceFrames();
            animator = fbxModel.GetComponent<Animator>();
        }

        GameObject multiplayerHandlerObject = new GameObject("MultiplayerHandlerObject");
        //multiplayer_handler = GetComponent<multiplayerHandler>();
        multiplayer_handler = multiplayerHandlerObject.AddComponent<multiplayerHandler>();

        if (multiplayer_handler == null)
        {
            Debug.LogError("Could not add multiplayerHandler component to the instantiated GameObject.");
        }

        if (multiplayer){
            //Debug.Log("HOLAA");
            //multiplayer_handler = GetComponent<multiplayerHandler>();
            //multiplayer_handler.broadcastData("1,0;2,0;3,0");
        }

        musicSource = GetComponent<AudioSource>();


        // Initialize LineRenderers
        skeletonRenderer = new LineRenderer[bones.GetLength(0)];
        for (int i = 0; i < bones.GetLength(0); i++)
        {
            GameObject lineObj = new GameObject("Bone_" + i);
            lineObj.transform.parent = this.transform;
            LineRenderer lr = lineObj.AddComponent<LineRenderer>();
            lr.startWidth = 0.15f;
            lr.endWidth = 0.15f;
            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.startColor = Color.green;
            lr.endColor = Color.green;
            skeletonRenderer[i] = lr;
        }
    }

    void Update ()
    {
        // if (multiplayer_handler != null)
        // { 
        //     Debug.Log("HOLA");
        //     multiplayer_handler.broadcastData("1,0;2,0;3,0");
        //     Debug.Log("Received Data From Server:" + multiplayer_handler.getData());
        // }
        // else
        // {
        //     Debug.Log("ADIOS");
        // }

        if (BodySourceManager == null)
        {
            return;
        }

        _BodyManager = BodySourceManager.GetComponent<BodySourceManager>();
        if (_BodyManager == null)
        {
            return;
        }

        Kinect.Body[] data = _BodyManager.GetData();
        if (data == null)
        {
            // destroy cursors?
            return;
        }

        List<ulong> trackedIds = new List<ulong>();
        foreach(var body in data)
        {
            if (body == null)
            {
                continue;
              }

            if(body.IsTracked)
            {
                trackedIds.Add (body.TrackingId);

                // PrintSkeletonCoordinates(body);
                handsCoords = GetHandsCoords(body);

                if (leftCursor == null){
                    leftCursor = CreateCursor();
                }

                if (rightCursor == null){
                    rightCursor = CreateCursor();
                }

                // Debug.Log("Joint: RIght, X: " + handsCoords[0]);
                // Debug.Log("Joint: Left, X: " + handsCoords[1]);

                UpdateCursor(leftCursor,  handsCoords[0]);
                UpdateCursor(rightCursor,  handsCoords[1]);

                handsState = GetHandsStates(body);

                // Debug.Log("HANDSTATE: Left: " + handsState[0]);
                // Debug.Log("HANDSTATE: Right: " + handsState[1]);

                DrawCircle(leftCursor,  handsState[0]);
                DrawCircle(rightCursor, handsState[1]);

                // Enable/Disable circle colliders based on hand state
                UpdateCircleColliderState(leftCursor, handsState[0]);
                UpdateCircleColliderState(rightCursor, handsState[1]);

                if (saveFrameJoints) recordFrame(body);

                if (evaluateDance) evaluateFrame(body);

                if (multiplayer){
                    // Broadcast my body coords
                    bcastMyBody(body);

                    // Get, reconstruct and print the other players coords
                    handleSecondPlayer();

                    // Edit my coords to the left
                    // Multiplayer evaluation

                }

                if (!animationStart){
                    if (evaluateDance) animator.Play("Finalized_Armature|ArmatureAction 0");
                    animationStart = true;
                    musicSource.clip = musicClip;

                    musicSource.Play();
                    PrintSkeletonJoints(body);

                }

            }
        }



        List<ulong> knownIds = new List<ulong>(_Bodies.Keys);

        // First delete untracked bodies
        foreach(ulong trackingId in knownIds)
        {
            if(!trackedIds.Contains(trackingId))
            {
                Destroy(_Bodies[trackingId]);
                _Bodies.Remove(trackingId);
            }
        }

        foreach(var body in data)
        {
            if (body == null)
            {
                continue;
            }

            if(body.IsTracked)
            {
                if(!_Bodies.ContainsKey(body.TrackingId))
                {
                    _Bodies[body.TrackingId] = CreateBodyObject(body.TrackingId);
                }

                RefreshBodyObject(body, _Bodies[body.TrackingId]);
            }
        }

    }

    private GameObject CreateBodyObject(ulong id)
    {
        GameObject body = new GameObject("Body:" + id);

        for (Kinect.JointType jt = Kinect.JointType.SpineBase; jt <= Kinect.JointType.ThumbRight; jt++)
        {
            GameObject jointObj = GameObject.CreatePrimitive(PrimitiveType.Cube);

            LineRenderer lr = jointObj.AddComponent<LineRenderer>();
            lr.SetVertexCount(2);
            lr.material = BoneMaterial;
            lr.SetWidth(0.05f, 0.05f);

            jointObj.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);
            jointObj.name = jt.ToString();
            jointObj.transform.parent = body.transform;
        }

        return body;
    }

    private void RefreshBodyObject(Kinect.Body body, GameObject bodyObject)
    {
        for (Kinect.JointType jt = Kinect.JointType.SpineBase; jt <= Kinect.JointType.ThumbRight; jt++)
        {
            Kinect.Joint sourceJoint = body.Joints[jt];
            Kinect.Joint? targetJoint = null;

            if(_BoneMap.ContainsKey(jt))
            {
                targetJoint = body.Joints[_BoneMap[jt]];
            }

            Transform jointObj = bodyObject.transform.Find(jt.ToString());
            jointObj.localPosition = GetVector3FromJoint(sourceJoint);

            LineRenderer lr = jointObj.GetComponent<LineRenderer>();
            if(targetJoint.HasValue)
            {
                lr.SetPosition(0, jointObj.localPosition);
                lr.SetPosition(1, GetVector3FromJoint(targetJoint.Value));
                lr.SetColors(GetColorForState (sourceJoint.TrackingState), GetColorForState(targetJoint.Value.TrackingState));
            }
            else
            {
                lr.enabled = false;
            }
        }
    }

    private static Color GetColorForState(Kinect.TrackingState state)
    {
        switch (state)
        {
        case Kinect.TrackingState.Tracked:
            return Color.green;

        case Kinect.TrackingState.Inferred:
            return Color.red;

        default:
            return Color.black;
        }
    }

    private static Vector3 GetVector3FromJoint(Kinect.Joint joint)
    {
        return new Vector3(joint.Position.X * 10, joint.Position.Y * 10, joint.Position.Z * 10);
    }


    private void PrintSkeletonCoordinates(Kinect.Body body)
    {
        foreach (var joint in body.Joints)
        {
            Kinect.JointType jointType = joint.Key;
            Kinect.Joint jointData = joint.Value;
            Vector3 jointPosition = GetVector3FromJoint(jointData);

            Debug.Log("Joint: " + jointType + ", X: " + jointPosition.x + ", Y: " + jointPosition.y + ", Z: " + jointPosition.z);
        }
    }

    private void PrintSkeletonJoints(Kinect.Body body)
    {

        string result = "";

        foreach (var joint in body.Joints)
        {
            Kinect.JointType jointType = joint.Key;
            Kinect.Joint jointData = joint.Value;
            Vector3 jointPosition = GetVector3FromJoint(jointData);

            result += jointType + ", "+ "\n";

        }

        Debug.Log(result);
    }

    private List<Vector3> GetHandsCoords(Kinect.Body body)
    {
        List<Vector3> coords = new List<Vector3>();

        foreach (var joint in body.Joints)
        {
            Kinect.JointType jointType = joint.Key;
            Kinect.Joint jointData = joint.Value;
            Vector3 jointPosition = GetVector3FromJoint(jointData);

            if (jointType == Kinect.JointType.HandLeft || jointType == Kinect.JointType.HandRight) {
                coords.Add(new Vector3(jointPosition.x, jointPosition.y, 0.0f));
                // Debug.Log("Joint: " + jointType + ", X: " + jointPosition.x + ", Y: " + jointPosition.y + ", Z: " + jointPosition.z);
            }
        }

        return coords;
    }


    private GameObject CreateCursor()
    {
        // Create a new cursor object dynamically
        GameObject cursor = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        cursor.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f); // Set the scale of the cursor
        cursor.GetComponent<Renderer>().material.color = Color.blue; // Set the color of the cursor
        return cursor;
    }

    private void UpdateCursor(GameObject cursor, Vector3 handCoords)
    {
        // Update the position of the cursor to the specified coordinates
        cursor.transform.position = handCoords;
    }


    // Add this method to check if the hand is closed
    private bool IsHandClosed(Kinect.HandState handState)
    {
        return handState == Kinect.HandState.Closed;
    }

    private List<bool> GetHandsStates(Kinect.Body body)
    {
        List<bool> states = new List<bool>();

        // Check the state of the left hand
        bool isLeftHandOpen = IsHandClosed (body.HandLeftState);
        states.Add(isLeftHandOpen);

        // Check the state of the right hand
        bool isRightHandOpen = IsHandClosed (body.HandRightState);
        states.Add(isRightHandOpen);

        return states;
    }

    // Add this method to enable/disable circle colliders
    private void UpdateCircleColliderState(GameObject cursor, bool enable)
    {
        var circleCollider = cursor.GetComponent<CircleCollider2D>();
        if (circleCollider != null)
        {
            circleCollider.enabled = enable;
        }
    }


    private void DrawCircle(GameObject cursor, bool handOpen)
    {
        // Check if the circle already exists as a child of the cursor
        Transform circleTransform = cursor.transform.Find("Circle");
        LineRenderer lineRenderer;

        if (circleTransform != null)
        {
            // If the circle already exists, get its LineRenderer component
            lineRenderer = circleTransform.GetComponent<LineRenderer>();
        }
        else
        {
            // If the circle doesn't exist, create a new one as a child of the cursor
            GameObject circle = new GameObject("Circle");
            circle.transform.parent = cursor.transform; // Make the circle a child of the cursor
            circle.transform.localPosition = Vector3.zero; // Set the local position of the circle to the center of the cursor
            lineRenderer = circle.AddComponent<LineRenderer>(); // Add a LineRenderer component to the circle object
            lineRenderer.material = new Material(Shader.Find("Sprites/Default")); // Set the material to render the circle
        }

        // Set color based on hand state
        if (handOpen)
        {
            lineRenderer.startColor = Color.green; // Set the color of the circle
            lineRenderer.endColor = Color.green;
        }
        else
        {
            lineRenderer.startColor = Color.red; // Set the color of the circle
            lineRenderer.endColor = Color.red;
        }

        lineRenderer.startWidth = 0.05f; // Set the width of the circle
        lineRenderer.endWidth = 0.05f;
        lineRenderer.useWorldSpace = false; // Ensure the line is rendered in local space

        // Define the number of vertices to create the circle
        int numSegments = 100;
        float angleIncrement = 360f / numSegments;

        // Populate the positions array to draw the circle
        Vector3[] positions = new Vector3[numSegments];
        for (int i = 0; i < numSegments; i++)
        {
            float angle = Mathf.Deg2Rad * (i * angleIncrement);
            float x = Mathf.Cos(angle);
            float y = Mathf.Sin(angle);
            positions[i] = new Vector3(x, y, 0f); // Define the position of each vertex
        }

        // Set the positions array to the LineRenderer component
        lineRenderer.positionCount = numSegments;
        lineRenderer.SetPositions(positions);
    }


    public void recordFrame(Kinect.Body body){

        timeSinceLastFrame += Time.deltaTime;
        if (timeSinceLastFrame < 1f / framesPerSecond) return;

        timeSinceLastFrame = 0f;

        recordJoints.Add(recordCurrentJoints(body));
        Debug.Log("RECORDING FRAME!!");

        // // TESTING
        // if (recordJoints.Count == 60){
        //     Debug.Log("END FRAMEEEEESSSSSSSSSSSSSSSSSS");
        //     saveTxtJoints();
        // }

        // Save point when music is finished
        if (!musicSource.isPlaying) {
            Debug.Log("Music finished playing!!");
            Debug.Log("END FRAMEEEEESSS (By music)");
            saveTxtJoints();

            saveFrameJoints = false;

        }

    }


    public void resetRecord(){
        recordCounter = 0;
        recordJoints.Clear();
    }



    /*
    --------------------------------------------------------------------------------------------
    --------------------------------------------------------------------------------------------
    --------------------------------------------------------------------------------------------
    */

    public string txtFilePath = @"C:\Users\UCSP\Downloads\joints_dance1COPIA BACKUP NO BORRAR.txt"; // Ruta del archivo de texto
    // public string txtFilePath = 'C:\Users\UCSP\Downloads/joints_dance1COPIA BACKUP NO BORRAR.txt'; // Ruta del archivo de texto

    public float similarityThreshold = 0.1f; // Umbral de similitud

    private List<List<Vector3>> referenceFrames = new List<List<Vector3>>(); // Lista de frames de referencia

    // Método para cargar los puntos del archivo de texto al iniciar la grabación
    private void LoadReferenceFrames()
    {

        Vector3 vectSum = Vector3.zero;

        if (File.Exists(txtFilePath))
        {
            referenceFrames.Clear();
            List<Vector3> currentFrame = new List<Vector3>();

            string[] lines = File.ReadAllLines(txtFilePath);
            foreach (string line in lines)
            {
                if (line == "*")
                {

                    vectSum[0] /= 25;
                    vectSum[1] /= 25;
                    vectSum[2] /= 25;

                    for (int i = 0; i < currentFrame.Count; ++i){
                        // Debug.Log("Joint: " +  currentFrame[i]);
                        currentFrame[i] -= vectSum;
                    }

                    // Se ha completado un frame, agregarlo a la lista
                    referenceFrames.Add(currentFrame);
                    currentFrame = new List<Vector3>();
                    vectSum = Vector3.zero;
                }
                else
                {
                    // Parsear la línea y agregar el vector al frame actual
                    string[] parts = line.Split(' ');
                    float x = float.Parse(parts[0].Substring(0, parts[0].Length-1));
                    float y = float.Parse(parts[1].Substring(0, parts[1].Length-1));
                    float z = float.Parse(parts[2]);

                    vectSum[0] += x;
                    vectSum[1] += y;
                    vectSum[2] += z;

                    currentFrame.Add(new Vector3(x, y, z));
                }
            }
        }
        else
        {
            Debug.LogError("Archivo de texto no encontrado en la ruta especificada: " + txtFilePath);
        }
    }

    // Otros métodos necesarios...

    // Método para obtener las coordenadas de una articulación
    // private Vector3 GetVector3FromJoint(Kinect.Joint joint)
    // {
    //     return new Vector3(joint.Position.X * 10, joint.Position.Y * 10, joint.Position.Z * 10);
    // }

    /*
    --------------------------------------------------------------------------------------------
    --------------------------------------------------------------------------------------------
    --------------------------------------------------------------------------------------------
    */



    public List<Vector3> recordCurrentJoints(Kinect.Body body){

        List<Vector3> frameJoints = new List<Vector3>();

        foreach (var joint in body.Joints) {

            // Kinect.JointType jointType = joint.Key;
            // Kinect.Joint jointData = joint.Value;
            // Vector3 jointPosition = GetVector3FromJoint(jointData);

            // Debug.Log("Joint: " + jointType + ", X: " + jointPosition.x + ", Y: " + jointPosition.y + ", Z: " + jointPosition.z);


            Kinect.Joint jointData = joint.Value;
            Vector3 jointPosition = GetVector3FromJoint(jointData);
            frameJoints.Add( jointPosition );

        }
        
        return frameJoints;

    }


    public void saveTxtJoints(){

        string filePath = "Assets/SavedDances/joints_dance1.txt";

        // Create a StreamWriter to write to the file
        using (StreamWriter writer = new StreamWriter(filePath)) {
            // Iterate through the list of lists
            foreach (List<Vector3> list in recordJoints) {
                // Iterate through each Vector3 in the list
                foreach (Vector3 vector3 in list) {
                    // Serialize the Vector3 to the desired format
                    string serializedVector = $"{vector3.x}, {vector3.y}, {vector3.z}";
                    // Write the serialized Vector3 to the file
                    writer.WriteLine(serializedVector);
                }
                // Add a delimiter after each list
                writer.WriteLine("*");
            }
        }

        // Check if the file was saved successfully
        if (File.Exists(filePath)) {
            Debug.Log("File saved successfully at: " + filePath);
        } else {
            Debug.LogError("Failed to save file at: " + filePath);
        }
    }

    



    public void evaluateFrame(Kinect.Body body){

        DrawPoints();

        if ((++recordCounter) % recordOffset != 0) return; // Doesnt evaluate all the frames

        if (referenceFrames.Count <= frameCounter) {
            Debug.Log("No more frames to compare! frameCounter: " + frameCounter);
            return;
        }

        localEuclidean = 0.0;

        int i = 0;

        Vector3 center = Vector3.zero;

        foreach (var joint in body.Joints) {

            Kinect.Joint jointData = joint.Value;
            Vector3 jointPosition = GetVector3FromJoint(jointData);

            center += jointPosition;
            
        }

        center /= 25;


        foreach (var joint in body.Joints) {

            Kinect.Joint jointData = joint.Value;
            Vector3 jointPosition = GetVector3FromJoint(jointData) - center;

            localEuclidean +=  Vector3.Distance(jointPosition, referenceFrames[frameCounter][i++]);

            Kinect.JointType jointType = joint.Key;
            if (jointType == Kinect.JointType.HandLeft){
                // Debug.Log("Dancer Hand: " + jointPosition);
                // Debug.Log("Model  Hand: " + referenceFrames[frameCounter][i-1]);
            }
            
        }

        ++frameCounter;
        localEuclidean = (double) localEuclidean / i;
        totalEuclidean += (double) localEuclidean / referenceFrames.Count; 

        // Debug.Log("Total euclidean (Frame Evaluator): " + localEuclidean);

    }


    // void DrawPoints()
    // {
    //     LineRenderer lineRenderer = GetComponent<LineRenderer>();

    //     if (lineRenderer == null)
    //     {
    //         lineRenderer = gameObject.AddComponent<LineRenderer>();
    //         lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
    //         lineRenderer.startColor = Color.red;
    //         lineRenderer.endColor = Color.red;
    //         lineRenderer.startWidth = 0.05f;
    //         lineRenderer.endWidth = 0.05f;
    //         lineRenderer.useWorldSpace = false;
    //     }

    //     List<Vector3> localPoints = new List<Vector3>();
    //     foreach (Vector3 point in referenceFrames[frameCounter])
    //     {
    //         localPoints.Add(transform.InverseTransformPoint(point)); // Convert world space points to local space
    //     }

    //     lineRenderer.positionCount = localPoints.Count;
    //     lineRenderer.SetPositions(localPoints.ToArray());
    // }

    void DrawPoints()
    {
        LineRenderer lineRenderer = GetComponent<LineRenderer>();

        if (lineRenderer == null)
        {
            lineRenderer = gameObject.AddComponent<LineRenderer>();
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            lineRenderer.startColor = Color.red;
            lineRenderer.endColor = Color.red;
            lineRenderer.startWidth = 0.05f;
            lineRenderer.endWidth = 0.05f;
            lineRenderer.useWorldSpace = false;
        }

        // Set position count to 0 to clear previous points
        lineRenderer.positionCount = 0;

        foreach (Vector3 point in referenceFrames[frameCounter])
        {
            lineRenderer.positionCount++; // Increase position count by 1
            lineRenderer.SetPosition(lineRenderer.positionCount - 1, transform.InverseTransformPoint(point)); // Add the point to the LineRenderer
        }
    }


    private string getMyRawJoints(Kinect.Body body){
        myRawJoints = "";

        foreach (var joint in body.Joints){
            Kinect.JointType jointType = joint.Key;
            Kinect.Joint jointData = joint.Value;
            Vector3 jointPosition = GetVector3FromJoint(jointData);

            myRawJoints += jointPosition.x + ";" + jointPosition.y + ";" + jointPosition.z + " ";
 
        }

        myRawJoints = myRawJoints.Substring(0, myRawJoints.Length - 1);
        return myRawJoints;
    }

    private void bcastMyBody(Kinect.Body body){

        // How often should I send my data?
        timeSinceLastFrame += Time.deltaTime; // This may not be here
        if (timeSinceLastFrame < 1f / framesPerSecond) return;

        string coords = getMyRawJoints(body);

        // Debug.Log(coords);

        // THIS IS THE CALL TO SEND DATA TO  SERVER
        // multiplayer_handler.broadcastData(coords);
        if (multiplayer_handler != null)
        {
            Debug.Log("Sending coords to server.");
            multiplayer_handler.broadcastData(coords);
            Debug.Log("Data sent succesfully.");
        }
        else
        {
            Debug.Log("Error server unavailable.");
        }

    }

    private List<Vector3> parseMultiplayerData(string inputString){
         // Split the input string by space to get individual coordinates
        string[] coordinateStrings = inputString.Split(' ');

        // Create a list to store Vector3 objects
        List<Vector3> vectorList = new List<Vector3>();

        // Loop through each coordinate string
        foreach (string coordinateString in coordinateStrings)
        {
            // Split the coordinate string by semicolon to get individual components
            string[] components = coordinateString.Split(';');

            // Parse each component and create a Vector3 object
            float x = float.Parse(components[0]) + 8.0f;
            float y = float.Parse(components[1]);
            float z = float.Parse(components[2]);

            // Create the Vector3 object and add it to the list
            vectorList.Add(new Vector3(x, y, z));
        }

        return vectorList;

    }


    private void handleSecondPlayer(){

        // How often should I receive my data?
        if (timeSinceLastFrame < 1f / framesPerSecond){
            // Draw Second Player skeleton
            buildSecondPlayerSkeleton();
            return;
        }

        // Get data from another body vs get get xample data
        string rawData = "";

        if (multiplayer_handler != null)
        {
            Debug.Log("Obtaining coords from server.");
            rawData = multiplayer_handler.getData();
            Debug.Log("Received Data From Server:" + rawData);
        }
        else
        {
            Debug.Log("Error server unavailable.");
        }
        // string rawData = multiplayer_handler.getData();
        // string rawData = "1,710967;-2,158211;11,32239 2,003033;0,2320382;9,664892 2,235195;2,512494;7,845569 2,373011;3,43982;7,431796 0,6008376;2,121691;8,378319 -1,169461;0,4527397;9,151056 -2,693496;-0,653423;7,947274 -3,131407;-0,6501862;7,61282 3,51626;1,672456;7,735881 5,307647;0,468281;7,354301 5,499436;-1,702253;7,369431 5,585092;-2,422013;7,404788 0,8425283;-1,980635;11,0223 0,2720947;-4,604675;11,05048 -0,6126581;-8,969175;11,65069 -0,8845406;-9,447478;10,63683 2,47036;-2,201218;10,91058 2,453259;-4,193339;8,649002 2,523203;-7,54302;5,377934 2,202255;-7,890672;4,283379 2,187695;1,964627;8,324061 -3,836374;-0,9140027;7,374783 -2,87657;-1,175836;7,929423 5,48987;-2,408348;7,194131 5,414104;-2,178874;7,147084";
        rawData = myRawJoints;


        secondPlayerCoords = parseMultiplayerData(rawData);

        // Draw Second Player skeleton
        buildSecondPlayerSkeleton();

        // foreach (Vector3 vector in secondPlayerCoords){
        //     Debug.Log(vector);
        // }
    }

    private readonly int[,] bones = new int[,]
    {
        // {0, 1}, // SpineBase -> SpineMid
        // {1, 21}, // SpineMid -> SpineShoulder
        // {21, 2}, // SpineShoulder -> Neck
        // {2, 3}, // Neck -> Head
        // {21, 4}, // SpineShoulder -> ShoulderLeft
        // {4, 5}, // ShoulderLeft -> ElbowLeft
        // {5, 6}, // ElbowLeft -> WristLeft
        // {6, 7}, // WristLeft -> HandLeft
        // {7, 22}, // HandLeft -> HandTipLeft
        // {6, 23}, // WristLeft -> ThumbLeft
        // {21, 8}, // SpineShoulder -> ShoulderRight
        // {8, 9}, // ShoulderRight -> ElbowRight
        // {9, 10}, // ElbowRight -> WristRight
        // {10, 11}, // WristRight -> HandRight
        // {11, 24}, // HandRight -> HandTipRight
        // {10, 25}, // WristRight -> ThumbRight
        // {0, 12}, // SpineBase -> HipLeft
        // {12, 13}, // HipLeft -> KneeLeft
        // {13, 14}, // KneeLeft -> AnkleLeft
        // {14, 15}, // AnkleLeft -> FootLeft
        // {0, 16}, // SpineBase -> HipRight
        // {16, 17}, // HipRight -> KneeRight
        // {17, 18}, // KneeRight -> AnkleRight
        // {18, 19} // AnkleRight -> FootRight

        {0, 1},     // SpineBase -> SpineMid
        {1, 20},    // SpineMid -> SpineShoulder
        {20, 2},    // SpineShoulder -> Neck
        {2, 3},     // Neck -> Head
        {20, 4},    // SpineShoulder -> ShoulderLeft
        {4, 5},     // ShoulderLeft -> ElbowLeft
        {5, 6},     // ElbowLeft -> WristLeft
        {6, 7},     // WristLeft -> HandLeft
        {20, 8},    // SpineShoulder -> ShoulderRight
        {8, 9},     // ShoulderRight -> ElbowRight
        {9, 10},    // ElbowRight -> WristRight
        {10, 11},   // WristRight -> HandRight
        {0, 12},    // SpineBase -> HipLeft
        {12, 13},   // HipLeft -> KneeLeft
        {13, 14},   // KneeLeft -> AnkleLeft
        {14, 15},   // AnkleLeft -> FootLeft
        {0, 16},    // SpineBase -> HipRight
        {16, 17},   // HipRight -> KneeRight
        {17, 18},   // KneeRight -> AnkleRight
        {18, 19},   // AnkleRight -> FootRight
        {7, 21},    // HandLeft -> HandTipLeft
        {7, 22},    // HandLeft -> ThumbLeft
        {11, 23},   // HandRight -> HandTipRight
        {11, 24}    // HandRight -> ThumbRight
    };

    

    private void buildSecondPlayerSkeleton(){

        if (secondPlayerCoords == null) return;
        if (secondPlayerCoords.Count == 0) return;

        // Drawing the bones
        // DrawBone(0, 1);     // SpineBase -> SpineMid
        // DrawBone(1, 20);    // SpineMid -> SpineShoulder
        // DrawBone(20, 2);    // SpineShoulder -> Neck
        // DrawBone(2, 3);     // Neck -> Head
        // DrawBone(20, 4);    // SpineShoulder -> ShoulderLeft
        // DrawBone(4, 5);     // ShoulderLeft -> ElbowLeft
        // DrawBone(5, 6);     // ElbowLeft -> WristLeft
        // DrawBone(6, 7);     // WristLeft -> HandLeft
        // DrawBone(20, 8);    // SpineShoulder -> ShoulderRight
        // DrawBone(8, 9);     // ShoulderRight -> ElbowRight
        // DrawBone(9, 10);    // ElbowRight -> WristRight
        // DrawBone(10, 11);   // WristRight -> HandRight
        // DrawBone(0, 12);    // SpineBase -> HipLeft
        // DrawBone(12, 13);   // HipLeft -> KneeLeft
        // DrawBone(13, 14);   // KneeLeft -> AnkleLeft
        // DrawBone(14, 15);   // AnkleLeft -> FootLeft
        // DrawBone(0, 16);    // SpineBase -> HipRight
        // DrawBone(16, 17);   // HipRight -> KneeRight
        // DrawBone(17, 18);   // KneeRight -> AnkleRight
        // DrawBone(18, 19);   // AnkleRight -> FootRight
        // DrawBone(7, 21);    // HandLeft -> HandTipLeft
        // DrawBone(7, 22);    // HandLeft -> ThumbLeft
        // DrawBone(11, 23);   // HandRight -> HandTipRight
        // DrawBone(11, 24);   // HandRight -> ThumbRight

        for (int i = 0; i < bones.GetLength(0); i++){
            int joint1 = bones[i, 0];
            int joint2 = bones[i, 1];
            LineRenderer lr = skeletonRenderer[i];
            lr.SetPosition(0, secondPlayerCoords[joint1]);
            lr.SetPosition(1, secondPlayerCoords[joint2]);
        }

    }

    // private void DrawBone(int joint1, int joint2){
    //     Debug.DrawLine(secondPlayerCoords[joint1], secondPlayerCoords[joint2], Color.red);
    // }


}