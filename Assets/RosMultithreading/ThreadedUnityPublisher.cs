using UnityEngine;
using RosSharp.RosBridgeClient;
using Unity.VisualScripting;

[RequireComponent(typeof(ThreadedRosConnector))]
public abstract class ThreadedUnityPublisher<T> : MonoBehaviour where T : Message
{
    public string topic;
    private string publicationId;
    protected bool ready = false;

    private ThreadedRosConnector connector;

    protected virtual void Start()
    {
        connector = GetComponent<ThreadedRosConnector>();
        publicationId = connector.RosSocket.Advertise<T>(topic);
        ready = true;
    }

    protected void LoopPublish(T message, int ttlFrames = 1)
    {
        if (!ready) return;
        connector.LoopPublish(publicationId, message, ttlFrames);
    }

    protected void LoopUnpublish()
    {
        if (!ready) return;
        connector.LoopUnpublish(publicationId);
    }

    protected void KillSpot(T killMessage)
    {
        if (!ready) return;
        connector.KillSpot(publicationId, killMessage);
    }
}