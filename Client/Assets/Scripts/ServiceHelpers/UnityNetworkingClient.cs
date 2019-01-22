using PlayFab;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Networking.NetworkSystem;
using UnityEngine.Events;

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
            catch (Exception e) { }

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
            public const short PlayerAddedMessage = 1000;
            public const short PlayerLocationMessage = 1001;
        }

        public class PlayerLocationMessage : MessageBase
        {
            public string PlayFabId;
            public Vector3 PlayerLocation;
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