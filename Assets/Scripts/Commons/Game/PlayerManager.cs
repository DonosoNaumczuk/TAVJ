using UnityEngine;

namespace Commons.Game
{
    public static class PlayerManager
    {
        private const float RotationScaleFactor = 200f;
        private const float MovementScaleFactor = 10f;
        
        public static void ProcessInput(Input input, GameObject player)
        {
            var movement = Physics.gravity;
            var rotation = Vector3.zero;
            if (input.IsPressingForwardKey)
            {
                movement = player.transform.forward.normalized;
            }
            else if (input.IsPressingBackwardsKey)
            {
                movement = -player.transform.forward.normalized;
            }

            if (input.IsPressingLeftKey)
            {
                rotation = Vector3.down;
            }
            else if (input.IsPressingRightKey)
            {
                rotation = Vector3.up;
            }
            player.GetComponent<CharacterController>().Move(movement * (MovementScaleFactor * Time.deltaTime));
            player.transform.Rotate(rotation * (RotationScaleFactor * Time.deltaTime));
        }
    }
}