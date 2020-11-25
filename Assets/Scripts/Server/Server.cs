using System.Collections.Generic;
using System.Linq;
using Commons.Game;
using Commons.Networking;
using UnityEngine;
using Event = Commons.Networking.Event;

namespace Server
{
    public class Server : MonoBehaviour
    {
        private const int SnapshotsPerSecond = Constants.PacketsPerSecond;
        private const float SecondsToSendNextSnapshot = 1f / SnapshotsPerSecond;

        public int port;
        public GameObject entityPrefab;

        private Channel _channel;
        private List<ClientInfo> _clients;
        private float _secondsSinceLastSnapshotSent;

        private void Awake()
        {
            _channel = new Channel(port);
            _clients = new List<ClientInfo>();
            _secondsSinceLastSnapshotSent = 0f;
        }

        private void OnDestroy()
        {
            _channel.Disconnect();
        }

        private void Update()
        {
            ReceiveEvents();
        }

        private void FixedUpdate()
        {
            HandleClientMovement();

            _secondsSinceLastSnapshotSent += Time.deltaTime;
            if (_secondsSinceLastSnapshotSent >= SecondsToSendNextSnapshot)
            {
                SendSnapshots();
                _secondsSinceLastSnapshotSent = 0f;
            }
        }

        private void SendSnapshots()
        {
            var snapshotPacket = GenerateSnapshotPacket();
            foreach (var client in _clients)
            {
                _channel.Send(snapshotPacket, client.EndPoint);
            }

            snapshotPacket.Free();
        }

        private Packet GenerateSnapshotPacket()
        {
            var packet = Packet.Obtain();
            var buffer = packet.Buffer;
            var clientsThatChanged = _clients.Where(client => client.Entity.transform.hasChanged).ToList();
            EventSerializer.SerializeIntoBuffer(buffer, Event.Snapshot);
            buffer.PutInt(clientsThatChanged.Count);
            foreach (var client in clientsThatChanged)
            {
                client.SerializeIntoBuffer(buffer);
                client.Entity.transform.hasChanged = false;
            }

            buffer.Flush();
            return packet;
        }

        private void ReceiveEvents()
        {
            var packet = _channel.GetPacket();
            while (packet != null)
            {
                HandleEventPacket(packet);
                packet.Free();
                packet = _channel.GetPacket();
            }
        }

        private void HandleEventPacket(Packet eventPacket)
        {
            var eventType = EventSerializer.DeserializeFromBuffer(eventPacket.Buffer);
            switch (eventType)
            {
                case Event.Join:
                    HandleJoinRequest(eventPacket);
                    break;
                case Event.Input:
                    HandleInputEvent(eventPacket);
                    break;
            }
        }

        private void HandleJoinRequest(Packet joinRequest)
        {
            var entity = Instantiate(entityPrefab, Vector3.up, Quaternion.identity);
            entity.name = "Player_" + _clients.Count + "@Server";
            var clientInfo = new ClientInfo(_clients.Count, joinRequest.FromEndPoint, entity);
            _clients.Add(clientInfo);
            SendJoinedResponse(clientInfo);
            BroadcastNewJoin(clientInfo.Id);
        }

        private void HandleInputEvent(Packet inputPacket)
        {
            var id = inputPacket.Buffer.GetInt();
            _clients[id].ClientInput.AddFromBuffer(inputPacket.Buffer);
        }

        private void HandleClientMovement()
        {
            foreach (var client in _clients)
            {
                while (client.ClientInput.NextInput())
                {
                    PlayerManager.ProcessInput(client.ClientInput.CurrentInput, client.Entity);
                }
            }
        }

        private void SendJoinedResponse(ClientInfo clientInfo)
        {
            var packet = GenerateJoinedPacket(clientInfo.Id);
            _channel.Send(packet, clientInfo.EndPoint);
            packet.Free();
        }

        private Packet GenerateJoinedPacket(int id)
        {
            var packet = Packet.Obtain();
            var buffer = packet.Buffer;
            EventSerializer.SerializeIntoBuffer(buffer, Event.Join);
            _clients[id].SerializeIntoBuffer(buffer);
            buffer.PutInt(_clients.Count - 1);
            foreach (var clientInfo in _clients.Where(clientInfo => clientInfo.Id != id))
            {
                clientInfo.SerializeIntoBuffer(buffer);
            }

            buffer.Flush();
            return packet;
        }

        private void BroadcastNewJoin(int joinedClientId)
        {
            var packet = Packet.Obtain();
            var buffer = packet.Buffer;
            EventSerializer.SerializeIntoBuffer(buffer, Event.JoinBroadcast);
            _clients[joinedClientId].SerializeIntoBuffer(buffer);
            buffer.Flush();
            foreach (var client in _clients.Where(client => client.Id != joinedClientId))
            {
                _channel.Send(packet, client.EndPoint);
            }

            packet.Free();
        }
    }
}