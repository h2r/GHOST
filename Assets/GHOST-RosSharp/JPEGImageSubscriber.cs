/*
© Siemens AG, 2017-2018
Author: Dr. Martin Bischoff (martin.bischoff@siemens.com)

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at
<http://www.apache.org/licenses/LICENSE-2.0>.
Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
*/

using UnityEngine;
using UnityEngine.UI;
using System;

namespace RosSharp.RosBridgeClient
{                                            
    [RequireComponent(typeof(RosConnector))]
    public class JPEGImageSubscriber : UnitySubscriber<MessageTypes.Sensor.Image>
    {
        public MeshRenderer meshRenderer;

        public Texture2D texture2D;
        private byte[] imageData;
        private bool isMessageReceived;
        

        protected override void Start()
        {
			base.Start();
            texture2D = new Texture2D(1, 1);
            meshRenderer.material = new Material(Shader.Find("Standard"));
        }

        private void Update()
        {
            // If we have a new color image and a new depth frame has been received (or there is no associated depth), update the color image
            if (isMessageReceived)
            {
                ProcessMessage();
            }
        }

        protected override void ReceiveMessage(MessageTypes.Sensor.Image image)
        {
            imageData = image.data;
            isMessageReceived = true;
        }

        /// <summary>
        /// Process an image into the texture2D returned by this subscriber
        /// </summary>
        /// <param name="closestInd">The index of the image in the buffer that has the closest timestamp to the most recent depth as</param>
        private void ProcessMessage()
        {
            texture2D.LoadImage(imageData);
            texture2D.Apply();
            meshRenderer.material.SetTexture("_MainTex", texture2D);
            isMessageReceived = false;
        }
    }
}

