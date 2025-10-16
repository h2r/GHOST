using UnityEngine;
using RosSharp.RosBridgeClient.MessageTypes;

namespace RosSharp.RosBridgeClient
{
    [RequireComponent(typeof(RosConnector))]
    public class BGRRawImageSubscriber : UnitySubscriber<MessageTypes.Sensor.Image>
    {
        public MeshRenderer meshRenderer;

        private Texture2D texture2D;
        private byte[] imageData;
        private bool isMessageReceived;
        private int width = 1;
        private int height = 1;
        private int messageCount = 0;
        private const int checkThreshold = 10;

        public enum Flip
        {
            None,
            FlipVertical,
            FlipHorizontal
        }

        public Flip flipMode = Flip.None;

        protected override void Start()
        {
            base.Start();
            texture2D = new Texture2D(width, height, TextureFormat.RGB24, false);
            meshRenderer.material = new Material(Shader.Find("Standard"));
        }

        private void Update()
        {
            if (isMessageReceived)
                ProcessMessage();
        }

        protected override void ReceiveMessage(MessageTypes.Sensor.Image image)
        {
            width = (int)image.width;
            height = (int)image.height;

            imageData = image.data;
            isMessageReceived = true;
        }

        private void ProcessMessage()
        {
            // Convert BGR to RGB by swapping the first and third bytes for each pixel
            byte[] processedData = new byte[imageData.Length];
            width = Mathf.Max(1, width);
            height = Mathf.Max(1, height);

            if (flipMode == Flip.FlipVertical)
            {
                int rowSize = width * 3;
                for (int y = 0; y < height; y++)
                {
                    int srcRow = (height - 1 - y) * rowSize;
                    int destRow = y * rowSize;
                    for (int x = 0; x < rowSize; x += 3)
                    {
                        processedData[destRow + x] = imageData[srcRow + x + 2];     // B -> R position
                        processedData[destRow + x + 1] = imageData[srcRow + x + 1]; // G stays same
                        processedData[destRow + x + 2] = imageData[srcRow + x];     // R -> B position
                    }
                }
            }
            else if (flipMode == Flip.FlipHorizontal)
            {
                int rowSize = width * 3;
                for (int y = 0; y < height; y++)
                {
                    int srcRow = y * rowSize;
                    int destRow = y * rowSize;
                    for (int x = 0; x < width; x++)
                    {
                        int srcIndex = srcRow + (width - 1 - x) * 3;
                        int destIndex = destRow + x * 3;
                        processedData[destIndex] = imageData[srcIndex + 2];     // B -> R position
                        processedData[destIndex + 1] = imageData[srcIndex + 1]; // G stays same
                        processedData[destIndex + 2] = imageData[srcIndex];     // R -> B position
                    }
                }
            }
            else // No flip
                for (int i = 0; i < imageData.Length; i += 3)
                {
                    processedData[i] = imageData[i + 2];     // B -> R position
                    processedData[i + 1] = imageData[i + 1]; // G stays same
                    processedData[i + 2] = imageData[i];     // R -> B position
                }

            if (texture2D.width != width || texture2D.height != height)
            {
                texture2D.Reinitialize(width, height);
                Debug.Log("Texture size reinitialized to " + width + "x" + height);
            }

            // Load raw image data into the texture
            texture2D.LoadRawTextureData(processedData);
            texture2D.Apply();

            meshRenderer.material.mainTexture = texture2D;
            isMessageReceived = false;
        }
    }
}
