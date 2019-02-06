using Assets.Scripts.PlayerScripts;
using PlayFab;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
using UnityEngine.Networking.NetworkSystem;

namespace Assets.Scripts.ServiceHelpers
{
    public class UnityNetworkingClient : MonoBehaviour
    {
        public ConnectedEvent OnConnected = new ConnectedEvent();
        public DisconnectedEvent OnDisconnected = new DisconnectedEvent();

        public NetworkClient Client;
        private NetworkManager _netManager;

        public class ConnectedEvent : UnityEvent { }
        public class DisconnectedEvent : UnityEvent<int?> { }

        public static UnityNetworkingClient Instance { get; set; }

        private void Awake()
        {
            Instance = this;

            _netManager = GetComponent<NetworkManager>();
            _netManager.StartClient();
            Client = _netManager.client;
        }

        private void Start()
        {
            Client.RegisterHandler(MsgType.Connect, OnNetworkConnected);
            Client.RegisterHandler(MsgType.Disconnect, OnNetworkDisconnected);
        }

        private void OnNetworkConnected(NetworkMessage netMsg)
        {
            Debug.Log("Connected To Network Server");
            OnConnected.Invoke();
        }

        private void OnNetworkDisconnected(NetworkMessage netMsg)
        {
            ErrorMessage message = null;
            try
            {
                message = netMsg.ReadMessage<ErrorMessage>();
            }
            catch { }

            if (message != null)
            {
                OnDisconnected.Invoke(message.errorCode);
            }
            else
            {
                OnDisconnected.Invoke(null);
            }
        }

        public class CustomGameServerMessageTypes
        {
            // CONNECTION
            public const short ReceiveAuthenticate = 900;
            public const short ShutdownMessage = 901;
            public const short MaintenanceMessage = 902;

            // COMMUNICATION
            public const short PlayerAddedMessage = 999;
            public const short PlayersAddedMessage = 1000;
            public const short PlayerInfoMessage = 1001;
            public const short ProjectileFiredMessage = 1002;
            public const short PlayerDeadMessage = 1003;
        }

        public class PlayerInfoMessages : MessageBase
        {
            public PlayerInfo[] Internal;

            public PlayerInfoMessages() { }

            public PlayerInfoMessages(PlayerInfo[] messages)
            {
                Internal = messages;
            }
        }

        public class PlayerInfoMessage : MessageBase
        {
            public PlayerInfo Internal;

            public PlayerInfoMessage() { }

            public PlayerInfoMessage(PlayerInfo playerInfo)
            {
                Internal = playerInfo;
            }
        }

        [Serializable]
        public class PlayerIdMessage : MessageBase
        {
            public string PlayFabId;

            public PlayerIdMessage() { }

            public PlayerIdMessage(string playFabId)
            {
                PlayFabId = playFabId;
            }
        }

        public struct PlayerInfo
        {
            public string PlayFabId;

            public Vector3 PlayerPosition;

            public Quaternion PlayerRotation;

            public int Health;

            public PlayerInfo(string playFabId, Transform player)
            {
                PlayFabId = playFabId;
                PlayerPosition = player.localPosition;
                PlayerRotation = player.localRotation;

                var health = player.GetComponent<PlayerHealth>();
                Health = health.CurrentHealth;
            }
        }

        public class ProjectileFiredMessage : MessageBase
        {
            public string PlayFabId;

            public Vector3 ProjectileStartingPosition;

            public Quaternion ProjectileStartingRotation;

            public Vector3 ProjectileVelocity;

            public ProjectileFiredMessage() { }

            public ProjectileFiredMessage(string playFabId, Transform projectile)
            {
                PlayFabId = playFabId;
                ProjectileStartingPosition = projectile.localPosition;
                ProjectileStartingRotation = projectile.localRotation;
                ProjectileVelocity = projectile.GetComponent<Rigidbody>().velocity;
            }
        }

        public class ShutdownMessage : MessageBase { }

        [Serializable]
        public class MaintenanceMessage : MessageBase
        {
            public DateTime ScheduledMaintenanceUTC;

            public override void Deserialize(NetworkReader reader)
            {
                var json = PlayFab.PluginManager.GetPlugin<ISerializerPlugin>(PluginContract.PlayFab_Serializer);
                ScheduledMaintenanceUTC = json.DeserializeObject<DateTime>(reader.ReadString());
            }

            public override void Serialize(NetworkWriter writer)
            {
                var json = PlayFab.PluginManager.GetPlugin<ISerializerPlugin>(PluginContract.PlayFab_Serializer);
                var str = json.SerializeObject(ScheduledMaintenanceUTC);
                writer.Write(str);
            }
        }

    }
}