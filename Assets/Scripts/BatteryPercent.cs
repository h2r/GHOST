using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using RosSharp.RosBridgeClient;
using RosSharp.RosBridgeClient.MessageTypes.Std;
using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;
using UnityEngine.UI;
using TMPro;
                     

namespace RosSharp.RosBridgeClient
{
    public class FloatSubscriber : UnitySubscriber<MessageTypes.Std.Float64>
    {
       //rooter's
        private Image batteryBar;
        public TextMeshProUGUI batteryText;
        public double batteryLevel;

       //tusker's 
        private Image batteryBar1;  
        public TextMeshProUGUI batteryText1;
        public double batteryLevel1;

        protected override void Start()
        {
            base.Start(); 
            batteryBar = GetComponent<Image> ();
        }

        protected override void ReceiveMessage(MessageTypes.Std.Float64 message)
        {
            // messages recieved from the battery_percentage_states topic
            batteryLevel = message.data; // message contains a single field for the battery %age
        }

        //this is for tusker 
        /*
        protected override void ReceiveMessage1(MessageTypes.Std.Float64 message)
        {
            // messages recieved from the battery_percentage_states topic
            batteryLevel = message.data; // message contains a single field for the battery %age
        }
        */

        void Update()
        {
            batteryBar.fillAmount = (float) (batteryLevel / 100.0);
            batteryText.text = "" + batteryLevel + "%"; 
        }
    }
}