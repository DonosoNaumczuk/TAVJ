using System;
using Commons.Utils;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Logger = Commons.Utils.Logger;

namespace Menu
{
    public class SceneManager : MonoBehaviour
    {
        public const string ModeKey = "mode";
        public const string ServerIpKey = "server_ip";
        public const string ServerPortKey = "server_port";
        public const string ClientPortKey = "client_port";
        public const int ServerMode = 0;
        public const int ClientMode = 1;
        public InputField serverIp;
        public InputField serverPort;
        public InputField clientIp;
        public InputField clientPort;
        public Scene serverScene;
        public Scene clientScene;

        private void Start()
        {
            serverIp.text = "127.0.0.1";
            serverPort.text = "9000";
            clientPort.text = "9001";
        }

        public void OnHostGame()
        {
            PlayerPrefs.SetInt(ModeKey, ServerMode);
            PlayerPrefs.SetString(ServerIpKey, serverIp.text);
            PlayerPrefs.SetInt(ServerPortKey, int.Parse(serverPort.text));
            PlayerPrefs.Save();
            LoadMainScene();
        }

        public void OnJoinGame()
        {
            PlayerPrefs.SetInt(ModeKey, ClientMode);
            PlayerPrefs.SetString(ServerIpKey, serverIp.text);
            PlayerPrefs.SetInt(ServerPortKey, int.Parse(serverPort.text));
            PlayerPrefs.SetInt(ClientPortKey, int.Parse(clientPort.text));
            PlayerPrefs.Save();
            LoadMainScene();
        }

        private void LoadMainScene()
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainScene");
        }
    }
}