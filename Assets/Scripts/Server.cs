using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Server : MonoBehaviour
{
    public int port;
    public GameObject cubeEntityPrefab;

    private Channel channel;
    private List<ClientInfo> clients;

    private void Awake()
    {
        channel = new Channel(port);
        clients = new List<ClientInfo>();
    }

    private void OnDestroy()
    {
        channel.Disconnect();
    }

    private void Update()
    {
        ReceiveEvents();
    }

    private void ReceiveEvents()
    {
        var packet = channel.GetPacket();
        Logger.Log("Server: Receiving events. I already have " + clients.Count + " clients", packet != null);
        while (packet != null)
        {
            HandleEventPacket(packet);
            packet.Free();
            packet = channel.GetPacket();
            Logger.Log("Server: Events received. I have " + clients.Count + " clients now", packet == null);
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
        var entity = Instantiate(cubeEntityPrefab, new Vector3(0, Random.Range(1.0f, 10.0f), 0), Quaternion.identity);
        var clientInfo = new ClientInfo(clients.Count, joinRequest.fromEndPoint, entity);
        clients.Add(clientInfo);
        SendJoinedResponse(clientInfo);
        BroadcastNewJoin(clientInfo.Id);
    }

    private void SendJoinedResponse(ClientInfo clientInfo)
    {
        var packet = GenerateJoinedPacket(clientInfo.Id);
        channel.Send(packet, clientInfo.EndPoint);
        packet.Free();
    }

    private Packet GenerateJoinedPacket(int id)
    {
        var packet = Packet.Obtain();
        var buffer = packet.buffer;
        EventSerializer.SerializeIntoBuffer(buffer, Event.Join);
        buffer.PutInt(id);
        buffer.Flush();
        return packet;
    }

    private void BroadcastNewJoin(int joinedClientId)
    {
        var packet = Packet.Obtain();
        var buffer = packet.buffer;
        EventSerializer.SerializeIntoBuffer(buffer, Event.JoinBroadcast);
        buffer.PutInt(joinedClientId);
        buffer.Flush();
        foreach (var client in clients.Where(client => client.Id != joinedClientId))
        {
            channel.Send(packet, client.EndPoint);
        }
        packet.Free();
    }
}