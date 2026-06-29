using UnityEngine;
using Unity.Netcode;
using System;
using static SpotObserverClient;
public class TextureSync : NetworkBehaviour
{
    public struct ImageNetwork : INetworkSerializable
    {
        public byte[] data;
        
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
           
            
            
            serializer.SerializeValue(ref data);
        }
        public void SetTexture(Texture2D texture)
        {
            data = texture.EncodeToPNG();
        }
        public Texture2D GetTexture()
        {
            if(data == null || data.Length == 0) { return null; }
            Texture2D tex = new Texture2D(1, 1);
            if (!tex.LoadImage(data))
            {
                return null;
            }
            return tex;
        }
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    
    public MeshRenderer meshRenderer;
    private Material runtimeMaterial;
    Texture2D tex;
    public SpotMode[] spots;
    private Texture2D[] spotArmImages = new Texture2D[2];
    public int SpotObserverStreamIdx;
    public SpotCamera SpotObserverCameraIndex;
    public ControllerModel model;
    public enum Flip
    {
        None,
        FlipVertical,
        FlipHorizontal
    }

    public Flip flipMode = Flip.None;
    public override void OnNetworkSpawn()
    {
        if (IsClient)
        {
            runtimeMaterial = new Material(Shader.Find("Standard"));
            meshRenderer.sharedMaterial = runtimeMaterial;
            if (flipMode == Flip.FlipVertical)
                runtimeMaterial.mainTextureScale = new Vector2(1, -1); // Flip vertically
            else if (flipMode == Flip.FlipHorizontal)
            {
                runtimeMaterial.mainTextureScale = new Vector2(-1, 1); // Flip horizontally
            }
        }

    }
    void Update()
    {
        if(!IsSpawned) return;
        if (IsServer)
        {
            ExecuteServerUpdateTextures();
        }
        if (IsClient)
        {
            RequestGetUpdatedImageServer(model.attachedSpotMode);        
        }
    }
    /// <summary>
    /// Update the textures on the server by getting both arm images from SpotObserverClient
    /// </summary>
    private void ExecuteServerUpdateTextures()
    {
        int spotIndex = 0;
        foreach (SpotMode spot in spots) {
            SpotObserverClient spotObserverClient = spot.spotObserverClient;
            if (spotObserverClient != null && spotObserverClient.TryGetCameraFrame(SpotObserverStreamIdx, SpotObserverCameraIndex, out SpotObserverClient.CameraDepthFrame frame))
            {
                
                if (frame.ColorTexture == null)
                {
                    Debug.Log("Retrieved null texture from camera, not sending to client");
                }
                else
                {
                    spotArmImages[spotIndex] = frame.ColorTexture;
                }
            }
            spotIndex++;
        }
    }
    /// <summary>
    /// Send back texture message to certain client
    /// </summary>
    /// <param name="image"></param>
    /// <param name="clientRpcParams"></param>
    [ClientRpc]
    public void RequestUpdateTextureClientRPC(ImageNetwork image,ClientRpcParams clientRpcParams=default)
    {
        tex = image.GetTexture();
        runtimeMaterial.mainTexture = tex;

    }
    /// <summary>
    /// Get the spot index for syncing spotmodes between server and client
    /// </summary>
    /// <param name="spotMode"></param>
    /// <param name="index"></param>
    /// <returns></returns>
    public bool TryGetSpotIndex(SpotMode spotMode,out int index)
    {
        if (spots[0] == spotMode)
        {
            index = 0;
            return true;
        }
        if (spots[1]== spotMode)
        {
            index = 1; return true;
        }
        index = -1;
        return false;
    }
    /// <summary>
    /// Request that the texture client side be updated with the image retrieved from given SpotMode
    /// </summary>
    /// <param name="spotMode">robot to retrieve image from</param>
    public void RequestGetUpdatedImageServer(SpotMode spotMode)
    {
        if(TryGetSpotIndex(spotMode,out int spotIndex)){
            RequestGetUpdatedImageServerRPC(spotIndex, NetworkManager.LocalClientId);
        }
        return;
    }
    /// <summary>
    /// Get the image from the specific spot and send it back to client
    /// </summary>
    /// <param name="spotIndex"></param>
    /// <param name="clientOrigin"></param>
    [ServerRpc(RequireOwnership =false)]
    public void RequestGetUpdatedImageServerRPC(int spotIndex,ulong clientOrigin)
    {
        ClientRpcParams clientRpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new ulong[] { clientOrigin }
            }
        };
        ImageNetwork imageNetwork = new ImageNetwork();
        imageNetwork.SetTexture(spotArmImages[spotIndex]);
        RequestUpdateTextureClientRPC(imageNetwork, clientRpcParams);
        


    }
}
