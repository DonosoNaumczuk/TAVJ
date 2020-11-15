using UnityEngine;

public class Entity
{
    private readonly int _id;
    private readonly GameObject _gameObject;

    private Transform _lastSnapshotTransform;

    public Entity(int id, GameObject gameObject)
    {
        _id = id;
        _gameObject = gameObject;
        _lastSnapshotTransform = gameObject.transform;
    }

    public int Id => _id;

    public GameObject GameObject => _gameObject;
    
    public Transform LastSnapshotTransform => _lastSnapshotTransform;

    public void DeserializeFromBuffer(BitBuffer buffer)
    {
        var transform = _gameObject.transform;
        var position = new Vector3(buffer.GetFloat(), buffer.GetFloat(), buffer.GetFloat());
        var rotation = new Quaternion(buffer.GetFloat(), buffer.GetFloat(), buffer.GetFloat(), buffer.GetFloat());
        transform.position = position;
        transform.rotation = rotation;
        _lastSnapshotTransform.position = position;
        _lastSnapshotTransform.rotation = rotation;
    }

    public void RefreshLastSnapshotTransform()
    {
        _lastSnapshotTransform = _gameObject.transform;
    }
}