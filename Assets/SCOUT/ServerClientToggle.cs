using UnityEngine;
using System.Collections.Generic;
using Unity.Netcode;
public class ServerClientToggle : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public List<GameObject> serverObjects=new List<GameObject>();
    public List<GameObject> clientObjects=new List<GameObject>();
     
    void Update()
    {
        if (NetworkManager.Singleton == null)
        {
            return;
        }
       for (int i = 0; i < serverObjects.Count; i++)
       {
            Debug.Log(NetworkManager.Singleton.IsServer);
           serverObjects[i].SetActive(NetworkManager.Singleton.IsServer);
       } 
       for (int i = 0; i < clientObjects.Count; i++)
        {
            clientObjects[i].SetActive(NetworkManager.Singleton.IsClient);
        }
    }

    // Update is called once per frame
    
}
