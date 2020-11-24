using System.Collections.Generic;
using System.Linq;
using Networking;
using UnityEngine;
using Event = Networking.Event;

namespace Server
{
    public class Server : MonoBehaviour
    {
        private const int SnapshotsPerSecond = 10;
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
            Logger.Log("Server: Receiving events", packet != null);
            while (packet != null)
            {
                HandleEventPacket(packet);
                packet.Free();
                packet = _channel.GetPacket();
                Logger.Log("Server: Events received and processed", packet == null);
            }
        }

        private void HandleEventPacket(Packet eventPacket)
        {
            var eventType = EventSerializer.DeserializeFromBuffer(eventPacket.Buffer);
            switch (eventType)
            {
                case Event.Join:
                    HandleJoinRequest(eventPacket);
                    Logger.Log("Server: Join event handled. I have " + _clients.Count + " clients now");
                    break;
                case Event.Input:
                    HandleInputEvent(eventPacket);
                    Logger.Log("Server: Input event handled");
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
            var input = new ClientInput(inputPacket.Buffer);
            _clients[id].UpdatePlayerInput(input);
        }

        private void HandleClientMovement()
        {
            foreach (var client in _clients)
            {
                var movement = Vector3.zero;
                var rotation = Vector3.zero;
                var animator = client.Entity.GetComponent<Animator>();

                if (client.ClientInput.IsPressingForwardKey)
                {
                    movement = client.Entity.transform.forward.normalized * 0.1f;
                    if (!animator.GetBool("WalkingForward"))
                    {
                        animator.SetBool("WalkingForward", true);
                        animator.SetBool("WalkingBackward", false);
                    }
                }
                else if (client.ClientInput.IsPressingBackwardsKey)
                {
                    movement = client.Entity.transform.forward.normalized * -0.1f;
                    if (!animator.GetBool("WalkingBackward"))
                    {
                        animator.SetBool("WalkingBackward", true);
                        animator.SetBool("WalkingForward", false);
                    }
                }
                else
                {
                    animator.SetBool("WalkingForward", false);
                    animator.SetBool("WalkingBackward", false);
                }

                if (client.ClientInput.IsPressingLeftKey)
                {
                    rotation = Vector3.down * 5f;
                    if (!animator.GetBool("RotatingLeft"))
                    {
                        animator.SetBool("RotatingLeft", true);
                        animator.SetBool("RotatingRight", false);
                    }
                }
                else if (client.ClientInput.IsPressingRightKey)
                {
                    rotation = Vector3.up * 5f;
                    if (!animator.GetBool("RotatingRight"))
                    {
                        animator.SetBool("RotatingRight", true);
                        animator.SetBool("RotatingLeft", false);
                    }
                }
                else
                {
                    animator.SetBool("RotatingLeft", false);
                    animator.SetBool("RotatingRight", false);
                }

                client.Entity.GetComponent<CharacterController>().Move(movement + Physics.gravity);
                client.Entity.transform.Rotate(rotation);
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