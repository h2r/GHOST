using UnityEngine;
using static SpotObserverClient;
using Unity.Netcode;
public class BGRRawImageSubscriberArms : NetworkBehaviour
{
    public MeshRenderer meshRenderer;

    public SpotObserverClient spotObserverClient;
    public SpotCamera SpotObserverCameraIndex;
    public int SpotObserverStreamIdx;

    private Texture2D texture2D;
    private Material runtimeMaterial;

    public enum Flip
    {
        None,
        FlipVertical,
        FlipHorizontal
    }

    public Flip flipMode = Flip.None;

    private void Start()
    {
        runtimeMaterial = new Material(Shader.Find("Standard"));
        meshRenderer.sharedMaterial = runtimeMaterial;
        if (flipMode == Flip.FlipVertical)
            runtimeMaterial.mainTextureScale = new Vector2(1, -1); // Flip vertically
        else if (flipMode == Flip.FlipHorizontal) {
            runtimeMaterial.mainTextureScale = new Vector2(-1, 1); // Flip horizontally
        }
    } 
    private void Update()
    {
        if (spotObserverClient != null && spotObserverClient.TryGetCameraFrame(SpotObserverStreamIdx, SpotObserverCameraIndex, out SpotObserverClient.CameraDepthFrame frame))
        {
            texture2D = frame.ColorTexture;
            
        }
    }

    private void OnDestroy()
    {
        if (runtimeMaterial != null)
        {
            Destroy(runtimeMaterial);
            runtimeMaterial = null;
        }
    }
}
