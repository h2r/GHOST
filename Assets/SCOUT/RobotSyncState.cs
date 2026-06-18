using UnityEngine;
using Unity.Netcode;
using RosSharp.Urdf;
using RosSharp.RosBridgeClient;
using System;
//using System.Numerics;

[Serializable]
public struct TransformData : INetworkSerializable
{
    public Vector3 Position;
    public Quaternion Rotation;
    public TransformData(UnityEngine.Vector3 position, Quaternion rotation)
    {
        Position=position;
        Rotation=rotation;
    }
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref Position);
        serializer.SerializeValue(ref Rotation);
    }
}
public class RobotSyncState : NetworkBehaviour
{
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public JointStateWriter[] robotJointWriters;
    public JointStateReader[] robotJointReaders;
    public Transform robotTransform;
    
    private NetworkList<float> jointStates = new(writePerm: NetworkVariableWritePermission.Server);
    private NetworkVariable<TransformData> bodyPose=new NetworkVariable<TransformData>(new TransformData(Vector3.zero,Quaternion.identity));

    public override void OnNetworkSpawn()
    {
        
        for (int i = 0; i < robotJointReaders.Length; i++)
        {
            robotJointReaders[i].Read(out string name, out float position, out float velocity, out float effort);
            jointStates.Add(position);
            
        }
        bodyPose.OnValueChanged +=OnPoseChanged;
        Debug.Log("Adding Joint States");
        jointStates.OnListChanged += OnJointStatesChanged;
    }
    private void OnPoseChanged(TransformData oldState, TransformData newState)
    {
        if (IsClient)
        {
            robotTransform.rotation=newState.Rotation;
            robotTransform.position=newState.Position;
        }
    }
    void Update()
    {
        
        if(IsServer)
        {
            UpdatePose();
            if(jointStates.Count>0){
                UpdateJointStates();
                Debug.Log("Updating JointStates");
            }
        }

    }
    void UpdatePose()
    {
        TransformData Message=new TransformData(robotTransform.position,robotTransform.rotation);
        bodyPose.Value=Message;
    }
    void UpdateJointStates()
    {
        for (int i = 0; i < robotJointReaders.Length; i++)
        {
            robotJointReaders[i].Read(out string name, out float position, out float velocity, out float effort);
            jointStates[i] = position;
        }
        UnityEngine.Debug.Log(jointStates[0]);
    }
    // Update is called once per frame
    private void OnJointStatesChanged(NetworkListEvent<float> changeEvent)
    {
        for (int i = 0; i < robotJointWriters.Length; i++)
        {
            if(IsClient){
                robotJointWriters[i].Write(jointStates[i]);
                UnityEngine.Debug.Log("Outputting State");
            }
        }
    }
}
