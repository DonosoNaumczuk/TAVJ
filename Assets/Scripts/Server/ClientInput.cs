using System.Collections.Generic;
using System.Linq;
using Commons.Game;
using Commons.Networking;

namespace Server
{
    public class ClientInput
    {
        private Input _currentInput;
        private int _lastInputIdProcessed;
        private readonly SortedDictionary<int, Input> _inputsToProcess;

        public ClientInput()
        {
            _currentInput = new Input();
            _lastInputIdProcessed = -1;
            _inputsToProcess = new SortedDictionary<int, Input>();
        }

        public bool NextInput()
        {
            if (_inputsToProcess.Count > 0)
            {
                var nextInput = _inputsToProcess.First();
                _currentInput = nextInput.Value;
                _lastInputIdProcessed = nextInput.Key;
                _inputsToProcess.Remove(nextInput.Key);
                return true;
            }
            return false;
        }

        public void AddFromBuffer(BitBuffer buffer)
        {
            for (var inputsToRead = buffer.GetInt(); inputsToRead > 0; inputsToRead--)
            {
                var inputId = buffer.GetInt();
                var input = new Input(buffer);
                if (inputId > _lastInputIdProcessed && !_inputsToProcess.ContainsKey(inputId))
                {
                    _inputsToProcess[inputId] = input;
                }
            }
        }

        public Input CurrentInput => _currentInput;

        public int LastInputIdProcessed => _lastInputIdProcessed;
    }
}