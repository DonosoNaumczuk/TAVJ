using Commons.Networking;
using UnityEngine;

namespace Client
{
    public class Player
    {
        private readonly int _id;
        private readonly GameObject _gameObject;

        private Transform _lastSnapshotTransform;
        private int _health;
        private int _score;

        public Player(int id, GameObject gameObject)
        {
            _id = id;
            _gameObject = gameObject;
            _lastSnapshotTransform = gameObject.transform;
            _health = 100;
            _score = 0;
        }
        
        public void DeserializeFromBuffer(BitBuffer buffer)
        {
            var transform = _gameObject.transform;
            var _ = buffer.GetInt(); // Last input id processed, ignored!
            _health = buffer.GetInt();
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

        public int IncrementScore(int extraScore)
        {
            _score += extraScore;
            return _score;
        }

        public int Id => _id;

        public GameObject GameObject => _gameObject;
    
        public Transform LastSnapshotTransform => _lastSnapshotTransform;

        public int Health => _health;

        public int Score => _score;
    }
}