using UnityEngine;

namespace Commons.Utils
{
    public static class Logger
    {
        public static void Log(string log)
        {
            Debug.Log(log);
        }
    
        public static void Log(string log, bool condition)
        {
            if (condition)
            {
                Debug.Log(log);
            }
        }

        public static void Log(string color, string log, bool condition)
        {
            if (condition)
            {
                Debug.Log("<color=" + color + ">" + log + "</color>");
            }
        }
    }
}