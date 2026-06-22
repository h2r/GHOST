using UnityEngine;
using System.Collections.Generic;
using Unity.Netcode;
//using System.Diagnostics;
public class ServerClientToggle : NetworkBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public List<GameObject> serverObjects=new List<GameObject>();
    public List<GameObject> clientObjects=new List<GameObject>();
     
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
    }

    // Update is called once per frame
    
}
