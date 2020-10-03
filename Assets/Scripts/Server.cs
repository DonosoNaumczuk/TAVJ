using System.Collections.Generic;
using UnityEngine;

namespace Tests
{
    public class Server : MonoBehaviour
    {
        public int port;

        private Channel channel;
        private List<ClientInfo> clients;

        private void Awake()
        {
            channel = new Channel(port);
            clients = new List<ClientInfo>();
        }
        
        private void OnDestroy()
        {
            channel.Disconnect();
        }

        private void Update()
        {
            ReceiveEvents();
        }

        private void ReceiveEvents()
        {
            var packet = channel.GetPacket();
            if (packet != null)
            {
                HandleEventPacket(packet);
                packet.Free();
            }
        }

        private void HandleEventPacket(Packet eventPacket)
        {
            var eventType = eventPacket.buffer.GetBit();
            if (eventType == Event.Join)
            {
                HandleJoinRequest(eventPacket);
            }
        }

        private void HandleJoinRequest(Packet joinRequest)
        {
            var clientInfo = new ClientInfo(clients.Count, joinRequest.fromEndPoint);
            clients.Add(clientInfo);
            SendJoinedResponse(clientInfo);
            BroadcastNewJoin(clientInfo.Id);
        }
        
        private void SendJoinedResponse(ClientInfo clientInfo)
        {
            var packet = GenerateJoinedPacket(clientInfo.Id);
            channel.Send(packet, clientInfo.EndPoint);
            packet.Free();
        }

        private Packet GenerateJoinedPacket(int id)
        {
            var packet = Packet.Obtain();
            packet.buffer.PutBit(Event.Join);
            packet.buffer.PutInt(id);
            packet.buffer.Flush();
            return packet;
        }
        
        private void BroadcastNewJoin(int joinedClientId)
        {
            
        }
    }
}