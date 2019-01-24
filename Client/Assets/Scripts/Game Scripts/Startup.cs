using System;
using Assets.Scripts.PlayerScripts;
using Assets.Scripts.ServiceHelpers;
using PlayFab;
using PlayFab.ClientModels;
using PlayFab.Helpers;
using UnityEngine;
using UnityEngine.Networking;

namespace Assets.Scripts.Game_Scripts
{
    public class Startup : MonoBehaviour
    {
        public PlayerMetadata Player;
        public Transform EnemyPrefab;
        public GameObject EnemiesContainer;

        private PlayFabAuthService _authService;
        private UnityNetworkingClient _unc;
        private MessageWindow _messageWindow;
        private float _timer = 0f;

        // Use this for initialization
        void Start()
        {
            _authService = PlayFabAuthService.Instance;
            //PlayFabAuthService.OnDisplayAuthentication += OnDisplayAuth;
            PlayFabAuthService.OnLoginSuccess += OnLoginSuccess;

            _unc = UnityNetworkingClient.Instance;
            _unc.OnDisconnected.AddListener(OnDisconnected);
            _unc.OnConnected.AddListener(OnConnected);
            _unc.Client.RegisterHandler(UnityNetworkingClient.CustomGameServerMessageTypes.ShutdownMessage, OnServerShutdown);
            _unc.Client.RegisterHandler(UnityNetworkingClient.CustomGameServerMessageTypes.MaintenanceMessage, OnMaintenanceMessage);
            _unc.Client.RegisterHandler(UnityNetworkingClient.CustomGameServerMessageTypes.PlayerAddedMessage, OnPlayerAdded);

            _messageWindow = MessageWindow.Instance;
        }

        private void OnPlayerAdded(NetworkMessage netMsg)
        {
            var message = netMsg.ReadMessage<UnityNetworkingClient.PlayerLocationMessage>();
            _messageWindow.Title.text = $"Player {message.PlayFabId} joined the game!";
            _messageWindow.Message.text = string.Empty;
            _messageWindow.gameObject.SetActive(true);
            Debug.Log($"Player {message.PlayFabId} joined game! Location: {message.PlayerLocation}");

            // Create enemy
            var enemyTransform = Instantiate(EnemyPrefab, EnemiesContainer.transform);
            enemyTransform.gameObject.SetActive(true);
            enemyTransform.name = $"enemy{message.PlayFabId}";
            enemyTransform.localPosition = message.PlayerLocation;

            // Set enemy playfabId
            var enemyMetadata = enemyTransform.GetComponentInChildren<PlayerMetadata>();
            enemyMetadata.PlayFabId = message.PlayFabId;
        }

        private void OnMaintenanceMessage(NetworkMessage netMsg)
        {
            var message = netMsg.ReadMessage<UnityNetworkingClient.MaintenanceMessage>();
            _messageWindow.Title.text = "Maintenance Shutown scheduled";
            _messageWindow.Message.text = string.Format("Maintenance is scheduled for: {0}", message.ScheduledMaintenanceUTC.ToString("MM-DD-YYYY hh:mm:ss"));
            _messageWindow.gameObject.SetActive(true);
        }

        private void OnServerShutdown(NetworkMessage netMsg)
        {
            _messageWindow.Title.text = "Shutdown In Progress";
            _messageWindow.Message.text = "Server has issued a shutdown.";
            _messageWindow.gameObject.SetActive(true);
            _unc.Client.Disconnect();
        }

        private void OnConnected()
        {
            var request = new LoginWithCustomIDRequest { CustomId = Guid.NewGuid().ToString(), CreateAccount = true };
            PlayFabClientAPI.LoginWithCustomID(request, OnLoginSuccess, OnLoginFailure);

            //_authService.Authenticate(Authtypes.Silent);
        }
        
        //private void OnDisplayAuth()
        //{
        //    _authService.Authenticate(Authtypes.Silent);
        //}

        private void OnLoginSuccess(LoginResult success)
        {
            _messageWindow.Title.text = "Login Successful";
            _messageWindow.Message.text = string.Format("You logged in successfully. ID:{0}", success.PlayFabId);
            _messageWindow.gameObject.SetActive(true);

            Player.PlayFabId = success.PlayFabId;

            _unc.Client.connection.Send(UnityNetworkingClient.CustomGameServerMessageTypes.ReceiveAuthenticate, new UnityNetworkingClient.PlayerLocationMessage()
            {
                PlayFabId = success.PlayFabId,
                PlayerLocation = Player.transform.localPosition
            });
        }

        private void OnLoginFailure(PlayFabError obj)
        {
            _messageWindow.Title.text = "Login Failed";
            _messageWindow.Message.text = string.Format("You failed to log in. Error:", obj.ErrorMessage);
            _messageWindow.gameObject.SetActive(true);
        }

        private void OnDisconnected(int? code)
        {
            _messageWindow.Title.text = "Disconnected!";
            _messageWindow.Message.text = "You were disconnected from the server";
            _messageWindow.gameObject.SetActive(true);
        }
    }
}