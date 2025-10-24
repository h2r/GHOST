using UnityEngine;
using static SpotObserverClient;

public class BGRRawImageSubscriber : MonoBehaviour
{
    public MeshRenderer meshRenderer;

    public SpotObserverClient spotObserverClient;
    public SpotCamera SpotObserverCameraIndex;
    public int SpotObserverStreamIdx;

    private Texture2D texture2D;

    public enum Flip
    {
        None,
        FlipVertical,
        FlipHorizontal
    }

    public Flip flipMode = Flip.None;

    private void Start()
    {
        meshRenderer.material = new Material(Shader.Find("Standard"));
        if (flipMode == Flip.FlipVertical)
            meshRenderer.material.mainTextureScale = new Vector2(1, -1); // Flip vertically
        else if (flipMode == Flip.FlipHorizontal) {
            meshRenderer.material.mainTextureScale = new Vector2(-1, 1); // Flip horizontally
        }
    }

    private void Update()
    {
        (texture2D, _) = spotObserverClient.GetCameraFeeds(SpotObserverStreamIdx, SpotObserverCameraIndex);
        meshRenderer.material.mainTexture = texture2D;
    }
}

