using System.Collections.Generic;
using Networking;
using UnityEngine;

namespace Client
{
    public class PlayerInput
    {
        private readonly KeyCode _forwardKey;
        private readonly KeyCode _backwardsKey;
        private readonly KeyCode _leftKey;
        private readonly KeyCode _rightKey;

        private bool _isPressingForwardKey; 
        private bool _isPressingBackwardsKey; 
        private bool _isPressingLeftKey;
        private bool _isPressingRightKey;
        private int _lastInputAckId;
        private int _lastInputReadId;
        private SortedDictionary<int, (bool, bool, bool, bool)> _inputs;
    
        public PlayerInput(KeyCode forwardKey, KeyCode backwardsKey, KeyCode leftKey, KeyCode rightKey)
        {
            _forwardKey = forwardKey;
            _backwardsKey = backwardsKey;
            _leftKey = leftKey;
            _rightKey = rightKey;
            _isPressingForwardKey = false;
            _isPressingBackwardsKey = false;
            _isPressingLeftKey = false;
            _isPressingRightKey = false;
            _inputs = new SortedDictionary<int, (bool, bool, bool, bool)>();
        }

        public void Read()
        {
            var isPressingForwardKey = Input.GetKey(_forwardKey);
            var isPressingBackwardsKey = Input.GetKey(_backwardsKey);
            var isPressingLeftKey = Input.GetKey(_leftKey);
            var isPressingRightKey = Input.GetKey(_rightKey);
            _isPressingForwardKey = isPressingForwardKey;
            _isPressingBackwardsKey = isPressingBackwardsKey;
            _isPressingLeftKey = isPressingLeftKey;
            _isPressingRightKey = isPressingRightKey;
        }

        public void SerializeIntoBuffer(BitBuffer buffer)
        {
            buffer.PutInt(_inputs.Count);
            foreach (var input in _inputs)
            {
                buffer.PutInt(input.Key);
                buffer.PutBit(input.Value.Item1); 
                buffer.PutBit(input.Value.Item2); 
                buffer.PutBit(input.Value.Item3);
                buffer.PutBit(input.Value.Item4);
            }
        }
    
        private void DeserializeFromBuffer(BitBuffer buffer)
        {
            for (var inputsToProcess = buffer.GetInt(); inputsToProcess > 0; inputsToProcess--)
            {
                var id = buffer.GetInt();
                var forward = buffer.GetBit();
                var back = buffer.GetBit(); 
                var light = buffer.GetBit();
                var right = buffer.GetBit();
                if (!_inputs.ContainsKey(id) && _lastInputReadId > 0)
                {
                    _inputs[id] = (forward, back, light, right);
                }
            }
        }

        public void ProcessInputAck(BitBuffer buffer)
        {
            var inputId = buffer.GetInt();
            if (inputId > _lastInputAckId)
            {
                _lastInputAckId = inputId;
            }
        }

        public bool IsPressingForwardKey => _isPressingForwardKey;

        public bool IsPressingBackwardsKey => _isPressingBackwardsKey;

        public bool IsPressingLeftKey => _isPressingLeftKey;

        public bool IsPressingRightKey => _isPressingRightKey;
    }
}