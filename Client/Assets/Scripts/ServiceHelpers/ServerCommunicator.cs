﻿using Assets.Scripts.Game_Scripts;
using UnityEngine;
using UnityEngine.Networking;
using static Assets.Scripts.ServiceHelpers.UnityNetworkingClient;

namespace Assets.Scripts.ServiceHelpers
{
    public class ServerCommunicator : MonoBehaviour
    {
        private static UnityNetworkingClient _unc;

        public void Start()
        {
            _unc = UnityNetworkingClient.Instance;
            _unc.Client.RegisterHandler(CustomGameServerMessageTypes.PlayerAddedMessage, OnPlayerAdded);
            _unc.Client.RegisterHandler(CustomGameServerMessageTypes.PlayersAddedMessage, OnPlayersAdded);
            _unc.Client.RegisterHandler(CustomGameServerMessageTypes.PlayerInfoMessage, OnPlayerInfoReceived);
            _unc.Client.RegisterHandler(CustomGameServerMessageTypes.ProjectileFiredMessage, OnProjectileFiredReceived);
            _unc.Client.RegisterHandler(CustomGameServerMessageTypes.PlayerDeadMessage, OnPlayerDeadReceived);
            _unc.Client.RegisterHandler(CustomGameServerMessageTypes.PlayerRespawnMessage, OnPlayerRespawnedReceived);
        }

        #region RECEIVING

        private void OnPlayerAdded(NetworkMessage netMsg)
        {
            var message = netMsg.ReadMessage<PlayerInfoMessage>();
            //Debug.Log($"On Player Added {message.Internal.PlayFabId}");
            GameState.CreatePlayer(message.Internal);
        }

        private void OnPlayersAdded(NetworkMessage netMsg)
        {
            var messages = netMsg.ReadMessage<PlayerInfoMessages>();
            //Debug.Log($"On Players Added");
            foreach (PlayerInfo playerInfo in messages.Internal)
            {
                GameState.CreatePlayer(playerInfo);
            }
        }

        public void OnPlayerInfoReceived(NetworkMessage netMsg)
        {
            var message = netMsg.ReadMessage<PlayerInfoMessage>();
            //Debug.Log($"Player {message.Internal.PlayFabId} location: {message.Internal.PlayerPosition}");
            GameState.UpdatePlayerInfo(message.Internal);
        }

        public void OnProjectileFiredReceived(NetworkMessage netMsg)
        {
            var message = netMsg.ReadMessage<ProjectileFiredMessage>();
            //Debug.Log($"Player {message.Internal.PlayFabId} location: {message.Internal.PlayerPosition}");
            GameState.CreateProjectile(message);
        }

        public void OnPlayerDeadReceived(NetworkMessage netMsg)
        {
            var message = netMsg.ReadMessage<PlayerIdMessage>();
            Debug.Log($"Player {message.PlayFabId} dead message received.");
            GameState.KillPlayer(message.PlayFabId, notifyServer: false);
        }

        public void OnPlayerRespawnedReceived(NetworkMessage netMsg)
        {
            var message = netMsg.ReadMessage<PlayerInfoMessage>();
            Debug.Log($"Player {message.Internal.PlayFabId} respawned message received.");
            GameState.RespawnPlayer(message.Internal);
        }

        #endregion RECEIVING

        #region SENDING

        public void SendPlayerInfo(string playfabId, Transform player)
        {
            _unc?.Client?.connection?.Send(CustomGameServerMessageTypes.PlayerInfoMessage,
                new PlayerInfoMessage(new PlayerInfo(playfabId, player))
            );
        }

        public void SendProjectileFired(string playfabId, Transform projectile)
        {
            _unc?.Client?.connection?.Send(CustomGameServerMessageTypes.ProjectileFiredMessage, 
                new ProjectileFiredMessage(playfabId, projectile));
        }

        public void SendPlayerDead(string playfabId)
        {
            _unc?.Client?.connection?.Send(CustomGameServerMessageTypes.PlayerDeadMessage, new PlayerIdMessage(playfabId));
        }

        #endregion SENDING

    }
}
