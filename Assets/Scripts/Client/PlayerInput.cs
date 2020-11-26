using System.Collections.Generic;
using System.Linq;
using Commons.Game;
using Commons.Networking;
using Commons.Utils;
using UnityEngine;
using Input = Commons.Game.Input;
using Logger = Commons.Utils.Logger;

namespace Client
{
    public class PlayerInput
    {
        private const float ShootingCooldownSeconds = 0.5f;

        private readonly KeyCode _forwardKey;
        private readonly KeyCode _backwardsKey;
        private readonly KeyCode _leftKey;
        private readonly KeyCode _rightKey;
        private readonly KeyCode _shootKey;
        private readonly SortedDictionary<int, Input> _inputsToSend;
        private readonly SortedDictionary<int, Shoot> _shootsToSend;

        private Input _currentInput;
        private bool _shooting;
        private int _biggestInputIdQueuedToSend;
        private int _biggestShootIdQueuedToSend;
        private float _accumCooldown;

        public PlayerInput(KeyCode forwardKey, KeyCode backwardsKey, KeyCode leftKey, KeyCode rightKey, KeyCode shootKey)
        {
            _forwardKey = forwardKey;
            _backwardsKey = backwardsKey;
            _leftKey = leftKey;
            _rightKey = rightKey;
            _shootKey = shootKey;
            _accumCooldown = 0f;
            _shooting = false;
            _currentInput = new Input();
            _biggestInputIdQueuedToSend = -1;
            _inputsToSend = new SortedDictionary<int, Input>();
            _shootsToSend = new SortedDictionary<int, Shoot>();
        }

        public void Read()
        {
            _shooting = UnityEngine.Input.GetKey(_shootKey) && _accumCooldown >= ShootingCooldownSeconds;
            if (_shooting)
            {
                _accumCooldown = 0f;
            }
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
        
        public bool HasShootsToSend()
        {
            return _shootsToSend.Count > 0;
        }

        public SortedDictionary<int, Input> GetInputsNotProcessedByServer()
        {
            return _inputsToSend;
        }
        
        public SortedDictionary<int, Shoot> GetShootsNotProcessedByServer()
        {
            return _shootsToSend;
        }

        public void AddShoot(Shoot shoot)
        {
            _shootsToSend[++_biggestShootIdQueuedToSend] = shoot;
        }
        
        public int DiscardShootsAlreadyProcessedByServer(int lastShootIdProcessedByServer)
        {
            var score = 0;
            foreach (var shootId in _shootsToSend.Keys.Where(id => id <= lastShootIdProcessedByServer).ToList())
            {
                _shootsToSend.Remove(shootId);
                score += 10;
            }
            return score;
        }

        public void DiscardShootsByTimeout(float newDeltaTime)
        {
            foreach (var shoot in _shootsToSend.ToList())
            {
                if (shoot.Value.MustBeDiscardedByTimeout(newDeltaTime))
                {
                    _shootsToSend.Remove(shoot.Key);
                }
            }
        }

        public Input CurrentInput => _currentInput;

        public int BiggestInputIdQueuedToSend => _biggestInputIdQueuedToSend;

        public bool Shooting => _shooting;

        public float AccumCooldown
        {
            get => _accumCooldown;
            set => _accumCooldown = value;
        }
    }
}