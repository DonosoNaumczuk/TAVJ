using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Server : MonoBehaviour
{
    public int port;
    public GameObject cubeEntityPrefab;

    private Channel _channel;
    private List<ClientInfo> _clients;

    private void Awake()
    {
        _channel = new Channel(port);
        _clients = new List<ClientInfo>();
    }

    private void OnDestroy()
    {
        _channel.Disconnect();
    }

    private void Update()
    {
        ReceiveEvents();
    }

    private void ReceiveEvents()
    {
        var packet = _channel.GetPacket();
        Logger.Log("Server: Receiving events. I already have " + _clients.Count + " clients", packet != null);
        while (packet != null)
        {
            HandleEventPacket(packet);
            packet.Free();
            packet = _channel.GetPacket();
            Logger.Log("Server: Events received. I have " + _clients.Count + " clients now", packet == null);
        }
    }

    private void HandleEventPacket(Packet eventPacket)
    {
        var eventType = EventSerializer.DeserializeFromBuffer(eventPacket.buffer);
        if (eventType == Event.Join)
        {
            HandleJoinRequest(eventPacket);
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