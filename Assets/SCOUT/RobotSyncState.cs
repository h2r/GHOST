using UnityEngine;
using Unity.Netcode;
using RosSharp.Urdf;
using RosSharp.RosBridgeClient;
public class RobotSyncState : NetworkBehaviour
{
   
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public JointStateWriter[] robotJointWriters;
    public JointStateReader[] robotJointReaders;
    private NetworkList<float> jointStates = new(writePerm: NetworkVariableWritePermission.Server);
    void OnNetworkSpawn()
    {
        jointStates.OnListChanged += OnJointStatesChanged;
        for (int i = 0; i < robotJointReaders.Length; i++)
        {
            robotJointReaders[i].Read(out string name, out float position, out float velocity, out float effort);
            jointStates.Add(position);
        }
    }
    void Update()
    {
        if(IsServer&&jointStates.Count>0)
        {
            UpdateJointStates();
        }
    }
    void UpdateJointStates()
    {
        for (int i = 0; i < robotJointReaders.Length; i++)
        {
            robotJointReaders[i].Read(out string name, out float position, out float velocity, out float effort);
            jointStates[i] = position;
        }
    }
    // Update is called once per frame
    private void OnJointStatesChanged(NetworkListEvent<float> changeEvent)
    {
        for (int i = 0; i < robotJointWriters.Length; i++)
        {
            if(IsClient){
                robotJointWriters[i].Write(jointStates[i]);
            }
        }
    }
}
