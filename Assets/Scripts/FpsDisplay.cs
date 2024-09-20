using UnityEngine;
using TMPro;
using RosSharp.RosBridgeClient;

public class FpsDisplay : MonoBehaviour
{
    public TextMeshProUGUI FPSText;
    public TextMeshProUGUI DepthText; 
    private float pollingTime = 1f;
    private float time;
    private int frameCount;
    private int messageCount;       // counts the number of depth messages
    public RawImageSubscriber depthSubscriber; // reference to the subscriber


     
    // Update is called once per frame
    void Update()
    {
        time += Time.deltaTime;
        
        //Should we update the message count based on state of the subscriber? 
        if (depthSubscriber != null)
        {
            messageCount++;
        }

        if (time >= pollingTime)
        {
            int frameRate = Mathf.RoundToInt(frameCount/time);
            int mesgRate = Mathf.RoundToInt(messageCount/time);
            FPSText.text = frameRate.ToString() + "FPS";
            DepthText.text = mesgRate.ToString().ToString() + "DPS"; 
            time -= pollingTime;
            frameCount = 0; 
            messageCount = 0;
        }
        frameCount++;
    }

}
