using UnityEngine;
using System.Collections.Generic;
using Unity.Netcode;
//using System.Diagnostics;
public class ServerClientToggle : NetworkBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public List<GameObject> serverObjects=new List<GameObject>();
    public List<GameObject> clientObjects=new List<GameObject>();
    public List<Behaviour> clientBehaviours=new List<Behaviour>();
    public List<Behaviour> serverBehaviours=new List<Behaviour>();
     
    public override void OnNetworkSpawn()
    {
        Debug.Log("Toggling on objects");
       for (int i = 0; i < serverObjects.Count; i++)
       {
            Debug.Log(NetworkManager.Singleton.IsServer);
           serverObjects[i].SetActive(IsServer);
       } 
       for (int i = 0; i < clientObjects.Count; i++)
        {
            clientObjects[i].SetActive(IsClient);
        }
        for (int i = 0; i < clientBehaviours.Count; i++) {
            clientBehaviours[i].enabled = IsClient;
        }
        for (int i = 0; i < serverBehaviours.Count; i++)
        {
            serverBehaviours[i].enabled = IsServer;
        }
    }

    // Update is called once per frame
    
}
