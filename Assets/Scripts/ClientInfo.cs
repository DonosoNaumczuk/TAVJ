using System.Net;

public class ClientInfo
{
    private int id;
    private IPEndPoint endPoint;

    public ClientInfo(int id, IPEndPoint endPoint)
    {
        this.id = id;
        this.endPoint = endPoint;
    }

    public int Id => id;

    public IPEndPoint EndPoint => endPoint;
}