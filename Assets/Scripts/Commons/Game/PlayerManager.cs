using Commons.Utils;
using UnityEngine;
using Logger = Commons.Utils.Logger;

namespace Commons.Game
{
    public static class PlayerManager
    {
        private const float RotationScaleFactor = 50f;
        private const float MovementScaleFactor = 8f;
        
        public static (Vector3, Vector3) ProcessInput(Input input, GameObject player, string entity, string color)
        {
            var movement = Vector3.zero;
            var rotation = Vector3.zero;
            var movChanged = false;
            var rotChanged = false;
            if (input.IsPressingForwardKey)
            {
                movement += player.transform.forward.normalized;
                movChanged = true;
            }
            else if (input.IsPressingBackwardsKey)
            {
                movement += -player.transform.forward.normalized;
                movChanged = true;
            }

            if (input.IsPressingLeftKey)
            {
                rotation += Vector3.down;
                rotChanged = true;
            }
            else if (input.IsPressingRightKey)
            {
                rotation += Vector3.up;
                rotChanged = true;
            }

            var finalMov = movement * (MovementScaleFactor * Time.fixedDeltaTime);
            var finalRot = rotation * (RotationScaleFactor * Time.fixedDeltaTime);
            if (movChanged)
            {
                player.GetComponent<CharacterController>().Move(finalMov);
            }
            if (rotChanged)
            {
                player.transform.Rotate(finalRot);
            }
            return (movement, rotation);
        }
    }
}