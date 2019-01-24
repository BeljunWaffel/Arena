﻿namespace PlayFab.Networking
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.Networking;
    using UnityEngine.Events;
    using UnityEngine.Networking.NetworkSystem;

    public class UnityNetworkServer : MonoBehaviour
    {
        public PlayerWithLocationEvent OnPlayerAdded = new PlayerWithLocationEvent();
        public PlayerEvent OnPlayerRemoved = new PlayerEvent();
        public PlayerWithLocationEvent OnPlayerLocationReceived = new PlayerWithLocationEvent();

        public int MaxConnections = 100;
        public int Port = 7777;

        private NetworkManager _netManager;

        public List<UnityNetworkConnection> Connections {
            get { return _connections; }
            private set { _connections = value; }
        }
        private List<UnityNetworkConnection> _connections = new List<UnityNetworkConnection>();

        public class PlayerEvent : UnityEvent<string> { }

        public class PlayerWithLocationEvent : UnityEvent<PlayerLocationMessage> { }        

        // Use this for initialization
        void Awake()
        {
            _netManager = FindObjectOfType<NetworkManager>();
            NetworkServer.RegisterHandler(MsgType.Connect, OnServerConnect);
            NetworkServer.RegisterHandler(MsgType.Disconnect, OnServerDisconnect);
            NetworkServer.RegisterHandler(MsgType.Error, OnServerError);
            NetworkServer.RegisterHandler(CustomGameServerMessageTypes.ReceiveAuthenticate, OnReceiveAuthenticate);
            NetworkServer.RegisterHandler(CustomGameServerMessageTypes.PlayerLocationMessage, OnReceivePlayerLocation);
            _netManager.networkPort = Port;
        }

        public void StartServer()
        {
            NetworkServer.Listen(Port);
        }

        private void OnApplicationQuit()
        {
            NetworkServer.Shutdown();
        }

        private void OnReceiveAuthenticate(NetworkMessage netMsg)
        {
            var conn = _connections.Find(c => c.ConnectionId == netMsg.conn.connectionId);
            if (conn != null)
            {
                var message = netMsg.ReadMessage<PlayerLocationMessage>();
                conn.PlayFabId = message.PlayFabId;
                conn.IsAuthenticated = true;
                OnPlayerAdded.Invoke(message);
            }
        }

        private void OnReceivePlayerLocation(NetworkMessage netMsg)
        {
            var conn = _connections.Find(c => c.ConnectionId == netMsg.conn.connectionId);
            if (conn != null)
            {
                var message = netMsg.ReadMessage<PlayerLocationMessage>();
                OnPlayerLocationReceived.Invoke(message);
            }
        }

        private void OnServerConnect(NetworkMessage netMsg)
        {
            Debug.LogWarning("Client Connected");
            var conn = _connections.Find(c => c.ConnectionId == netMsg.conn.connectionId); 
            if(conn == null)
            {
                _connections.Add(new UnityNetworkConnection()
                {
                    Connection = netMsg.conn,
                    ConnectionId = netMsg.conn.connectionId,
                    LobbyId = PlayFabMultiplayerAgentAPI.SessionConfig.SessionId
                });
            }
        }

        private void OnServerError(NetworkMessage netMsg)
        {
            try
            {
                var error = netMsg.ReadMessage<ErrorMessage>();
                if (error != null)
                {
                    Debug.Log(string.Format("Unity Network Connection Status: code - {0}", error.errorCode));
                }
            }
            catch (Exception)
            {
                Debug.Log("Unity Network Connection Status, but we could not get the reason, check the Unity Logs for more info.");
            }
        }

        private void OnServerDisconnect(NetworkMessage netMsg)
        {
            var conn = _connections.Find(c => c.ConnectionId == netMsg.conn.connectionId);
            if(conn != null)
            {
                if (!string.IsNullOrEmpty(conn.PlayFabId))
                {
                    OnPlayerRemoved.Invoke(conn.PlayFabId);
                }
                _connections.Remove(conn);
            }
        }

    }

    [Serializable]
    public class UnityNetworkConnection
    {
        // CONNECTION
        public bool IsAuthenticated;
        public string PlayFabId;
        public string LobbyId;
        public int ConnectionId;
        public NetworkConnection Connection;
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

        public Vector3 PlayerPosition;

        public Quaternion PlayerRotation;

        public PlayerLocationMessage() { }

        public PlayerLocationMessage(string playFabId, Vector3 playerPos, Quaternion playerRot)
        {
            PlayFabId = playFabId;
            PlayerPosition = playerPos;
            PlayerRotation = playerRot;
        }
    }

    public class ShutdownMessage : MessageBase {}

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