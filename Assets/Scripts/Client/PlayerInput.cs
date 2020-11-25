using System.Collections.Generic;
using System.Linq;
using Commons.Networking;
using UnityEngine;
using Input = Commons.Game.Input;
using Logger = Commons.Utils.Logger;

namespace Client
{
    public class PlayerInput
    {
        private readonly KeyCode _forwardKey;
        private readonly KeyCode _backwardsKey;
        private readonly KeyCode _leftKey;
        private readonly KeyCode _rightKey;
        private readonly SortedDictionary<int, Input> _inputsToSend;

        private Input _currentInput;
        private int _biggestInputIdQueuedToSend;
    
        public PlayerInput(KeyCode forwardKey, KeyCode backwardsKey, KeyCode leftKey, KeyCode rightKey)
        {
            _forwardKey = forwardKey;
            _backwardsKey = backwardsKey;
            _leftKey = leftKey;
            _rightKey = rightKey;
            _currentInput = new Input();
            _biggestInputIdQueuedToSend = -1;
            _inputsToSend = new SortedDictionary<int, Input>();
        }

        public void Read()
        {
            _currentInput = new Input(UnityEngine.Input.GetKey(_forwardKey), UnityEngine.Input.GetKey(_backwardsKey),
                UnityEngine.Input.GetKey(_leftKey), UnityEngine.Input.GetKey(_rightKey));
            _inputsToSend[++_biggestInputIdQueuedToSend] = _currentInput;
        }

        public void SerializeIntoBuffer(BitBuffer buffer)
        {
            buffer.PutInt(_inputsToSend.Count);
            foreach (var input in _inputsToSend)
            {
                buffer.PutInt(input.Key);
                input.Value.SerializeIntoBuffer(buffer);
            }
        }

        public void DiscardInputsAlreadyProcessedByServer(int lastInputIdProcessedByServer)
        {
            foreach (var inputId in _inputsToSend.Keys.Where(id => id <= lastInputIdProcessedByServer).ToList())
            {
                _inputsToSend.Remove(inputId);
            }
        }

        public bool HasInputsToSend()
        {
            return _inputsToSend.Count > 0;
        }

        public SortedDictionary<int, Input> GetInputsNotProcessedByServer()
        {
            return _inputsToSend;
        }

        public Input CurrentInput => _currentInput;

        public int BiggestInputIdQueuedToSend => _biggestInputIdQueuedToSend;
    }
}