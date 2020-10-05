using System.Net;

public class ClientInfo
{
    private readonly int _id;
    private readonly IPEndPoint _endPoint;

    public ClientInfo(int id, IPEndPoint endPoint)
    {
        _id = id;
        _endPoint = endPoint;
    }

    public int Id => _id;

    public IPEndPoint EndPoint => _endPoint;
}