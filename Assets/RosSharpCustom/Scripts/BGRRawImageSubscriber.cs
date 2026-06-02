using UnityEngine;
using static SpotObserverClient;

public class BGRRawImageSubscriber : MonoBehaviour
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
            runtimeMaterial.mainTexture = texture2D;
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
