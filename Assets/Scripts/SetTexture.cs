using Meta.WitAi;
using RosSharp.RosBridgeClient;
using System.IO;
using System.Text;
using UnityEngine;

public class SetTexture : MonoBehaviour
{
    public JPEGImageSubscriber colorSubscriber; // ROS subscriber holding the color image
    public Material material;

    public bool use_saved_meshes = false;
    private Texture2D color_image;

    void Start()
    {
        if (colorSubscriber == null)
        {
            Debug.LogError("Color subscriber is not assigned.");
            return;
        }
        // Get the material and set the texture to the one provided by the color subscriber

        byte[] bytes;
        using (var stream = File.Open("Assets/PointClouds/Color_5.png", FileMode.Open))
        {
            using (var reader = new BinaryReader(stream, Encoding.UTF8, false))
            {
                bytes = reader.ReadBytes(int.MaxValue);
            }
        }

        color_image = new Texture2D(1, 1);
        color_image.LoadImage(bytes);
    }

    // Update is called once per frame
    void Update()
    {
        if (use_saved_meshes)
        {
            material.SetTexture("_MainTex", color_image);
        }
        else
        {
            material.SetTexture("_MainTex", colorSubscriber.texture2D);

        }
    }
}
