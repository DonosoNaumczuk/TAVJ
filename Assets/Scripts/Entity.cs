using UnityEngine;

public class Entity
{
    private readonly Transform _transform;
    private readonly int _id;

    public Entity(int id, Transform transform)
    {
        _id = id;
        _transform = transform;
    }

    public Transform Transform => _transform;

    public int Id => _id;
}