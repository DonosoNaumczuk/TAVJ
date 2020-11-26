using System.Collections.Generic;
using System.Linq;
using Commons.Game;
using Commons.Networking;
using Commons.Utils;
using UnityEngine;
using UnityEngine.UI;
using Event = Commons.Networking.Event;
using Logger = Commons.Utils.Logger;

namespace Client
{
    public class Client : MonoBehaviour
    {
        public int port;
        public int serverPort;
        public string serverIp;
        public GameObject mainPlayerPrefab;
        public GameObject playerPrefab;
        public GameObject conciliationPrefab;
        private GameObject _conciliationObject;
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
        private int _lastInputUsedAsBaseForConciliation;

        private const int SnapshotsPerSecond = Constants.PacketsPerSecond;
        private const float SecondsToReceiveNextSnapshot = 1f / SnapshotsPerSecond;
        private const int InterpolationBufferSize = 3;

        private void Awake()
        {
            port = PlayerPrefs.GetInt(Menu.SceneManager.ClientPortKey);
            serverIp = PlayerPrefs.GetString(Menu.SceneManager.ServerIpKey);
            serverPort = PlayerPrefs.GetInt(Menu.SceneManager.ServerPortKey);
            PlayerPrefs.DeleteKey(Menu.SceneManager.ClientPortKey);
            PlayerPrefs.DeleteKey(Menu.SceneManager.ServerIpKey);
            PlayerPrefs.DeleteKey(Menu.SceneManager.ServerPortKey);
            _channel = new Channel(serverIp, port, serverPort);
            _players = new Dictionary<int, Player>();
            _snapshotBuffer = new Queue<Snapshot>();
            _currentSnapshot = null;
            _timeFromLastSnapshotInterpolation = 0f;
            _playerInput = new PlayerInput(forwardKey, backwardsKey, leftKey, rightKey, shootKey);
            _isConnected = false;
            _lastInputUsedAsBaseForConciliation = -1;
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

            // if (_isConnected)
            // {
            //     Debug.DrawRay(_players[_id].GameObject.transform.position + Vector3.up * 1.5f,
            //         _players[_id].GameObject.transform.forward * 100f, Color.red);
            // }
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

                _playerInput.AccumCooldown += Time.fixedDeltaTime;
                if (_playerInput.Shooting)
                {
                    Shoot();
                }

                _playerInput.DiscardShootsByTimeout(Time.fixedDeltaTime);
                if (_playerInput.HasShootsToSend())
                {
                    SendHits();
                }

                // if (_playerInput.CurrentInput.IsPressingSomething())
                // {
                //     HandleInputThroughPrediction();
                // }
                HandleInputThroughPrediction();

                _timeFromLastSnapshotInterpolation += Time.fixedDeltaTime;
                if (_snapshotBuffer.Count >= InterpolationBufferSize)
                {
                    _currentSnapshot = _snapshotBuffer.Dequeue();
                    if (_currentSnapshot.Contains(_id))
                    {
                        Logger.Log("ACK = " + _currentSnapshot.GetLastInputIdProcessed(_id), false, "magenta");
                    }
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
                        var lastInputIdProcessedByServer = _currentSnapshot.GetLastInputIdProcessed(_id);
                        _playerInput.DiscardInputsAlreadyProcessedByServer(lastInputIdProcessedByServer);
                        if (_lastInputUsedAsBaseForConciliation < lastInputIdProcessedByServer)
                        {
                            //Conciliate(); //TODO: Fixme!!!
                        }
                    }
                }
            }
        }

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
                case Event.Hit:
                    HandleHit(eventPacket);
                    break;
            }
        }

        private void HandleJoinResponse(Packet joinResponse)
        {
            var buffer = joinResponse.Buffer;
            _id = buffer.GetInt();
            CreateNewPlayerFromBuffer(_id, "MainPlayer", mainPlayerPrefab, buffer);
            _conciliationObject = Instantiate(conciliationPrefab, Vector3.zero, Quaternion.identity);
            _conciliationObject.name = "Conciliation_Object@Client_" + _id;
            _conciliationObject.tag = "ConciliationObject";
            for (var playersToAdd = buffer.GetInt(); playersToAdd > 0; playersToAdd--)
            {
                CreateNewPlayerFromBuffer(buffer.GetInt(), "Player", playerPrefab, buffer);
            }
            _isConnected = true;
        }

        private void CreateNewPlayerFromBuffer(int id, string objectTag, GameObject prefab, BitBuffer buffer)
        {
            var playerGameObject = Instantiate(prefab, Vector3.zero, Quaternion.identity);
            playerGameObject.tag = objectTag;
            if (objectTag == "Player")
            {
                playerGameObject.GetComponent<Info>().Id = id;
            }
            playerGameObject.name = "Player_" + id + "@Client_" + _id;
            var player = new Player(id, playerGameObject);
            player.DeserializeFromBuffer(buffer);
            _players.Add(id, player);
        }

        private void HandleJoinBroadcast(Packet joinBroadcast)
        {
            var buffer = joinBroadcast.Buffer;
            var joinedId = buffer.GetInt();
            CreateNewPlayerFromBuffer(joinedId, "Player", playerPrefab, buffer);
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
            if (_playerInput.CurrentInput.IsPressingSomething())
            {
                var player = _players[_id].GameObject;
                Logger.Log("P: Before Executing #" + _playerInput.BiggestInputIdQueuedToSend
                   + ", P = " + Printer.V3(player.transform.position)
                   + ", R = " + Printer.Q4(player.transform.rotation), false, "cyan");
                var (mov, rot) = PlayerManager.ProcessInput(_playerInput.CurrentInput, player, "P", "grey");
                Logger.Log("P: After  Executing #" + _playerInput.BiggestInputIdQueuedToSend
                   + ", mM = " + Printer.V3(mov)
                   + ", mR = " + Printer.V3(rot)
                   + ", P = " + Printer.V3(player.transform.position)
                   + ", R = " + Printer.Q4(player.transform.rotation), false, "cyan");
            }
            _players[_id].GameObject.GetComponent<CharacterController>().Move(Physics.gravity * Time.fixedDeltaTime);
            foreach (var uiObject in FindObjectsOfType<Text>())
            {
                if (uiObject.CompareTag("LifeText"))
                {
                    uiObject.text = _players[_id].Health + "%";
                }
                else if (uiObject.CompareTag("ScoreText"))
                {
                    uiObject.text = "Score: " + _players[_id].Score;
                }
            }
        }

        private void Conciliate()
        {
            _conciliationObject.transform.position = _currentSnapshot.GetPosition(_id);
            _conciliationObject.transform.rotation = _currentSnapshot.GetRotation(_id);
            Logger.Log("C: Transform took from #" + _currentSnapshot.GetLastInputIdProcessed(_id)
                + ", P = " + Printer.V3(_conciliationObject.transform.position) 
                + ", R = " + Printer.Q4(_conciliationObject.transform.rotation), false, "orange");
            foreach (var input in _playerInput.GetInputsNotProcessedByServer())
            {
                Logger.Log("C: Before Executing #" + input.Key 
                    + ", P = " + Printer.V3(_conciliationObject.transform.position) 
                    + ", R = " + Printer.Q4(_conciliationObject.transform.rotation), false, "yellow");
                var (mov, rot) = PlayerManager.ProcessInput(input.Value, _conciliationObject, "C", "orange");
                Logger.Log("C: After  Executing #" + input.Key
                    + ", mM = " + Printer.V3(mov)
                    + ", mR = " + Printer.V3(rot)                                
                    + ", P = " + Printer.V3(_conciliationObject.transform.position) 
                    + ", R = " + Printer.Q4(_conciliationObject.transform.rotation), false, "yellow");
            }
            Logger.Log("Conciliation: Finished"
                + ", P = " + Printer.V3(_conciliationObject.transform.position) 
                + ", R = " + Printer.Q4(_conciliationObject.transform.rotation), false, "orange");
            _players[_id].GameObject.transform.position = _conciliationObject.transform.position;
            _players[_id].GameObject.transform.rotation = _conciliationObject.transform.rotation;
            _lastInputUsedAsBaseForConciliation = _currentSnapshot.GetLastInputIdProcessed(_id);
        }

        private void Shoot()
        {
            Logger.Log("C: Shooting!", false);
            var origin = _players[_id].GameObject.transform.position + Vector3.up * 1.5f;
            var forward = _players[_id].GameObject.transform.forward;
            var hitted = Physics.Raycast(origin, forward, out var hit);
            if (hitted && hit.collider.gameObject.CompareTag("Player"))
            {
                Logger.Log("C: Player hitted", false, "blue");
                var shoot = new Shoot(_id, hit.collider.gameObject.GetComponent<Info>().Id);
                _playerInput.AddShoot(shoot);
            }
        }

        private void SendHits()
        {
            var packet = Packet.Obtain();
            EventSerializer.SerializeIntoBuffer(packet.Buffer, Event.Hit);
            var shoots = _playerInput.GetShootsNotProcessedByServer();
            packet.Buffer.PutInt(_id);
            packet.Buffer.PutInt(shoots.Count);
            foreach (var shoot in shoots)
            {
                packet.Buffer.PutInt(shoot.Key);
                packet.Buffer.PutInt(shoot.Value.Hitted);
            }
            packet.Buffer.Flush();
            Logger.Log("C: Sending hits", false, "cyan");
            _channel.Send(packet);
            packet.Free();
        }
        
        private void HandleHit(Packet hitPacket)
        {
            Logger.Log("C: Receiving hit response", false, "cyan");
            var buffer = hitPacket.Buffer;
            var lastShootIdProcessed = buffer.GetInt();
            _players[_id].IncrementScore(_playerInput.DiscardShootsAlreadyProcessedByServer(lastShootIdProcessed));
        }
    }
}