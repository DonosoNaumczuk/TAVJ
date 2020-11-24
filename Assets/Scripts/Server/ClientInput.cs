using System.Collections.Generic;
using Networking;

namespace Server
{
    public class ClientInput
    {
        private bool _isPressingForwardKey; 
        private bool _isPressingBackwardsKey; 
        private bool _isPressingLeftKey;
        private bool _isPressingRightKey;
        private SortedDictionary<int, (bool, bool, bool, bool)> _inputs;
        
        public ClientInput(BitBuffer buffer)
        {
            _isPressingForwardKey = buffer.GetBit(); 
            _isPressingBackwardsKey = buffer.GetBit(); 
            _isPressingLeftKey = buffer.GetBit();
            _isPressingRightKey = buffer.GetBit();
            _inputs = new SortedDictionary<int, (bool, bool, bool, bool)>();
        }
        
        public ClientInput()
        {
            _isPressingForwardKey = false; 
            _isPressingBackwardsKey = false; 
            _isPressingLeftKey = false;
            _isPressingRightKey = false;
            _inputs = new SortedDictionary<int, (bool, bool, bool, bool)>();
        }

        public bool IsPressingForwardKey => _isPressingForwardKey;

        public bool IsPressingBackwardsKey => _isPressingBackwardsKey;

        public bool IsPressingLeftKey => _isPressingLeftKey;

        public bool IsPressingRightKey => _isPressingRightKey;
    }
}