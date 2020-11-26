using System;
using Menu;
using UnityEngine;

namespace Commons.Game
{
    public class GameManager : MonoBehaviour
    {
        public Server.Server serverPrefab;
        public Client.Client clientPrefab;
        public GameObject serverCamera;
        
        private int _mode;

        private void Awake()
        {
            _mode = PlayerPrefs.GetInt(Menu.SceneManager.ModeKey);
            PlayerPrefs.DeleteKey(Menu.SceneManager.ModeKey);
        }

        private void Start()
        {
            if (_mode == SceneManager.ClientMode)
            {
                Instantiate(clientPrefab);
            }
            else
            {
                Instantiate(serverCamera);
                Instantiate(serverPrefab);
            }
        }
    }
}