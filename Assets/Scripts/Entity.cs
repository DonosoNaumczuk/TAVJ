using UnityEngine;

public class Entity
{
    private readonly int _id;
    private readonly GameObject _gameObject;

    public Entity(int id, GameObject gameObject)
    {
        _id = id;
        _gameObject = gameObject;
    }

    public int Id => _id;

    public GameObject GameObject => _gameObject;

    public void DeserializeFromBuffer(BitBuffer buffer)
    {
        var transform = _gameObject.transform;
        var position = new Vector3(buffer.GetFloat(), buffer.GetFloat(), buffer.GetFloat());
        var rotation = new Quaternion(buffer.GetFloat(), buffer.GetFloat(), buffer.GetFloat(), buffer.GetFloat());
        transform.position = position;
        transform.rotation = rotation;
    }
}