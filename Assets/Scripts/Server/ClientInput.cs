using System.Collections.Generic;
using System.Linq;
using Networking;

namespace Server
{
    public class ClientInput
    {
        private bool _isPressingForwardKey; 
        private bool _isPressingBackwardsKey; 
        private bool _isPressingLeftKey;
        private bool _isPressingRightKey;
        private int _lastProcessedInput;
        private readonly SortedDictionary<int, (bool, bool, bool, bool)> _inputsToProcess;

        public ClientInput()
        {
            _isPressingForwardKey = false; 
            _isPressingBackwardsKey = false; 
            _isPressingLeftKey = false;
            _isPressingRightKey = false;
            _lastProcessedInput = -1;
            _inputsToProcess = new SortedDictionary<int, (bool, bool, bool, bool)>();
        }

        public bool NextInput()
        {
            Logger.Log("Server: _inputsToProcess.Count = " + _inputsToProcess.Count, false);
            Logger.Log("Server: _lastProcessedInput = " + _lastProcessedInput, false);
            if (_inputsToProcess.Count > 0)
            {
                var nextInput = _inputsToProcess.First();
                (_isPressingForwardKey, _isPressingBackwardsKey, _isPressingLeftKey, _isPressingRightKey) = nextInput.Value;
                Logger.Log("yellow", "Server: _lastProcessedInput = " + _lastProcessedInput 
                    + ", nextInput.Key = " + nextInput.Key, false);
                _lastProcessedInput = nextInput.Key;
                _inputsToProcess.Remove(nextInput.Key);
                return true;
            }
            return false;
        }

        public void AddFromBuffer(BitBuffer buffer)
        {
            for (var inputsToRead = GetInputsToRead(buffer); inputsToRead > 0; inputsToRead--)
            {
                var inputId = buffer.GetInt();
                var input = (buffer.GetBit(),  buffer.GetBit(),  buffer.GetBit(),  buffer.GetBit());
                Logger.Log("cyan", input.ToString(), inputsToRead == 1);
                if (inputId > _lastProcessedInput && !_inputsToProcess.ContainsKey(inputId))
                {
                    Logger.Log("green", "Server: _lastProcessedInput = " + _lastProcessedInput 
                        + ", _inputsToProcess.Add(Input " + inputId + ")", false);
                    _inputsToProcess[inputId] = input;
                }
            }
        }

        private int GetInputsToRead(BitBuffer buffer)
        {
            var inputsToRead = buffer.GetInt();
            Logger.Log("Server: inputsToRead = " + inputsToRead, false);
            return inputsToRead;
        }

        public bool IsPressingForwardKey => _isPressingForwardKey;

        public bool IsPressingBackwardsKey => _isPressingBackwardsKey;

        public bool IsPressingLeftKey => _isPressingLeftKey;

        public bool IsPressingRightKey => _isPressingRightKey;

        public int LastProcessedInput => _lastProcessedInput;
    }
}