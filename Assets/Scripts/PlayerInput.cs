
using UnityEngine;

public class PlayerInput
{
    private readonly KeyCode _forwardKey;
    private readonly KeyCode _backwardsKey;
    private readonly KeyCode _leftKey;
    private readonly KeyCode _rightKey;
    private readonly KeyCode _shootKey;
    
    private bool _isPressingForwardKey; 
    private bool _isPressingBackwardsKey; 
    private bool _isPressingLeftKey;
    private bool _isPressingRightKey;
    private bool _isPressingShootKey;
    private bool _hasChanged;

    public PlayerInput(KeyCode forwardKey, KeyCode backwardsKey, KeyCode leftKey, KeyCode rightKey, KeyCode shootKey)
    {
        _forwardKey = forwardKey;
        _backwardsKey = backwardsKey;
        _leftKey = leftKey;
        _rightKey = rightKey;
        _shootKey = shootKey;
        _isPressingForwardKey = false; 
        _isPressingBackwardsKey = false; 
        _isPressingLeftKey = false;
        _isPressingRightKey = false;
        _isPressingShootKey = false;
        _hasChanged = false;
    }

    public PlayerInput(BitBuffer buffer)
    {
        _isPressingForwardKey = buffer.GetBit(); 
        _isPressingBackwardsKey = buffer.GetBit(); 
        _isPressingLeftKey = buffer.GetBit();
        _isPressingRightKey = buffer.GetBit();
        _isPressingShootKey = buffer.GetBit();
    }

    public PlayerInput()
    {
        _isPressingForwardKey = false; 
        _isPressingBackwardsKey = false; 
        _isPressingLeftKey = false;
        _isPressingRightKey = false;
        _isPressingShootKey = false;
    }

    public void Read()
    {
        var isPressingForwardKey = Input.GetKey(_forwardKey);
        var isPressingBackwardsKey = Input.GetKey(_backwardsKey);
        var isPressingLeftKey = Input.GetKey(_leftKey);
        var isPressingRightKey = Input.GetKey(_rightKey);
        var isPressingShootKey = Input.GetKey(_shootKey);

        _hasChanged = _isPressingForwardKey ^ isPressingForwardKey
                      || _isPressingBackwardsKey ^ isPressingBackwardsKey
                      || _isPressingLeftKey ^ isPressingLeftKey
                      || _isPressingRightKey ^ isPressingRightKey
                      || _isPressingShootKey ^ isPressingShootKey;    
            
        _isPressingForwardKey = isPressingForwardKey;
        _isPressingBackwardsKey = isPressingBackwardsKey;
        _isPressingLeftKey = isPressingLeftKey;
        _isPressingRightKey = isPressingRightKey;
        _isPressingShootKey = isPressingShootKey;
    }

    public void SerializeIntoBuffer(BitBuffer buffer)
    {
        buffer.PutBit(_isPressingForwardKey); 
        buffer.PutBit(_isPressingBackwardsKey); 
        buffer.PutBit(_isPressingLeftKey);
        buffer.PutBit(_isPressingRightKey);
        buffer.PutBit(_isPressingShootKey);
    }

    public bool IsPressingForwardKey => _isPressingForwardKey;

    public bool IsPressingBackwardsKey => _isPressingBackwardsKey;

    public bool IsPressingLeftKey => _isPressingLeftKey;

    public bool IsPressingRightKey => _isPressingRightKey;

    public bool IsPressingShootKey => _isPressingShootKey;

    public bool HasChanged => _hasChanged;
}