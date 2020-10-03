using UnityEngine;

public class Client : MonoBehaviour
{
    public int port;
    public int serverPort;
    public string serverIp;

    private Channel channel;

    private void Awake()
    {
        channel = new Channel(serverIp, port, serverPort);
    }

    void Start()
    {
        SendJoinRequest();
    }

    private void Update()
    {
        var packet = channel.GetPacket();
        if (packet != null)
        {
            var eventType = packet.buffer.GetBit();
            var id = packet.buffer.GetInt();
            Debug.Log("Client[" + port + "]: Join response arrived! My id is " + id);
        }
    }

    private void OnDestroy()
    {
        channel.Disconnect();
    }

    private void SendJoinRequest()
    {
        var packet = GenerateJoinPacket();
        channel.Send(packet);
        packet.Free();
        Debug.Log("Client[" + port + "]: Join request already sent to server");
    }

    private Packet GenerateJoinPacket()
    {
        var packet = Packet.Obtain();
        packet.buffer.PutBit(Event.Join);
        packet.buffer.Flush();
        return packet;
    }
}