using System.Collections.Generic;
using System.Linq;
using Networking;
using UnityEngine;
using Event = Networking.Event;

namespace Client
{
    public class Client : MonoBehaviour
    {
        public int port;
        public int serverPort;
        public string serverIp;
        public GameObject entityPrefab;
        public KeyCode forwardKey;
        public KeyCode backwardsKey;
        public KeyCode leftKey;
        public KeyCode rightKey;
        public KeyCode shootKey;
        private PlayerInput _playerInput;

        private Channel _channel;
        private int _id;
        private Dictionary<int, Player> _entities;
        private Queue<Snapshot> _snapshotBuffer;
        private Snapshot _currentSnapshot;
        private float _timeFromLastSnapshotInterpolation;

        private const int SnapshotsPerSecond = 10;
        private const float SecondsToReceiveNextSnapshot = 1f / SnapshotsPerSecond;
        private const int InterpolationBufferSize = 3;

        private void Awake()
        {
            _channel = new Channel(serverIp, port, serverPort);
            _entities = new Dictionary<int, Player>();
            _snapshotBuffer = new Queue<Snapshot>();
            _currentSnapshot = null;
            _timeFromLastSnapshotInterpolation = 0f;
            _playerInput = new PlayerInput(forwardKey, backwardsKey, leftKey, rightKey);
        }

        void Start()
        {
            SendJoinRequest();
        }

        private void Update()
        {
            var packet = _channel.GetPacket();
            while (packet != null)
            {
                HandleEventPacket(packet);
                packet.Free();
                packet = _channel.GetPacket();
            }
        }

        private void FixedUpdate()
        {
            _playerInput.Read();
            SendInputEvent();
            HandleInputThroughPrediction();

            if (_snapshotBuffer.Count >= InterpolationBufferSize)
            {
                _currentSnapshot = _snapshotBuffer.Dequeue();
                _timeFromLastSnapshotInterpolation = 0f;
                foreach (var entity in _entities.Values)
                {
                    entity.RefreshLastSnapshotTransform();
                }
            }

            if (_currentSnapshot != null)
            {
                InterpolateSnapshots();
            }
        }

        private void SendInputEvent()
        {
            var packet = GenerateInputPacket();
            _channel.Send(packet);
            packet.Free();
            Logger.Log("Client[" + port + "]: Input already sent to server");
        }

        private Packet GenerateInputPacket()
        {
            var packet = Packet.Obtain();
            EventSerializer.SerializeIntoBuffer(packet.Buffer, Event.Input);
            packet.Buffer.PutInt(_id);
            _playerInput.SerializeIntoBuffer(packet.Buffer);
            packet.Buffer.Flush();
            return packet;
        }

        private void HandleEventPacket(Packet eventPacket)
        {
            var eventType = EventSerializer.DeserializeFromBuffer(eventPacket.Buffer);
            switch (eventType)
            {
                case Event.Join:
                    HandleJoinResponse(eventPacket);
                    break;
                case Event.JoinBroadcast:
                    HandleJoinBroadcast(eventPacket);
                    break;
                case Event.Snapshot:
                    HandleSnapshot(eventPacket);
                    break;
            }
        }

        private void HandleJoinResponse(Packet joinResponse)
        {
            var buffer = joinResponse.Buffer;
            _id = buffer.GetInt();
            Logger.Log("Client[" + port + "]: Join response arrived! My id is " + _id);
            CreateNewEntityFromBuffer(_id, buffer);
            for (var clientsToAdd = buffer.GetInt(); clientsToAdd > 0; clientsToAdd--)
            {
                CreateNewEntityFromBuffer(buffer.GetInt(), buffer);
            }
        }

        private void CreateNewEntityFromBuffer(int id, BitBuffer buffer)
        {
            var entityGameObject = Instantiate(entityPrefab, Vector3.up, Quaternion.identity);
            entityGameObject.name = "Player_" + id + "@Client_" + _id;
            var entity = new Player(id, entityGameObject);
            entity.DeserializeFromBuffer(buffer);
            _entities.Add(id, entity);
        }

        private void HandleJoinBroadcast(Packet joinBroadcast)
        {
            var buffer = joinBroadcast.Buffer;
            var joinedId = buffer.GetInt();
            CreateNewEntityFromBuffer(joinedId, buffer);
            Logger.Log("Client[" + port + "]: Client " + joinedId + " has joined!");
        }

        private void OnDestroy()
        {
            _channel.Disconnect();
        }

        private void SendJoinRequest()
        {
            var packet = GenerateJoinPacket();
            _channel.Send(packet);
            packet.Free();
            Logger.Log("Client[" + port + "]: Join request already sent to server");
        }

        private Packet GenerateJoinPacket()
        {
            var packet = Packet.Obtain();
            EventSerializer.SerializeIntoBuffer(packet.Buffer, Event.Join);
            packet.Buffer.Flush();
            return packet;
        }

        private void HandleSnapshot(Packet snapshotPacket)
        {
            _snapshotBuffer.Enqueue(new Snapshot(snapshotPacket.Buffer));
        }

        private void InterpolateSnapshots()
        {
            _timeFromLastSnapshotInterpolation += Time.deltaTime;
            var time = Mathf.Clamp01(_timeFromLastSnapshotInterpolation / SecondsToReceiveNextSnapshot);
            foreach (var id in _currentSnapshot.Ids.Where(key => _entities.ContainsKey(key)))
            {
                var entity = _entities[id];
                var positionRotationTuple = _currentSnapshot.GetPositionRotationTuple(id);
                entity.GameObject.transform.position = Vector3.Lerp(entity.LastSnapshotTransform.position,
                    positionRotationTuple.Item1, time);
                entity.GameObject.transform.rotation = Quaternion.Lerp(entity.LastSnapshotTransform.rotation,
                    positionRotationTuple.Item2, time);
            }
        }

        private void HandleInputThroughPrediction()
        {
            var client = _entities[_id];
            var movement = Vector3.zero;
            var rotation = Vector3.zero;

            if (_playerInput.IsPressingForwardKey)
            {
                movement = client.GameObject.transform.forward.normalized * 0.1f;
            }
            else if (_playerInput.IsPressingBackwardsKey)
            {
                movement = client.GameObject.transform.forward.normalized * -0.1f;
            }

            if (_playerInput.IsPressingLeftKey)
            {
                rotation = Vector3.down * 5f;
            }
            else if (_playerInput.IsPressingRightKey)
            {
                rotation = Vector3.up * 5f;
            }

            client.GameObject.GetComponent<CharacterController>().Move(movement + Physics.gravity);
            client.GameObject.transform.Rotate(rotation);
        }
    }
}