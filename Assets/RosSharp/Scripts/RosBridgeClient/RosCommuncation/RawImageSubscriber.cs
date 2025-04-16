/*
Modified from ImageSubscriber, which was written by
© Siemens AG, 2018
Author: Berkay Alp Cakal (berkay_alp.cakal.ct@siemens.com)

This script was created by Brown University

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

using System;
using RosSharp.RosBridgeClient.MessageTypes.Sensor;
using RosSharp.RosBridgeClient.MessageTypes.Std;
using UnityEngine;
using System.Collections;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;

namespace RosSharp.RosBridgeClient
{
    public class RawImageSubscriber : UnitySubscriber<MessageTypes.Sensor.Image>
    {
        private byte[] data;
        private bool isMessageReceived;
        private float[] globalData;            // final depth array returned
        private MessageTypes.Std.Time
                               timestamp_proc; // timestamp of message being processed
        public double timestamp_synced;        // timestamp of message that has been processed in nanoseconds, to return
        //private float[ , ] image_data_cbuffer; // buffer of previous frames' depth value
        //private float[] image_data_sum;        // sum of non-zero values in each pixel's history
        //private byte[] image_data_pixcount;    // number of non-zero values in each pixel's history
        //private int image_data_cbuffer_pos;    // tracker for spot in image_data_cbuffer
        //private int image_data_cbuffer_length; // number of frames to average depth over
        //private bool useDepthHistory;          // whether to use depth buffer for averaging, or just use current frame
        //private bool clearCbuf;                // whether to clear image_data_cbuffer at beginning of next message
        //private float turnDepthOnTime;         // when to turn depth history back 

        public bool printMessageReceiveRate;
        public bool printMessageProcRate;
        public bool compressed;
        private DateTime lastMessageRetrieved;
        private DateTime lastUpdate;
        private DateTime threadStart;
        private bool depthUpdated;
        private float farPlane;

        private Thread messageThread;

        public bool new_depth = false;

        /* struct to hold the recency of a depth value */
        private struct DepthInfo
        {
            public DepthInfo(float d, int r)
            {
                depth = d;
                recency = r;
            }

            public float depth { get; set; }
            public int recency { get; set; }
        };
        private DepthInfo[] depthHistory;


        protected override void Start()
        {
            base.Start();

            lastMessageRetrieved = DateTime.Now;
            lastUpdate = DateTime.Now;

            //image_data_cbuffer_pos = 0;

            ////how many frames to average depth over
            //image_data_cbuffer_length = 100;

            globalData = new float[1];
            timestamp_synced = 0d;
            //useDepthHistory = true;
            //clearCbuf = false;
            //turnDepthOnTime = 0f;
            depthUpdated = false;
        }

        protected override void ReceiveMessage(MessageTypes.Sensor.Image image)
        {
            double totalSeconds;
            if (printMessageReceiveRate)
            {
                totalSeconds = (DateTime.Now - lastMessageRetrieved).TotalSeconds;
                UnityEngine.Debug.Log("Time between messages: " + totalSeconds.ToString("0.0000") + " seconds, " + (1 / totalSeconds).ToString("0.00") + " FPS");
                lastMessageRetrieved = DateTime.Now;
            }

            data = image.data;
            timestamp_proc = image.header.stamp;

            // HACK -- width is actually the far plane
            farPlane = image.width;
            //farPlane = 4000f;

            // Thread the expensive processing of depth data
            if (messageThread == null || !messageThread.IsAlive)
            {
                threadStart = DateTime.Now;
                messageThread = new Thread(processMessageThreaded);
                messageThread.Start();
                depthUpdated = true;
            }
        }

        private void processMessageThreaded()
        {
            float depthVal;
            DateTime end;
            double totalSeconds;
            float[] image_data = new float[1];
            int incrate;

            if (compressed)
            {
                image_data = new float[data.Length];
                incrate = 1;
            }
            else
            {
                image_data = new float[data.Length / 2];
                incrate = 2;
            }

            //// Initialize the buffer containing past frame values for each pixel
            //if (image_data_cbuffer == null)
            //{
            //    image_data_cbuffer_pos = 0;
            //    image_data_cbuffer = new float[image_data.Length, image_data_cbuffer_length];
            //    image_data_pixcount = new byte[image_data.Length];
            //    image_data_sum = new float[image_data.Length];
            //}
            //else if (clearCbuf)
            //{
            //    image_data_cbuffer_pos = 0;
            //    Array.Clear(image_data_cbuffer, 0, image_data_cbuffer.Length);
            //    Array.Clear(image_data_pixcount, 0, image_data_pixcount.Length);
            //    Array.Clear(image_data_sum, 0, image_data_sum.Length);

            //    // Don't need to keep clearing buffer
            //    clearCbuf = false;
            //}

            byte[] bytes = new byte[2];
            int j = 0;
            for (int i = 0; i < data.Length; i += incrate)
            {
                if (compressed)
                {
                    //Decompress the RangeLinear compression
                    depthVal = data[i] / 255f;
                    depthVal *= farPlane;
                    depthVal /= 1000.0f;
                }
                else
                {
                    bytes[0] = data[i];
                    bytes[1] = data[i + 1];
                    depthVal = (BitConverter.ToUInt16(bytes)) / 1000.0f;
                }

                image_data[j] = depthVal;

                //if (useDepthHistory)
                //{
                //    // Update this index in pixcount and sum arrays
                //    if (image_data_cbuffer[j, image_data_cbuffer_pos] > 0f)
                //    {
                //        // Decrement from counts
                //        image_data_pixcount[j] -= 1;

                //        // subtract from sum
                //        image_data_sum[j] -= image_data_cbuffer[j, image_data_cbuffer_pos];
                //    }
                //    if (depthVal > 0f)
                //    {
                //        // Increment to counts
                //        image_data_pixcount[j] += 1;

                //        // Add to sum
                //        image_data_sum[j] += depthVal;
                //    }

                //    // Store this value in the buffer
                //    image_data_cbuffer[j, image_data_cbuffer_pos] = depthVal;
                //}

                j++;
            }


            //// Average depth frames
            //if (useDepthHistory) // If robot is not moving
            //{
            //    // Normalize
            //    for (j = 0; j < image_data.Length; j++)
            //    {
            //        if (image_data_pixcount[j] > 0)
            //        {
            //            image_data[j] = image_data_sum[j] / image_data_pixcount[j];
            //        }
            //    }
            //}

            //image_data_cbuffer_pos = (image_data_cbuffer_pos + 1) % image_data_cbuffer_length;

            // Copy into the final return array and timestamp
            globalData = new float[image_data.Length];
            Array.Copy(image_data, globalData, image_data.Length);

            new_depth = true;

            // Set timestamps
            timestamp_synced = timestamp_proc.secs + timestamp_proc.nsecs * 0.000000001;

            // Debugging info
            if (printMessageProcRate)
            {
                end = DateTime.Now;
                totalSeconds = (end - threadStart).TotalSeconds;
                UnityEngine.Debug.Log("Depth retrieval took " + totalSeconds.ToString("0.0000") + " seconds, " + (1 / totalSeconds).ToString("0.00") + " hz");
            }
        }


        private void Update()
        {
            //// UnityEngine.Debug.Log((DateTime.Now - lastUpdate).TotalSeconds);
            //lastUpdate = DateTime.Now;

            //// Turn depth history back on after the allotted time has passed
            //if (useDepthHistory == false && UnityEngine.Time.time > turnDepthOnTime)
            //{
            //    turnDepthOn();
            //}
        }

        public bool newDepthAvailable()
        {
            bool ret;
            ret = depthUpdated;
            depthUpdated = false;
            return ret;
        }

        // Get the most recently calculated depth array
        public float[] getDepthArr()
        {
            float[] copyOfData = new float[globalData.Length];
            Array.Copy(globalData, copyOfData, globalData.Length);
            return copyOfData;
        }

        // Turn off depth history for specified time
        public void pauseDepthHistory(float howLong)
        {
            //// If depth history already off, no need to clear cbuf
            //if (useDepthHistory)
            //{
            //    clearCbuf = true;
            //    useDepthHistory = false;
            //}

            //// Turn depth on after time has passed             
            //turnDepthOnTime = UnityEngine.Time.time + howLong;
        }

        // Turn depth info back on
        //private void turnDepthOn()
        //{
        //    useDepthHistory = true;
        //    clearCbuf = false;
        //}

        private void OnDestroy()
        {
            if (messageThread != null && messageThread.IsAlive)
            {
                messageThread.Abort();
            }
        }


    }
}