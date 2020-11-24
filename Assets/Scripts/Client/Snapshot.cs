using System.Collections.Generic;
using System.Linq;
using Networking;
using UnityEngine;

namespace Client
{
    public class Snapshot
    {
        private readonly Dictionary<int, (Vector3, Quaternion)> _transforms;

        public Snapshot(BitBuffer buffer)
        {
            _transforms = DeserializeFromBuffer(buffer);
        }
    
        private Dictionary<int, (Vector3, Quaternion)> DeserializeFromBuffer(BitBuffer buffer)
        {
            var transforms = new Dictionary<int, (Vector3, Quaternion)>();
            for (var clientsToProcess = buffer.GetInt(); clientsToProcess > 0; clientsToProcess--)
            {
                var id = buffer.GetInt();
                var position = new Vector3(buffer.GetFloat(), buffer.GetFloat(), buffer.GetFloat());
                var rotation = new Quaternion(buffer.GetFloat(), buffer.GetFloat(), buffer.GetFloat(), buffer.GetFloat());
                transforms[id] = (position, rotation);
            }
            return transforms;
        }
    
        public List<int> Ids => _transforms.Keys.ToList();
    
        public bool Contains(int id)
        {
            return _transforms.ContainsKey(id);
        }

        public (Vector3, Quaternion) GetPositionRotationTuple(int id)
        {
            return _transforms[id];
        }
    }
}
