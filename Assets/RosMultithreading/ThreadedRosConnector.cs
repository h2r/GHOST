using System.Threading;
using UnityEngine;
using RosSharp.RosBridgeClient;
using RosSharp.RosBridgeClient.Protocols;
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;

public class ThreadedRosConnector : MonoBehaviour
{
    public int secondsTimeout = 10;

    public RosSocket RosSocket { get; private set; }
    public RosSocket.SerializerEnum serializer;
    public Protocol protocol;
    public string rosBridgeServerUrl = "ws://192.168.1.38:9090";

    // Loop publish messages
    private readonly ConcurrentDictionary<string, (Message message, int ttlFrames)> lpMessages = new();

    public void Awake()
    {
        new Thread(RosThreadMain).Start();
    }

    public void Update()
    {
        foreach (var kvp in lpMessages)
        {
            var (message, ttlFrames) = lpMessages[kvp.Key];
            ttlFrames--;
            lpMessages[kvp.Key] = (message, ttlFrames);
        }
    }

    private void RosThreadMain()
    {
        ConnectAndWait();

        while (true)
        {
            foreach (var kvp in lpMessages)
            {
                var (message, ttlFrames) = lpMessages[kvp.Key];
                if (ttlFrames > 0)
                {
                    print("lpm: " + kvp.Key + " " + message);
                    RosSocket.Publish(kvp.Key, message);
                }
                else
                    LoopUnpublish(kvp.Key);
            }
            Thread.Sleep(1000 / 100);
        }
    }

    public void LoopPublish(string publicationId, Message message)
    {
        lpMessages[publicationId] = (message, 3);
    }

    public void LoopUnpublish(string publicationId)
    {
        lpMessages.Remove(publicationId, out _);
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