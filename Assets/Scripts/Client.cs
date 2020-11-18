using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Client : MonoBehaviour
{
    public int port;
    public int serverPort;
    public string serverIp;
    public GameObject cubeEntityPrefab;
    public KeyCode jumpKey;

    private Channel _channel;
    private int _id;
    private Dictionary<int, Entity> _entities;
    private Queue<Snapshot> _snapshotBuffer;
    private Snapshot _currentSnapshot;
    private float _timeFromLastSnapshotInterpolation;
    
    private const int SnapshotsPerSecond = 10;
    private const float SecondsToReceiveNextSnapshot = 1f / SnapshotsPerSecond;
    private const int InterpolationBufferSize = 3;

    private void Awake()
    {
        _channel = new Channel(serverIp, port, serverPort);
        _entities = new Dictionary<int, Entity>();
        _snapshotBuffer = new Queue<Snapshot>();
        _currentSnapshot = null;
        _timeFromLastSnapshotInterpolation = 0f;
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

        if (Input.GetKey(jumpKey))
        {
            SendInputEvent();
        }

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
        //TODO: now we assume input is jump, then we must specify the input type (jump, shoot, movement, etc.)
        var packet = Packet.Obtain();
        EventSerializer.SerializeIntoBuffer(packet.buffer, Event.Input);
        packet.buffer.PutInt(_id); //TODO: this doesn't look too safe as auth/id system...
        packet.buffer.Flush();
        return packet;
    }
    
    private void HandleEventPacket(Packet eventPacket)
    {
        var eventType = EventSerializer.DeserializeFromBuffer(eventPacket.buffer);
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
        var buffer = joinResponse.buffer;
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
        var entityGameObject = Instantiate(cubeEntityPrefab, Vector3.zero, Quaternion.identity);
        entityGameObject.name = "Player_" + id + "@Client_" + _id;
        var entity = new Entity(id, entityGameObject);
        entity.DeserializeFromBuffer(buffer);
        _entities.Add(id, entity);
    }
    
    private void HandleJoinBroadcast(Packet joinBroadcast)
    {
        var buffer = joinBroadcast.buffer;
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
        EventSerializer.SerializeIntoBuffer(packet.buffer, Event.Join);
        packet.buffer.Flush();
        return packet;
    }

    private void HandleSnapshot(Packet snapshotPacket)
    {
        _snapshotBuffer.Enqueue(new Snapshot(snapshotPacket.buffer));
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
}