using UnityEngine;

namespace Commons.Utils
{
    public static class Printer
    {
        public static string V3(Vector3 vector3)
        {
            return "(" + vector3.x + "; " + vector3.y + "; " + vector3.z + ")";
        }
    }
}