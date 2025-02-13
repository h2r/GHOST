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
        public RawImageSubscriber associatedDepth;
        private MessageTypes.Sensor.Image[] imgBuffer;
        private uint bufferInd;
        private uint bufferLength;
        private byte[] imageData;
        private bool isMessageReceived;
        private bool freezeColor = false;
        private DateTime lastMessageRetrieved;
        private DateTime startSyncTime;
        public bool printRate;
        public bool printSyncTime;
        public bool printClosestTime;
        public double latest_time;
        private bool frameUpdated;

        protected override void Start()
        {
            base.Start();
            texture2D = new Texture2D(1, 1);
            meshRenderer.material = new Material(Shader.Find("Standard"));
            freezeColor = false;
            lastMessageRetrieved = DateTime.Now;

            // Keep last 5 color images, or just 1 if there is no depth
            if (associatedDepth == null || !associatedDepth.enabled)
            {
                bufferLength = 1;
            }
            else
            {
                bufferLength = 20;
            }
            bufferInd = 0;
            imgBuffer = new MessageTypes.Sensor.Image[bufferLength];
            frameUpdated = false;

        }

        private void Update()
        {
            // If we have a new color image and a new depth frame has been received (or there is no associated depth), update the color image
            if (isMessageReceived && (associatedDepth == null || !associatedDepth.enabled || associatedDepth.newDepthAvailable()))
            {
                startSyncTime = DateTime.Now;
                ProcessMessage();
            }
        }

        protected override void ReceiveMessage(MessageTypes.Sensor.Image image)
        {
            double totalSeconds;
            if (printRate)
            {
                totalSeconds = (DateTime.Now - lastMessageRetrieved).TotalSeconds;
                Debug.Log("Time between messages: " + totalSeconds.ToString("0.0000") + " seconds, " + (1 / totalSeconds).ToString("0.00") + " FPS");
            }

            imgBuffer[bufferInd] = image;
            bufferInd = (bufferInd + 1) % bufferLength;
            latest_time = image.header.stamp.secs + image.header.stamp.nsecs * 0.000000001;

            isMessageReceived = true;
            lastMessageRetrieved = DateTime.Now;
        }

        /// <summary>
        /// Process an image into the texture2D returned by this subscriber
        /// </summary>
        /// <param name="closestInd">The index of the image in the buffer that has the closest timestamp to the most recent depth as</param>
        private void ProcessMessage()
        {
            int closestInd;
            double totalSeconds;

            if (!freezeColor)
            {
                closestInd = getClosestTime();
                texture2D.LoadImage(imgBuffer[closestInd].data);
                texture2D.Apply();
                meshRenderer.material.SetTexture("_MainTex", texture2D);
            }

            if (printSyncTime)
            {
                totalSeconds = (DateTime.Now - startSyncTime).TotalSeconds;
                Debug.Log("Time to sync with depth: " + totalSeconds.ToString("0.0000") + " seconds, " + (1 / totalSeconds).ToString("0.00") + " hz");
            }

            frameUpdated = true;
            isMessageReceived = false;
        }

        private int getClosestTime()
        {
            double depthTime;   // When the depth data was received
            int closestInd;     // Closest index to return
            double closestTime; // Closest time found
            double disparity;   // Disparity between a given image's timestamp and the depth's timestamp
            double imgTime;     // The formatted timestamp for a given image (seconds plus nanoseconds)

            // Ignore all of this if there is no depth subscriber
            if (associatedDepth == null || !associatedDepth.enabled)
            {
                return 0;
            }

            // Initialize variables
            depthTime = associatedDepth.timestamp_synced;
            closestTime = 1000d;
            closestInd = 0;

            // Look at the timestamp of each image in the buffer
            for (int i = 0; i < imgBuffer.Length; i++)
            {
                if (imgBuffer[i] != null && imgBuffer[i].header != null)
                {
                    imgTime = imgBuffer[i].header.stamp.secs + imgBuffer[i].header.stamp.nsecs * 0.000000001;
                    disparity = Math.Abs(depthTime - imgTime);
                    if (disparity < closestTime)
                    {
                        closestInd = i;
                        closestTime = disparity;
                    }
                }
            }

            // If the user wants to see the closest depth to image time
            if (printClosestTime)
            {
                Debug.Log(closestTime);
            }

            return closestInd;
        }

        public void toggleFreeze()
        {
            freezeColor = !freezeColor;
        }

        public bool getFrameUpdated()
        {
            bool ret = frameUpdated;
            frameUpdated = false;
            return ret;
        }

    }
}