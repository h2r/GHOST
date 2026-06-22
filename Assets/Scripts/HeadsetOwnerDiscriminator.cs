using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
public class HeadsetOwnerDiscriminator : NetworkBehaviour
{
    public List<Behaviour> scriptsToEnableForOwners;
    public List<GameObject> gameObjectsToEnableForOwners;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public override void OnNetworkSpawn()
    {
        if(IsOwner){
            foreach(var script in scriptsToEnableForOwners)
            {
                if(script!=null){
                    script.enabled=true;
                }
            }
            foreach(GameObject go in gameObjectsToEnableForOwners)
            {
                if (go != null)
                {
                    go.SetActive(true);
                }
            }
        }
    }
}
