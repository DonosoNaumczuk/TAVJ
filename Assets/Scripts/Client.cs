using System.Collections.Generic;
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

    private void Awake()
    {
        _channel = new Channel(serverIp, port, serverPort);
        _entities = new Dictionary<int, Entity>();
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
        var gameObject = Instantiate(cubeEntityPrefab, Vector3.zero, Quaternion.identity);
        var entity = new Entity(id, gameObject);
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
        var buffer = snapshotPacket.buffer;
        for (var clientsToProcess = buffer.GetInt(); clientsToProcess > 0; clientsToProcess--)
        {
            var id = buffer.GetInt();
            if (_entities.ContainsKey(id)) //TODO: if not? consumes buffer ignoring its transform?
            {
                _entities[id].DeserializeFromBuffer(buffer);
            }
        }
    }
}