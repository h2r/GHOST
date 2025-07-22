using UnityEngine;
using RosSharp.RosBridgeClient;

[RequireComponent(typeof(RosConnector))]
public abstract class ThreadedUnityPublisher<T> : MonoBehaviour where T : Message
{
    public string topic;
    private string publicationId;

    private ThreadedRosConnector connector;

    protected virtual void Start()
    {
        connector = GetComponent<ThreadedRosConnector>();
        publicationId = connector.RosSocket.Advertise<T>(topic);
    }

    protected void LoopPublish(T message)
    {
        connector.LoopPublish(publicationId, message);
    }

    protected void LoopUnpublish()
    {
        connector.LoopUnpublish(publicationId);
    }
}