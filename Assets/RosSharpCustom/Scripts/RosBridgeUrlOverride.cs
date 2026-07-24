using UnityEngine;

namespace RosSharp.RosBridgeClient
{
    // Every RosConnector in this project stores its own copy of RosBridgeServerUrl,
    // so it's easy for individual GameObjects to drift onto a stale server address
    // (as happened with the battery and arm-cam subscribers). This runs before any
    // RosConnector.Awake() (which is where it opens its connection) and stamps every
    // RosConnector in the scene with one shared URL, so there is a single place to
    // update the address instead of hunting down each GameObject by hand.
    [DefaultExecutionOrder(-1000)]
    public class RosBridgeUrlOverride : MonoBehaviour
    {
        public string sharedRosBridgeServerUrl = "ws://127.0.0.1:9090";

        private void Awake()
        {
            var connectors = FindObjectsByType<RosConnector>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var connector in connectors)
            {
                connector.RosBridgeServerUrl = sharedRosBridgeServerUrl;
            }
            Debug.Log($"[RosBridgeUrlOverride] Set {connectors.Length} RosConnector(s) to {sharedRosBridgeServerUrl}");
        }
    }
}
