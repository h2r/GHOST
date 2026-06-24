using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
public class OwnerGameObjectToggle : NetworkBehaviour
{
    public List<GameObject> objectsToActivateForOwnerOnly;
    public List<Behaviour> scriptsToActivateForOwnerOnly;
    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            foreach (var obj in objectsToActivateForOwnerOnly)
            {
                obj.gameObject.SetActive(true);
            }
            foreach (var script in scriptsToActivateForOwnerOnly)
            {
                script.enabled = true;
            }
        }
    }
}
