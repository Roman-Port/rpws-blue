using System;
using System.Collections.Generic;
using System.Text;
using RpwsServerBridge;
using RpwsServerBridge.Client;
using RpwsServerBridge.Entities;
using System.Net;
using RpwsServerBridge.NetBase;
using LibRpws2;
using Newtonsoft.Json;
using LibRpws2.QuestionArgs;

namespace LibRpws2
{
    public delegate void OnSubscribedQuestionEvent(NetworkPacket p);

    public class LibRpwsClient : RpwsBridgeClient
    {
        /// <summary>
        /// Start the client and connect.
        /// </summary>
        public LibRpwsClient(NetConfigServer config)
        {
            //Set variables
            this.config = config;
            name = "CLIENT";

            //Create connections.
            ReconnectToServer();
        }

        /// <summary>
        /// Send a JSON question and get a JSON question back.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="data"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public T SendQuestionGetReplyWait<T>(object data, RpwsNetQuestion type)
        {
            //Convert the type into a type to send over the network.
            RequestType net_type = (RequestType)((int)type + 20);

            //Create a packet and send it.
            NetworkPacket reply = SendQuestionGetReplyWait(new NetworkPacket(data, net_type, RequestStatusCode.Ok));

            //Deserialize
            return reply.DecodeAsJson<T>();
        }

        /// <summary>
        /// Hold data about subscriptions to events.
        /// </summary>
        private Dictionary<RpwsNetQuestion, OnSubscribedQuestionEvent> subscribed_questions = new Dictionary<RpwsNetQuestion, OnSubscribedQuestionEvent>();

        /// <summary>
        /// The callback you add will be called when this type of question comes in.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="callback"></param>
        public void SubscribeToQuestionEvent(RpwsNetQuestion type, OnSubscribedQuestionEvent callback)
        {
            subscribed_questions.Add(type, callback);
        }

        /// <summary>
        /// Called on incoming questions.
        /// </summary>
        /// <param name="packet"></param>
        /// <param name="client"></param>
        public override void OnQuestionMessage(NetworkPacket packet, ServerNetworkClient client)
        {
            //Convert from net type to our type.
            RpwsNetQuestion type = (RpwsNetQuestion)((int)packet.type - 20);

            //Check if we have any subscriptions.
            if (!subscribed_questions.ContainsKey(type))
            {
                //Ignore
                Log("Unknown incoming LibRPWS type " + type.ToString() + ". Ignroing.", RpwsLogLevel.Error);
                return;
            }
            
            //Call the callback.
            OnSubscribedQuestionEvent callback = subscribed_questions[type];
            callback(packet);
        }
    }
}
