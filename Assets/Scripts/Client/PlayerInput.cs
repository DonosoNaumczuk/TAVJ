using System.Collections.Generic;
using System.Linq;
using Commons.Networking;
using UnityEngine;
using Input = Commons.Game.Input;

namespace Client
{
    public class PlayerInput
    {
        private readonly KeyCode _forwardKey;
        private readonly KeyCode _backwardsKey;
        private readonly KeyCode _leftKey;
        private readonly KeyCode _rightKey;
        private readonly SortedDictionary<int, Input> _inputs;

        private Input _currentInput;
        private int _lastInputIdProcessed;
        private int _lastInputReadId;
    
        public PlayerInput(KeyCode forwardKey, KeyCode backwardsKey, KeyCode leftKey, KeyCode rightKey)
        {
            _forwardKey = forwardKey;
            _backwardsKey = backwardsKey;
            _leftKey = leftKey;
            _rightKey = rightKey;
            _currentInput = new Input();
            _lastInputReadId = -1;
            _lastInputIdProcessed = -1;
            _inputs = new SortedDictionary<int, Input>();
        }

        public void Read()
        {
            _currentInput = new Input(UnityEngine.Input.GetKey(_forwardKey), UnityEngine.Input.GetKey(_backwardsKey),
                UnityEngine.Input.GetKey(_leftKey), UnityEngine.Input.GetKey(_rightKey));
            if (_currentInput.IsPressingSomething())
            {
                _inputs[++_lastInputReadId] = _currentInput;
            }
        }

        public void SerializeIntoBuffer(BitBuffer buffer)
        {
            buffer.PutInt(_inputs.Count);
            foreach (var input in _inputs)
            {
                buffer.PutInt(input.Key);
                input.Value.SerializeIntoBuffer(buffer);
            }
        }

        public void SetLastInputIdProcessedByServer(int lastInputIdProcessed)
        {
            if (lastInputIdProcessed > _lastInputIdProcessed)
            {
                _lastInputIdProcessed = lastInputIdProcessed;
                foreach (var inputId in _inputs.Keys.Where(id => id <= _lastInputIdProcessed).ToList())
                {
                    _inputs.Remove(inputId);
                }
            }
        }

        public bool HasInputsToSend()
        {
            return _inputs.Count > 0;
        }

        public SortedDictionary<int, Input> GetInputsNotProcessedByServer()
        {
            return _inputs;
        }

        public Input CurrentInput => _currentInput;

        public int LastInputReadId => _lastInputReadId;

        public int LastInputIdProcessed => _lastInputIdProcessed;
    }
}