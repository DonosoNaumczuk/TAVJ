using System.Collections.Generic;
using System.Linq;
using Commons.Game;
using Commons.Networking;
using Commons.Utils;
using UnityEngine;
using Event = Commons.Networking.Event;
using Logger = Commons.Utils.Logger;

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
            port = PlayerPrefs.GetInt(Menu.SceneManager.ServerPortKey);
            PlayerPrefs.DeleteKey(Menu.SceneManager.ServerPortKey);
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

            _secondsSinceLastSnapshotSent += Time.fixedDeltaTime;
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
            var clientsThatChanged = _clients; //_clients.Where(client => client.Entity.transform.hasChanged).ToList();
            EventSerializer.SerializeIntoBuffer(buffer, Event.Snapshot);
            buffer.PutInt(clientsThatChanged.Count);
            foreach (var client in clientsThatChanged)
            {
                client.SerializeIntoBuffer(buffer);
                //client.Entity.transform.hasChanged = false;
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
                case Event.Hit:
                    HandleHitEvent(eventPacket);
                    break;
            }
        }

        private void HandleJoinRequest(Packet joinRequest)
        {
            var entity = Instantiate(entityPrefab, Vector3.zero, Quaternion.identity);
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
                    Logger.Log("S: Before Executing #" + client.ClientInput.CurrentInputId 
                        + ", P = " + Printer.V3(client.Entity.transform.position) 
                        + ", R = " + Printer.Q4(client.Entity.transform.rotation), false, "lime");
                    var (mov, rot) = PlayerManager.ProcessInput(client.ClientInput.CurrentInput, client.Entity, "S", "green");
                    Logger.Log("S: After  Executing #" + client.ClientInput.CurrentInputId 
                        + ", mM = " + Printer.V3(mov)
                        + ", mR = " + Printer.V3(rot)    
                        + ", P = " + Printer.V3(client.Entity.transform.position) 
                        + ", R = " + Printer.Q4(client.Entity.transform.rotation), false, "lime");
                }
                client.Entity.GetComponent<CharacterController>().Move(Physics.gravity * Time.fixedDeltaTime);
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

        private void HandleHitEvent(Packet hitPacket)
        {
            Logger.Log("S: Receiving hits", false, "lime");
            var buffer = hitPacket.Buffer;
            var shooter = buffer.GetInt();
            for (var shootsToProcess = buffer.GetInt(); shootsToProcess > 0; shootsToProcess--)
            {
                var shootId = buffer.GetInt();
                var hitted = buffer.GetInt();
                if (!_clients[shooter].ClientInput.ShootWasAlreadyProcessed(shootId))
                {
                    Logger.Log("Server: Client #" + shooter + " hitted Client #" + hitted + " in Shoot #" + shootId, false);
                    _clients[hitted].DecreaseHealth();
                    _clients[shooter].ClientInput.UpdateLastProcessedShoot(shootId);
                }
            }
            SendHitResponsePacket(shooter);
        }

        private void SendHitResponsePacket(int shooter)
        {
            Logger.Log("S: Sending las processed = " + _clients[shooter].ClientInput.LastShootProcessed, false, "lime");
            var packet = Packet.Obtain();
            var buffer = packet.Buffer;
            EventSerializer.SerializeIntoBuffer(buffer, Event.Hit);
            buffer.PutInt(_clients[shooter].ClientInput.LastShootProcessed);
            buffer.Flush();
            _channel.Send(packet, _clients[shooter].EndPoint);
            packet.Free();
        }
    }
}