using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using RpwsServerBridge.Entities;
using RpwsServerBridge.Tools;

namespace RpwsServerBridge.NetBase
{
    public delegate void GetPacketCallback(NetworkPacket pak);

    public class ServerNetworkClient
    {
        public const int MESSAGE_HEADER_SIZE = 54;
        public const int MAXIMUM_MESSAGE_PAYLOAD_SIZE = 16777216; //About 16 MB

        /// <summary>
        /// Connection to the end client.
        /// </summary>
        public Socket sock;

        /// <summary>
        /// The server class.
        /// </summary>
        public NetBase server;

        /// <summary>
        /// Buffer for incoming data.
        /// </summary>
        public byte[] buffer;

        /// <summary>
        /// Message ID send to the server to prevent replay attacks.
        /// </summary>
        public ulong target_send_secure_message_id;

        /// <summary>
        /// Message ID validated on incoming requests to prevent replay attacks.
        /// </summary>
        public ulong target_receive_secure_message_id;

        /// <summary>
        /// The message ID sent for replies to a message.
        /// </summary>
        public uint question_message_id;

        /// <summary>
        /// Stores callbacks for pending messages.
        /// </summary>
        private Dictionary<uint, GetPacketCallback> question_callbacks = new Dictionary<uint, GetPacketCallback>();

        /// <summary>
        /// Holds packets waiting to be sent.
        /// </summary>
        private Queue<NetworkPacket> pending_output_packets = new Queue<NetworkPacket>();

        /// <summary>
        /// While this is false, reply attack security is turned off.
        /// </summary>
        private bool has_handshook = false;

        /// <summary>
        /// Name used when logging.
        /// </summary>
        public string name;

        /// <summary>
        /// If this is a special registered client.
        /// </summary>
        public bool is_registered = false;

        /// <summary>
        /// Name of this registered special client if is_registered == true
        /// </summary>
        public string registered_name;

        public ServerNetworkClient(Socket sock, NetBase server, string name)
        {
            //Set variables.
            this.sock = sock;
            this.server = server;
            this.name = name;
            question_message_id = 1;
            server.rand = new Random();

            //Generate a random target secure message id.
            byte[] rand = new byte[8];
            server.rand.NextBytes(rand);
            target_send_secure_message_id = BitConverter.ToUInt64(rand, 0);
        }

        private void Log(string message, RpwsLogLevel logLevel = RpwsLogLevel.Debug)
        {
            server.Log(message, logLevel, name);
        }

        /// <summary>
        /// Checks if this client is connected. Thanks to https://stackoverflow.com/questions/722240/instantly-detect-client-disconnection-from-server-socket
        /// </summary>
        /// <param name="socket"></param>
        /// <returns></returns>
        public bool IsConnected()
        {
            try
            {
                return !(sock.Poll(1, SelectMode.SelectRead) && sock.Available == 0);
            }
            catch (SocketException) { return false; }
        }

        /// <summary>
        /// Disconnect this client.
        /// </summary>
        public void ForcefulDisconnect()
        {
            Console.WriteLine("Forcefully disconnecting channel...");
            try
            {
                sock.Close();
            } catch
            {

            }
            //Remove from any registrations this clienmt has.
            if(is_registered)
            {
                server.registered_clients[registered_name].Remove(this);
            }
        }

        /// <summary>
        /// Begin listening for new data.
        /// </summary>
        public void BeginListeningForData()
        {
            buffer = new byte[MESSAGE_HEADER_SIZE];
            sock.BeginReceive(buffer, 0, MESSAGE_HEADER_SIZE, SocketFlags.None, OnReceiveHeaderData, null);
        }

        /// <summary>
        /// Complete the handshake to open a fully secure connection from the client side.
        /// </summary>
        public void StartHandshakeAndWait()
        {
            //Create the actual request data.
            RequestEntities.HelloHandshake hello = new RequestEntities.HelloHandshake();
            hello.my_secure_request_id = target_send_secure_message_id;

            //Send the handshake request as a question, even on the notification channel.
            NetworkPacket question = new NetworkPacket(hello, RequestType.ClientHello, RequestStatusCode.Ok);

            //Send and wait for a reply.
            NetworkPacket reply = SendQuestionGetReplyAndWait(question);

            //Decode as our request.
            RequestEntities.HelloHandshake serverHello = reply.DecodeAsJson<RequestEntities.HelloHandshake>();

            //Set the server's secure request ID.
            target_receive_secure_message_id = serverHello.my_secure_request_id+1;

            //Set our flag to true.
            has_handshook = true;

            //Test the connection by sending a ping message.
            NetworkPacket ping = new NetworkPacket("Hello, server!", RequestType.Ping, RequestStatusCode.Ok);
            NetworkPacket ping_reply = SendQuestionGetReplyAndWait(ping);
            if (ping.DecodeAsString() != ping_reply.DecodeAsString())
                throw new Exception("Ping reply did not equal ping sent!");
        }

        /// <summary>
        /// Handles incoming handshakes on the server side.
        /// </summary>
        /// <param name="packet"></param>
        private void HandleIncomingHandshake(NetworkPacket packet)
        {
            Log("Got handshake request. Commiting...");
            //Read the incoming message.
            RequestEntities.HelloHandshake client_hello = packet.DecodeAsJson<RequestEntities.HelloHandshake>();

            //Set the value output value.
            target_receive_secure_message_id = client_hello.my_secure_request_id+1;

            //Create a response containing our secure request id.
            RequestEntities.HelloHandshake hello = new RequestEntities.HelloHandshake
            {
                my_secure_request_id = target_send_secure_message_id
            };

            //Set our flag.
            has_handshook = true;

            Log("Finished handhshake request. Replying...");

            //Send this.
            NetworkPacket hello_packet = new NetworkPacket(hello, RequestType.Answer, RequestStatusCode.Ok);
            packet.ReplyToQuestion(hello_packet);
        }

        /// <summary>
        /// Called when we get the header. The actual payload has not been read up to this point.
        /// </summary>
        /// <param name="ar"></param>
        private void OnReceiveHeaderData(IAsyncResult ar)
        {
            Log("[PACKET DECODE] Got header data for incoming packet.");

            //Finish getting data.
            int bytesRead;
            try
            {
                bytesRead = sock.EndReceive(ar);
            } catch
            {
                //We were disconnected. 
                CloseConnection(6);
                return;
            }
            
            //Make sure we got enough data.
            if(bytesRead != MESSAGE_HEADER_SIZE)
            {
                server.Log($"Got invalid packet! Header bytes read was too short or too long to decode the message header. Closing connection...");
                CloseConnection(1);
                return;
            }

            //Decode the header data.
            HeaderData header = PacketDecoder.ReadMessageHeader(buffer);

            //Make sure the payload is not too large.
            if(header.encrypted_size > MAXIMUM_MESSAGE_PAYLOAD_SIZE)
            {
                server.Log($"Got invalid packet! Incoming packet length was too large to recieve! Incoming length {header.encrypted_size} is greater than {MAXIMUM_MESSAGE_PAYLOAD_SIZE}. Closing connection...");
                CloseConnection(2);
                return;
            }

            //Now, wait for the payload to come.
            int payload_size = (int)header.encrypted_size;
            buffer = new byte[payload_size];
            Log($"Awaiting {payload_size} bytes from incoming packet payload. Packet secure request ID is {header.secure_request_id}.");
            sock.BeginReceive(buffer, 0, payload_size, SocketFlags.None, OnReceivePacketPayload, header);
        }

        /// <summary>
        /// Called when we get the packet data after we've read the header.
        /// </summary>
        /// <param name="ar"></param>
        private void OnReceivePacketPayload(IAsyncResult ar)
        {
            //Get the header data.
            HeaderData header = (HeaderData)ar.AsyncState;

            //Finish getting data.
            int bytesRead = sock.EndReceive(ar);

            Log($"Got {bytesRead} packet payload data for packet with secure request ID {header.secure_request_id}.");

            //Make sure we got enough data.
            if (bytesRead != header.encrypted_size)
            {
                server.Log($"Got invalid packet! Payload bytes read was too short or too long compared to the sent size. Closing connection...");
                CloseConnection(3);
                return;
            }

            //Decode the encrypted payload data.
            NetworkPacket packet = PacketDecoder.SecureDecodePacketPayload(buffer, header, target_receive_secure_message_id, server.config.users, out PacketCredentials creds, !has_handshook);
            target_receive_secure_message_id++;

            //Begin listening again.
            BeginListeningForData();

            Log($"Decoded packet with secure request ID of {header.secure_request_id} as {packet.ToDebugString()}. Handling...");

            //Handle packet
            OnMessage(packet);
        }

        private void CloseConnection(int code)
        {
            sock.Close();
            server.Log("Closed socket.");
        }

        /// <summary>
        /// Sends a packet to the other end. This call is thread safe.
        /// </summary>
        /// <param name="packet"></param>
        public void SendPacket(NetworkPacket packet)
        {
            Log($"Adding packet {packet.ToDebugString()} to queue...");
            //Add to the queue.
            lock(pending_output_packets)
            {
                pending_output_packets.Enqueue(packet);
            }
            Log($"Added packet {packet.ToDebugString()} to queue.");

            //Process queue
            ProcessQueue();
        }

        /// <summary>
        /// Process the message queue and send all packets. This call is thread safe.
        /// </summary>
        private void ProcessQueue()
        {
            lock (pending_output_packets)
            {
                while(pending_output_packets.Count > 0)
                {
                    //Pop
                    NetworkPacket packet = pending_output_packets.Dequeue();
                    Log($"Processing queued packet {packet.ToDebugString()} and sending it...");

                    //Encode the packet using the server creds and send it.
                    byte[] encoded_packet = PacketEncoder.EncodePacket(packet, server.config.server_creds, target_send_secure_message_id);
                    target_send_secure_message_id++;

                    //Transmit
                    lock (sock)
                        sock.Send(encoded_packet);

                    Log($"Sent queued packet {packet.ToDebugString()}.");
                }
            }
        }

        /// <summary>
        /// Sends a question and sends a callback when an answer arrives. This call is thread safe.
        /// </summary>
        /// <param name="packet"></param>
        /// <param name="callback"></param>
        public void SendQuestionGetReply(NetworkPacket packet, GetPacketCallback callback)
        {
            //Insert this into the dictonary.
            uint id;
            lock(question_callbacks)
            {
                id = question_message_id++;
                question_callbacks.Add(id, callback);
            }
            //Send the packet.
            packet.request_id = id;
            SendPacket(packet);
        }

        /// <summary>
        /// Sends a question and waits for a reply, hanging this thread. You are encouraged not to use this call. This call is thread safe.
        /// </summary>
        /// <param name="packet"></param>
        /// <param name="timeoutMs"></param>
        /// <returns></returns>
        public NetworkPacket SendQuestionGetReplyAndWait(NetworkPacket packet, int timeoutMs = 5000)
        {
            NetworkPacket reply = null;
            //Send the request.
            SendQuestionGetReply(packet, (NetworkPacket newReply) =>
            {
                //Set our reply.
                reply = newReply;
            });

            //Wait for the reply to be set, or to timeout.
            DateTime start = DateTime.UtcNow;
            DateTime end = start.AddMilliseconds(timeoutMs);

            while (reply == null && DateTime.UtcNow < end)
                Thread.Sleep(50);

            //Check if we've timed out.
            if (DateTime.UtcNow >= end)
                throw new Exception("Timed out while waiting for packet.");

            return reply;
        }

        /// <summary>
        /// Called when we get a full message.
        /// </summary>
        /// <param name="packet"></param>
        private void OnMessage(NetworkPacket packet)
        {
            try {
                //Set client
                packet.receiving_client = this;

                //Handle special requests.
                if (packet.type == RequestType.Ping)
                {
                    //Just respond with a ping.
                    packet.ReplyToQuestion(new NetworkPacket(packet.payload, RequestType.Answer, RequestStatusCode.Ok));
                    return;
                }
                if (packet.type == RequestType.ClientHello)
                {
                    //Handle handshake to open a secure connection.
                    HandleIncomingHandshake(packet);
                    return;
                }
                if(packet.type == RequestType.ClientRegister)
                {
                    //Register this client.
                    string type = Encoding.ASCII.GetString(packet.payload);
                    lock (server.registered_clients)
                    {
                        //Check if a list exists for this event.
                        if (!server.registered_clients.ContainsKey(type))
                            server.registered_clients.Add(type, new List<ServerNetworkClient>());
                        //Add socket
                        server.registered_clients[type].Add(this);
                    }
                    //Set our own values
                    registered_name = type;
                    is_registered = true;
                    //Respond
                    packet.ReplyToQuestion(new NetworkPacket(new byte[]{ 0x01 }, RequestType.Answer, RequestStatusCode.Ok));
                    return;
                }

                //If this is a notification packet, send to the server.
                if (packet.request_id == 0)
                {
                    server.OnNotificationMessage(packet, this);
                    return;
                }

                //This is either a question or an answer. Check by looking at the message type. According to the spec, an answer will always have it's own value.
                bool isQuestion = packet.type != RequestType.Answer;

                if (isQuestion)
                {
                    //Allow the server to handle this.
                    server.OnQuestionMessage(packet, this);
                    return;
                } else
                {
                    //This is an answer. Look it up in our database.
                    //Check if we have this request ID in our pending questions.
                    GetPacketCallback callback = null;

                    lock (question_callbacks)
                    {
                        if (question_callbacks.ContainsKey(packet.request_id))
                        {
                            callback = question_callbacks[packet.request_id];
                            //Remove
                            question_callbacks.Remove(packet.request_id);
                        }
                    }

                    //Check if we found it.
                    if (callback == null)
                    {
                        server.Log($"Unknown incoming question packet request ID {packet.request_id}! Ignoring packet...");
                        return;
                    }

                    //Call the code.
                    callback(packet);
                    return;
                }
            } catch (Exception ex)
            {
                //Unhandled error. 
                server.ErrorLog("Got error while handling incoming packet "+packet.ToDebugString(), ex);
            }
        }
    }
}
