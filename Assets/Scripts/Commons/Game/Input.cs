using Commons.Networking;

namespace Commons.Game
{
    public class Input
    {
        private readonly bool _isPressingForwardKey; 
        private readonly bool _isPressingBackwardsKey; 
        private readonly bool _isPressingLeftKey;
        private readonly bool _isPressingRightKey;

        public Input()
        {
            _isPressingForwardKey = false;
            _isPressingBackwardsKey = false;
            _isPressingLeftKey = false;
            _isPressingRightKey = false;
        }
        
        public Input(bool isPressingForwardKey, bool isPressingBackwardsKey, bool isPressingLeftKey, bool isPressingRightKey)
        {
            _isPressingForwardKey = isPressingForwardKey;
            _isPressingBackwardsKey = isPressingBackwardsKey;
            _isPressingLeftKey = isPressingLeftKey;
            _isPressingRightKey = isPressingRightKey;
        }

        public Input(BitBuffer buffer)
        {
            _isPressingForwardKey = buffer.GetBit();
            _isPressingBackwardsKey = buffer.GetBit();
            _isPressingLeftKey = buffer.GetBit();
            _isPressingRightKey = buffer.GetBit();
        }

        public void SerializeIntoBuffer(BitBuffer buffer)
        {
            buffer.PutBit(_isPressingForwardKey); 
            buffer.PutBit(_isPressingBackwardsKey); 
            buffer.PutBit(_isPressingLeftKey);
            buffer.PutBit(_isPressingRightKey);
        }
        
        public bool IsPressingSomething()
        {
            return _isPressingForwardKey || _isPressingBackwardsKey || _isPressingLeftKey || _isPressingRightKey;
        }

        public bool IsPressingForwardKey => _isPressingForwardKey;

        public bool IsPressingBackwardsKey => _isPressingBackwardsKey;

        public bool IsPressingLeftKey => _isPressingLeftKey;

        public bool IsPressingRightKey => _isPressingRightKey;
    }
}