using Newtonsoft.Json;
using RpwsServerBridge.NetBase;
using System;
using System.Collections.Generic;
using System.Text;

namespace RpwsServerBridge.Entities
{
    public class NetworkPacket
    {
        public NetworkPacket()
        {

        }

        public NetworkPacket(byte[] payload, RequestType type, RequestStatusCode status = RequestStatusCode.Ok)
        {
            this.payload = payload;
            this.type = type;
            this.status = status;
        }

        public NetworkPacket(string payload, RequestType type, RequestStatusCode status = RequestStatusCode.Ok)
        {
            this.payload = Encoding.UTF8.GetBytes(payload);
            this.type = type;
            this.status = status;
        }

        public NetworkPacket(object payload, RequestType type, RequestStatusCode status = RequestStatusCode.Ok)
        {
            this.payload = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(payload));
            this.type = type;
            this.status = status;
        }

        /// <summary>
        /// HTTP-like status code
        /// </summary>
        /// 
        public RequestStatusCode status;
        /// <summary>
        /// The request ID for questions and answers. Echoed back from the recipent
        /// </summary>
        public uint request_id;

        /// <summary>
        /// The type of request this is. Is not sent on answers.
        /// </summary>
        public RequestType type;

        /// <summary>
        /// The actual payload data.
        /// </summary>
        public byte[] payload;

        /// <summary>
        /// Creds used for this packet.
        /// </summary>
        public PacketCredentials creds;

        /// <summary>
        /// The secure request ID used with this.
        /// </summary>
        public ulong secure_request_id;


        /* Used by the clients */

        /// <summary>
        /// The client that received this packet.
        /// </summary>
        public ServerNetworkClient receiving_client;

        /// <summary>
        /// Reads the payload as json.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T DecodeAsJson<T>()
        {
            string jsonString = DecodeAsString();
            return JsonConvert.DeserializeObject<T>(jsonString);
        }

        /// <summary>
        /// Reads the payload as a string
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public string DecodeAsString()
        {
            return Encoding.UTF8.GetString(payload);
        }

        /// <summary>
        /// Creates a debug string.
        /// </summary>
        /// <returns></returns>
        public string ToDebugString()
        {
            return $"[status={status.ToString().ToUpper()}, type={type.ToString().ToUpper()}, request_id={request_id.ToString()}, secure_request_id={secure_request_id.ToString()}, packet_size={payload.Length.ToString()}]";
        }

        /// <summary>
        /// Responds to an answer and autofills details.
        /// </summary>
        /// <param name="p">Packet to send.</param>
        public void ReplyToQuestion(NetworkPacket p)
        {
            p.request_id = request_id;
            p.type = RequestType.Answer;

            //Send
            receiving_client.SendPacket(p);
        }

        /// <summary>
        /// Responds to an answer and autofills details.
        /// </summary>
        /// <param name="p">Packet to send.</param>
        public void ReplyToQuestion(object o)
        {
            ReplyToQuestion(new NetworkPacket(o, RequestType.Answer, RequestStatusCode.Ok));
        }
    }
}
