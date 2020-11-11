using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Server : MonoBehaviour
{
    private const float JumpForceModule = 1.5f;
    private const int SnapshotsPerSecond = 3;
    private const float SecondsToSendNextSnapshot = 1f / SnapshotsPerSecond;
    
    public int port;
    public GameObject cubeEntityPrefab;

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
        var buffer = packet.buffer;
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
        var eventType = EventSerializer.DeserializeFromBuffer(eventPacket.buffer);
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
        var entityFirstPosition = new Vector3(0, Random.Range(1.0f, 10.0f), 0);
        var entity = Instantiate(cubeEntityPrefab, entityFirstPosition, Quaternion.identity);
        var clientInfo = new ClientInfo(_clients.Count, joinRequest.fromEndPoint, entity);
        _clients.Add(clientInfo);
        SendJoinedResponse(clientInfo);
        BroadcastNewJoin(clientInfo.Id);
    }
    
    private void HandleInputEvent(Packet inputPacket)
    {
        var id = inputPacket.buffer.GetInt();
        _clients[id].Entity.GetComponent<Rigidbody>()
            .AddForceAtPosition(JumpForceModule * Vector3.up, Vector3.zero, ForceMode.Impulse);
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
        var buffer = packet.buffer;
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
        var buffer = packet.buffer;
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