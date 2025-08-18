using System.Threading;
using UnityEngine;
using RosSharp.RosBridgeClient;
using RosSharp.RosBridgeClient.Protocols;
using System.Collections.Concurrent;

public class ThreadedRosConnector : MonoBehaviour
{
    public int secondsTimeout = 10;

    public RosSocket RosSocket { get; private set; }
    public RosSocket.SerializerEnum serializer;
    public Protocol protocol;
    public string rosBridgeServerUrl = "ws://192.168.1.38:9090";
    public int rosTicksPerSecond = 100;

    private readonly ConcurrentDictionary<string, LoopPublishAgent> lpAgents = new();
    private bool isSpotKilled = false;

    public void Awake()
    {
        new Thread(RosThreadMain).Start();
    }

    public void Update()
    {
        foreach (var kvp in lpAgents)
            kvp.Value.OnFrame();
    }

    private void RosThreadMain()
    {
        ConnectAndWait();

        while (true)
        {
            foreach (var kvp in lpAgents)
                kvp.Value.OnRosTick();
            
            Thread.Sleep(1000 / rosTicksPerSecond);
        }
    }

    public void LoopPublish(string publicationId, Message message, int ttlFrames)
    {
        if (isSpotKilled) return;

        if (!lpAgents.ContainsKey(publicationId))
            lpAgents[publicationId] = new(RosSocket, publicationId);

        lpAgents[publicationId].SetMessage(message, ttlFrames);
    }

    public void LoopUnpublish(string publicationId)
    {
        lpAgents[publicationId].ClearMessage();
    }

    public void KillSpot(string killPublicationId, Message killMessage)
    {
        LoopPublish(killPublicationId, killMessage, 3);

        isSpotKilled = true;

        foreach (var kvp in lpAgents)
        {
            if (kvp.Key != killPublicationId)
                kvp.Value.ClearMessage();
        }
    }

    private void ConnectAndWait()
    {
        var isConnected = new ManualResetEvent(false);

        var handledProtocol = ProtocolInitializer.GetProtocol(protocol, rosBridgeServerUrl);
        handledProtocol.OnConnected += (_, _) => isConnected.Set();
        handledProtocol.OnClosed += (_, _) => isConnected.Reset();
        RosSocket = new(handledProtocol, serializer);

        if (isConnected.WaitOne(secondsTimeout * 1000))
            Debug.Log("Connected to RosBridge: " + rosBridgeServerUrl);
        else
            Debug.LogWarning("Failed to connect to RosBridge at: " + rosBridgeServerUrl);
    }

    private void OnApplicationQuit()
    {
        RosSocket.Close();
    }
}

class LoopPublishAgent
{
    private readonly RosSocket RosSocket;
    private readonly string publicationId;

    private readonly object dataLock = new();
    private Message message;
    private int ttlFrames = 0;

    public LoopPublishAgent(RosSocket RosSocket, string publicationId)
    {
        this.RosSocket = RosSocket;
        this.publicationId = publicationId;
    }

    public void OnFrame()
    {
        // lock (dataLock)
        // {
            if (ttlFrames > 0)
                ttlFrames--;
        // }
    }

    public void OnRosTick()
    {
        bool isValidMessage;

        // lock (dataLock)
            isValidMessage = ttlFrames > 0;

        if (isValidMessage)
        {
            Debug.Log("publish " + publicationId + " " + message);
            RosSocket.Publish(publicationId, message);
        }
    }

    public void SetMessage(Message message, int ttlFrames)
    {
        // lock (dataLock)
        // {
            this.message = message;
            this.ttlFrames = ttlFrames;
        // }
    }

    public void ClearMessage()
    {
        // lock (dataLock)
            ttlFrames = 0;
    }
}