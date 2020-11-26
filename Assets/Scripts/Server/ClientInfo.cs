using System.Net;
using Commons.Networking;
using Commons.Utils;
using UnityEngine;

namespace Server
{
    public class ClientInfo
    {
        private readonly int _id;
        private readonly IPEndPoint _endPoint;
        private readonly GameObject _entity;
        private readonly ClientInput _clientInput;

        private int _health;

        public ClientInfo(int id, IPEndPoint endPoint, GameObject entity)
        {
            _id = id;
            _endPoint = endPoint;
            _entity = entity;
            _clientInput = new ClientInput();
            _health = 100;
        }

        public int Id => _id;

        public IPEndPoint EndPoint => _endPoint;

        public GameObject Entity => _entity;
    
        public ClientInput ClientInput => _clientInput;

        public void SerializeIntoBuffer(BitBuffer buffer)
        {
            buffer.PutInt(_id);
            buffer.PutInt(_clientInput.LastInputIdProcessed);
            buffer.PutInt(_health);
            var transform = _entity.transform;
            var position = transform.position;
            buffer.PutFloat(position.x);
            buffer.PutFloat(position.y);
            buffer.PutFloat(position.z);
            var rotation = transform.rotation;
            buffer.PutFloat(rotation.x);
            buffer.PutFloat(rotation.y);
            buffer.PutFloat(rotation.z);
            buffer.PutFloat(rotation.w);
        }

        public void DecreaseHealth()
        {
            _health -= 20;
        }

        public bool IsAlive()
        {
            return _health > 0;
        }
    }
}