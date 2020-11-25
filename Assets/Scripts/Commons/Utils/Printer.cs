using UnityEngine;
using Input = Commons.Game.Input;

namespace Commons.Utils
{
    public static class Printer
    {
        public static string V3(Vector3 vector3)
        {
            return "( x = " + vector3.x + "; y = " + vector3.y + "; z = " + vector3.z + ")";
        }
        
        public static string Q4(Quaternion quaternion)
        {
            return "( x = " + quaternion.x + "; y = " + quaternion.y + "; z = " + quaternion.z + "; w = " + quaternion.w + ")";
        }

        public static string I(Input input)
        {
            return "( f = " + input.IsPressingForwardKey + "; b = " + input.IsPressingBackwardsKey + "; l = " + input.IsPressingLeftKey + "; r = " + input.IsPressingRightKey + ")";
        }
    }
}