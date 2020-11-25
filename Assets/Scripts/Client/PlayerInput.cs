using System.Collections.Generic;
using System.Linq;
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
        private readonly SortedDictionary<int, (bool, bool, bool, bool)> _inputs;

        private bool _isPressingForwardKey; 
        private bool _isPressingBackwardsKey; 
        private bool _isPressingLeftKey;
        private bool _isPressingRightKey;
        private int _lastInputAckId;
        private int _lastInputReadId;
    
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
            _lastInputReadId = -1;
            _lastInputAckId = -1;
            _inputs = new SortedDictionary<int, (bool, bool, bool, bool)>();
        }

        public void Read()
        {
            _isPressingForwardKey = Input.GetKey(_forwardKey);
            _isPressingBackwardsKey = Input.GetKey(_backwardsKey);
            _isPressingLeftKey = Input.GetKey(_leftKey);
            _isPressingRightKey = Input.GetKey(_rightKey);
            if (IsPressingSomething())
            {
                _inputs[++_lastInputReadId] = (_isPressingForwardKey, _isPressingBackwardsKey, _isPressingLeftKey,
                    _isPressingRightKey);
            }
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

        public void SetLastProcessedInputId(int lastProcessedInputId)
        {
            if (lastProcessedInputId > _lastInputAckId)
            {
                _lastInputAckId = lastProcessedInputId;
                foreach (var inputId in _inputs.Keys.Where(id => id <= _lastInputAckId).ToList())
                {
                    _inputs.Remove(inputId);
                }
            }
        }

        private bool IsPressingSomething()
        {
            return _isPressingForwardKey || _isPressingBackwardsKey || _isPressingLeftKey || _isPressingRightKey;
        }

        public bool HasInputsToSend()
        {
            Logger.Log("green", "_inputs.Count = " + _inputs.Count, true);
            return _inputs.Count > 0;
        }

        public bool IsPressingForwardKey => _isPressingForwardKey;

        public bool IsPressingBackwardsKey => _isPressingBackwardsKey;

        public bool IsPressingLeftKey => _isPressingLeftKey;

        public bool IsPressingRightKey => _isPressingRightKey;
    }
}