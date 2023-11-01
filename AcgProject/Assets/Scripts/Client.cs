using System;
using System.Threading;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

public class Client : MonoBehaviour
{
    [Header("Host&Port")]
    [SerializeField]
    private string host = "127.0.0.1";
    [SerializeField]
    private int port = 10086;

    [Header("Train")]
    [SerializeField]
    int _maxTrainFrame = 10;
    [SerializeField]
    [Range(0, 1)]
    float _dropOut = 0.8F;

    [Header("Reference")]
    [SerializeField]
    ModelController modelController;

    private Socket client;
    private byte[] messTmp;
    Vector2 captureSize;
    Vector2 _gazePointPrediction;
    private List<Vector3> faceMeshLandmarks = new List<Vector3>();
    private List<Vector3> leftHandLandmarks = new List<Vector3>();
    private List<Vector3> rightHandLandmarks = new List<Vector3>();

    Thread thread;
    TrainData trainData = new TrainData();
    bool _isTraining;
    bool _sendTrainData;
    bool _isTrained;
    List<Vector3> _forPredicationPoints = new List<Vector3>();
    void Start()
    {
        messTmp = new byte[1024 * 32];
        client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        Debug.Log("Connecting...");
        try
        {
            client.Connect(new IPEndPoint(IPAddress.Parse(host), port));
        }
        catch (Exception e)
        {
            Debug.Log(e.Message);
            return;
        }
        thread = new Thread(new ThreadStart(() =>
        {
            while (true)
            {
                if (!client.Connected)
                    continue;
                var count = client.Receive(messTmp);
                if (count != 0)
                {
                    string str = Encoding.UTF8.GetString(messTmp, 1, count - 2);
                    int overIndex = str.IndexOf("'");
                    if (overIndex != -1)
                        str = str.Substring(0, str.IndexOf("'"));
                    Data data = ReadToObject(str, out bool success);
                    if(success)
                    {
                        faceMeshLandmarks = Data.LandmarksToVector3Array(data.faceLandmarks);
                        leftHandLandmarks = Data.LandmarksToVector3Array(data.leftHandLandmarks);
                        rightHandLandmarks = Data.LandmarksToVector3Array(data.rightHandLandmarks);
                        captureSize.x = data.cameraWidth;
                        captureSize.y = data.cameraHeight;
                    }
                    Array.Clear(messTmp, 0, count);
                }
                //send test
                if (_sendTrainData)
                {
                    byte[] buffer;
                    buffer = Encoding.UTF8.GetBytes("train" + trainData.GetTrain() + "|" + trainData.GetTest());
                    if(buffer.Length % 1024 == 0)
                        buffer = Encoding.UTF8.GetBytes("train" + trainData.GetTrain() + "|" + trainData.GetTest() + " ");
                    client.Send(buffer);
                    trainData.Init();
                    _sendTrainData = false;
                    _isTrained = true;
                }
                else if(_isTrained)
                {
                    Vector3[] points = _forPredicationPoints.ToArray();
                    if (points.Length > 0)
                    {
                        StringBuilder sb = new StringBuilder();
                        foreach (Vector3 position in points)
                        {
                            sb.Append(position.x.ToString("G6")).Append(',')
                                .Append(position.y.ToString("G6")).Append(',')
                                .Append(position.z.ToString("G6")).Append(',');
                        }
                        byte[] buffer;
                        buffer = Encoding.UTF8.GetBytes("predict" + sb.ToString().Substring(0, sb.Length - 2));
                        if (buffer.Length % 1024 == 0)
                            buffer = Encoding.UTF8.GetBytes("predict" + sb.ToString().Substring(0, sb.Length - 2) + " ");
                        client.Send(buffer);
                    }
                    else
                    {
                        client.Send(Encoding.UTF8.GetBytes("idle"));
                    }
                }
                else
                {
                    client.Send(Encoding.UTF8.GetBytes("idle"));
                }
                Thread.Sleep(20);
            }
        }));
        thread.Start();
    }
    private void FixedUpdate()
    {
        modelController.UpdateHeadModel(new List<Vector3>(faceMeshLandmarks), captureSize, _gazePointPrediction);
        modelController.UpdateHandModel(new List<Vector3>(leftHandLandmarks), false);
        modelController.UpdateHandModel(new List<Vector3>(rightHandLandmarks), true);
    }
    Data ReadToObject(string json, out bool success)
    {
        Data deserializedUser = new Data();
        //print("json: " + json);
        success = true;
        try
        {
            deserializedUser = (Data)JsonUtility.FromJson(json, deserializedUser.GetType());
        }
        catch (ArgumentException e)
        {
            print(e.Message + ", " + e.StackTrace);
            success = false;
        }
        return deserializedUser;
    }
    public void AppendForPredicationPoints(Vector3[] positions, Vector2 screenPosition)
    {
        if(_isTraining)
        {
            if(UnityEngine.Random.value > _dropOut)
                trainData.AppendTrainAndTest(positions, new Vector2(screenPosition.x / Screen.width, screenPosition.y / Screen.height));
        }
        else if(_isTrained)
        {
            _forPredicationPoints.Clear();
            _forPredicationPoints.AddRange(positions);
        }
    }
    public void StartTrain()
    {
        _isTraining = true;
    }
    public void StopTrain()
    {
        _isTraining = false;
        _sendTrainData = true;
    }
    private void OnDestroy()
    {
        thread?.Abort();
        client?.Close();
    }
}

[Serializable]
class Data
{
    public float cameraWidth;
    public float cameraHeight;
    public List<Landmark> faceLandmarks;
    public List<Landmark> leftHandLandmarks;
    public List<Landmark> rightHandLandmarks;
    public override string ToString()
    {
        string tmp = "";
        foreach (Landmark landmark in faceLandmarks)
        {
            tmp += landmark + "\n";
        }
        return tmp;
    }
    public static List<Vector3> LandmarksToVector3Array(List<Landmark> landmarks)
    {
        List<Vector3> vector3s = new List<Vector3>();
        landmarks.ForEach(landmark => vector3s.Add(landmark.ToVector3()));
        return vector3s;
    }
}

[Serializable]
class Landmark
{
    public string x;
    public string y;
    public string z;
    public override string ToString()
    {
        return "x: " + x + " y: " + y + " z: " + z;
    }
    public Vector3 ToVector3()
    {
        return new Vector3(float.Parse(x), float.Parse(y), float.Parse(z));
    }
}