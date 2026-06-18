using Unity.Netcode;
using UnityEngine;

public class RpcTest : NetworkBehaviour
{
   public override void OnNetworkSpawn()
   {
       if (!IsServer && IsOwner) //Only send an RPC to the server on the client that owns the NetworkObject that owns this NetworkBehaviour instance
       {
           TestServerRpc(0, NetworkObjectId);
       }
   }

   [Rpc(SendTo.ClientsAndHost)]
   void TestClientRpc(int value, ulong sourceNetworkObjectId)
   {
       Debug.Log($"Client Received the RPC #{value} on NetworkObject #{sourceNetworkObjectId}");
       if (IsOwner) //Only send an RPC to the server on the client that owns the NetworkObject that owns this NetworkBehaviour instance
       {
           TestServerRpc(value + 1, sourceNetworkObjectId);
       }
   }

   [Rpc(SendTo.Server)]
   void TestServerRpc(int value, ulong sourceNetworkObjectId)
   {
       Debug.Log($"Server Received the RPC #{value} on NetworkObject #{sourceNetworkObjectId}");
       TestClientRpc(value, sourceNetworkObjectId);
   }
}