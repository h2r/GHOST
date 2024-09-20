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
using RosSharp.RosBridgeClient.MessageTypes.Geometry;
using RosSharp.RosBridgeClient.MessageTypes.Std;
using UnityEngine;
using System.Collections;

namespace RosSharp.RosBridgeClient
{
    public class ExtrinsicsSubscriber: UnitySubscriber<MessageTypes.Geometry.Pose>
    {
        public byte[] data;
        private bool isMessageReceived;
        public RosSharp.RosBridgeClient.MessageTypes.Geometry.Pose pose;
        // public Pose[] extrinsics_pos;


        protected override void Start()
        {
            base.Start();
        }

        protected override void ReceiveMessage(MessageTypes.Geometry.Pose pose_)
        {
            pose = pose_;
            isMessageReceived = true;
        }

        private void Update()
        {
            if (isMessageReceived)
                ProcessMessage();
        }

        private void ProcessMessage()
        {
        }
    }
}