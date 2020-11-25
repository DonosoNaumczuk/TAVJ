using System.Collections.Generic;
using System.Linq;
using Commons.Game;
using Commons.Networking;
using UnityEngine;
using Event = Commons.Networking.Event;

namespace Client
{
    public class Client : MonoBehaviour
    {
        public int port;
        public int serverPort;
        public string serverIp;
        public GameObject playerPrefab;
        public GameObject conciliationObject;
        public KeyCode forwardKey;
        public KeyCode backwardsKey;
        public KeyCode leftKey;
        public KeyCode rightKey;
        public KeyCode shootKey;

        private Channel _channel;
        private int _id;
        private bool _isConnected;
        private Dictionary<int, Player> _players;
        private Queue<Snapshot> _snapshotBuffer;
        private Snapshot _currentSnapshot;
        private float _timeFromLastSnapshotInterpolation;
        private PlayerInput _playerInput;

        private const int SnapshotsPerSecond = Constants.PacketsPerSecond;
        private const float SecondsToReceiveNextSnapshot = 1f / SnapshotsPerSecond;
        private const int InterpolationBufferSize = 3;

        private void Awake()
        {
            _channel = new Channel(serverIp, port, serverPort);
            _players = new Dictionary<int, Player>();
            _snapshotBuffer = new Queue<Snapshot>();
            _currentSnapshot = null;
            _timeFromLastSnapshotInterpolation = 0f;
            _playerInput = new PlayerInput(forwardKey, backwardsKey, leftKey, rightKey);
            _isConnected = false;
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
            if (_isConnected)
            {
                _playerInput.Read();
                if (_playerInput.HasInputsToSend())
                {
                    SendInputEvent();
                }

                if (_playerInput.CurrentInput.IsPressingSomething())
                {
                    HandleInputThroughPrediction();
                }

                _timeFromLastSnapshotInterpolation += Time.deltaTime;
                if (_snapshotBuffer.Count >= InterpolationBufferSize)
                {
                    _currentSnapshot = _snapshotBuffer.Dequeue();
                    _timeFromLastSnapshotInterpolation = 0f;
                    foreach (var player in _players.Values)
                    {
                        player.RefreshLastSnapshotTransform();
                    }
                }

                if (_currentSnapshot != null)
                {
                    InterpolateSnapshots();
                    if (_currentSnapshot.Contains(_id))
                    {
                        SetLastInputIdProcessed();
                        if (_lastInputUsedForConciliation < _currentSnapshot.GetLastInputIdProcessed(_id))
                        {
                            Conciliate();
                        }
                    }
                }
            }
        }

        private int _lastInputUsedForConciliation = -1;

        private void SendInputEvent()
        {
            var packet = GenerateInputPacket();
            _channel.Send(packet);
            packet.Free();
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
            CreateNewPlayerFromBuffer(_id, buffer);
            for (var playersToAdd = buffer.GetInt(); playersToAdd > 0; playersToAdd--)
            {
                CreateNewPlayerFromBuffer(buffer.GetInt(), buffer);
            }
            _isConnected = true;
        }

        private void CreateNewPlayerFromBuffer(int id, BitBuffer buffer)
        {
            var playerGameObject = Instantiate(playerPrefab, Vector3.up, Quaternion.identity);
            playerGameObject.name = "Player_" + id + "@Client_" + _id;
            var player = new Player(id, playerGameObject);
            player.DeserializeFromBuffer(buffer);
            _players.Add(id, player);
        }

        private void HandleJoinBroadcast(Packet joinBroadcast)
        {
            var buffer = joinBroadcast.Buffer;
            var joinedId = buffer.GetInt();
            CreateNewPlayerFromBuffer(joinedId, buffer);
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
            var time = Mathf.Clamp01(_timeFromLastSnapshotInterpolation / SecondsToReceiveNextSnapshot);
            foreach (var id in _currentSnapshot.Ids.Where(key => key != _id && _players.ContainsKey(key)))
            {
                var player = _players[id];
                player.GameObject.transform.position = Vector3.Lerp(player.LastSnapshotTransform.position,
                    _currentSnapshot.GetPosition(id), time);
                player.GameObject.transform.rotation = Quaternion.Lerp(player.LastSnapshotTransform.rotation,
                    _currentSnapshot.GetRotation(id), time);
            }
        }

        private void HandleInputThroughPrediction()
        {
            var player = _players[_id].GameObject;
            PlayerManager.ProcessInput(_playerInput.CurrentInput, player);
        }

        private void SetLastInputIdProcessed()
        {
            _playerInput.SetLastInputIdProcessedByServer(_currentSnapshot.GetLastInputIdProcessed(_id));
        }
        
        private void Conciliate()
        {
            conciliationObject.transform.position = _currentSnapshot.GetPosition(_id);
            conciliationObject.transform.rotation = _currentSnapshot.GetRotation(_id);
            foreach (var input in _playerInput.GetInputsNotProcessedByServer())
            {
                PlayerManager.ProcessInput(input.Value, conciliationObject);
            }
            // var positionThreshold = 1f;
            // var rotationThreshold = 1f;
            // if (Vector3.Distance(conciliationTransform.position, playerTransform.position) > positionThreshold 
            //     || Quaternion.Angle(conciliationTransform.rotation, playerTransform.rotation) > rotationThreshold)
            _players[_id].GameObject.transform.position = conciliationObject.transform.position;
            _players[_id].GameObject.transform.rotation = conciliationObject.transform.rotation;
            _lastInputUsedForConciliation = _currentSnapshot.GetLastInputIdProcessed(_id);
        }
    }
}