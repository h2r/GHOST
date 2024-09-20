/*
© Siemens AG, 2017-2019
Modified from JointStateSubscriber.cs by Dr. Martin Bischoff (martin.bischoff@siemens.com)

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

using System.Collections.Generic;

namespace RosSharp.RosBridgeClient
{
    public class IntrinsicsSubscriber : UnitySubscriber<MessageTypes.Std.Float32MultiArray>
    {
        public float[] intrinsics;
        public bool messageReceived;

        protected override void Start()
        {
            base.Start();
            intrinsics = null;
            messageReceived = false;
        }

        protected override void ReceiveMessage(MessageTypes.Std.Float32MultiArray message)
        {
            intrinsics = message.data;
            messageReceived = true;
        }
    }
}

