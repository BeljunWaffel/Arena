using Assets.Server.Scripts;
using PlayFab;
using PlayFab.AgentModels;
using PlayFab.Networking;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class AgentListener : MonoBehaviour {
    public UnityNetworkServer UNetServer;
    public bool Debugging = false;

    private float _timer = 0f;
    private float _timer2 = 0f;
    private Vector3 _curr;
    private bool _wasAdded = false;

    void Start () {

        PlayFabMultiplayerAgentAPI.Start();
        PlayFabMultiplayerAgentAPI.IsDebugging = Debugging;
        PlayFabMultiplayerAgentAPI.OnMaintenance += OnMaintenance;
        PlayFabMultiplayerAgentAPI.OnShutDown += OnShutdown;
        PlayFabMultiplayerAgentAPI.OnServerActive += OnServerActive;
        PlayFabMultiplayerAgentAPI.OnAgentError += OnAgentError;

        UNetServer.OnPlayerAdded.AddListener(OnPlayerAdded);
        UNetServer.OnPlayerInfoReceived.AddListener(OnPlayerInfoReceived);
        UNetServer.OnProjectileFiredReceived.AddListener(OnProjectileFired);
        UNetServer.OnPlayerRemoved.AddListener(OnPlayerRemoved);
        UNetServer.OnPlayerDeadReceived.AddListener(OnPlayerDead);

        StartCoroutine(ReadyForPlayers());

        // Debugging
        //_curr = new Vector3(2.5f, 0.5f, 2.5f);
        //var message = new PlayerInfoMessage(new PlayerInfo { "1234", _curr, Quaternion.identity } ));
        //OnPlayerAdded(message);

        //_curr = new Vector3(2.5f, 0.5f, 5f);
        //var message2 = new PlayerInfoMessage(new PlayerInfo("1235", _curr, Quaternion.identity));
        //OnPlayerAdded(message2);
    }

    // FOR DEBUGGING AND SENDING FAKE EVENTS

    //private void Update()
    //{
    //    _timer += Time.deltaTime;
    //    _timer2 += Time.deltaTime;
    //    var jump = Input.GetAxis("Jump");
    //    if (jump != 0 && _timer > 5f)
    //    {
    //        _timer = 0f;
    //        Debug.Log("Sending player added message");
    //        _curr = new Vector3(2.5f, 0.5f, 2.5f);

    //        var playerInfo = new PlayerInfo()
    //        {
    //            PlayFabId = "1234",
    //            PlayerPosition = new Vector3(2, .5f, 2),
    //            PlayerRotation = Quaternion.identity,
    //            Health = 5
    //        };

    //        var message = new PlayerInfoMessage(playerInfo);
    //        OnPlayerAdded(message);
    //        _wasAdded = true;
    //    }

    //    //if (_wasAdded && _timer2 > 1f)
    //    //{
    //    //    _timer2 = 0f;
    //    //    var info = new PlayerInfo("1234", _curr += new Vector3(.3f, 0f, 0f), Quaternion.identity);
    //    //    SendEventToOtherClients(CustomGameServerMessageTypes.PlayerInfoMessage, new PlayerInfoMessage(info));
    //    //}
    //}

    IEnumerator ReadyForPlayers()
    {
        yield return new WaitForSeconds(.5f);
        PlayFabMultiplayerAgentAPI.ReadyForPlayers();
    }
    
    private void OnServerActive()
    {
        UNetServer.StartServer();
        Debug.Log("Server Started From Agent Activation");
    }

    private void OnPlayerRemoved(string playfabId)
    {
        PlayFabMultiplayerAgentAPI.RemovePlayer(playfabId);
    }

    private void OnPlayerDead(string playFabId)
    {
        Debug.Log($"player {playFabId} dead");
        if (PlayFabMultiplayerAgentAPI.Players.ContainsKey(playFabId))
        {
            SendEventToOtherClients(CustomGameServerMessageTypes.PlayerDeadMessage, new PlayerIdMessage(playFabId), ignoreId: playFabId);
        }

        PlayerInfo respawnInfo = new PlayerInfo()
        {
            PlayFabId = playFabId,
            PlayerPosition = new Vector3(-2, .5f, -2),
            PlayerRotation = Quaternion.identity,
            Health = 5
        };
        TimedEvent te = new TimedEvent(5f, CustomGameServerMessageTypes.PlayerRespawnMessage, new PlayerInfoMessage(respawnInfo));
        GetComponent<TimedEventController>().TimedEvents.Add(te);
        //System.Diagnostics.Stopwatch respawnTimer = new System.Diagnostics.Stopwatch();
        //respawnTimer.Start();
        //Debug.Log($"RespawnTimer 1: {respawnTimer.ElapsedMilliseconds}");
        //while (respawnTimer.ElapsedMilliseconds < 5f)
        //{
        //}

        //Debug.Log($"RespawnTimer 2: {respawnTimer.ElapsedMilliseconds}");
        //PlayerInfo respawnInfo = new PlayerInfo()
        //{
        //    PlayFabId = playFabId,
        //    PlayerPosition = new Vector3(-2, .5f, -2),
        //    PlayerRotation = Quaternion.identity,
        //    Health = 5
        //};
        //SendEventToOtherClients(CustomGameServerMessageTypes.PlayerRespawnMessage, new PlayerInfoMessage(respawnInfo));
    }

    private void OnPlayerAdded(PlayerInfoMessage message)
    {
        var playFabId = message.Internal.PlayFabId;
        Debug.Log($"Player {playFabId} added!");

        // If there are other players than the one that just connected, send their data over
        if (PlayFabMultiplayerAgentAPI.Players.Count > 0)
        {
            var playerInfoArray = new PlayerInfo[PlayFabMultiplayerAgentAPI.Players.Count];
            var counter = 0;
            foreach (var playerId in PlayFabMultiplayerAgentAPI.Players.Keys)
            {
                playerInfoArray[counter] = new PlayerInfo(PlayFabMultiplayerAgentAPI.Players[playerId]);
                counter++;
            }
            var existingPlayersMessage = new PlayerInfoMessages(playerInfoArray);

            foreach (var item in existingPlayersMessage.Internal)
            {
                Debug.Log($"{item.PlayFabId}, {item.PlayerPosition}, {item.PlayerRotation}");
            }
            SendEventToClient(playFabId, CustomGameServerMessageTypes.PlayersAddedMessage, existingPlayersMessage);
        }

        // Then add this player, and send their data to other clients.
        PlayFabMultiplayerAgentAPI.AddPlayer(message.Internal);
        SendEventToOtherClients(CustomGameServerMessageTypes.PlayerAddedMessage, message, ignoreId: playFabId);
    }

    private void OnPlayerInfoReceived(PlayerInfoMessage message)
    {
        var playFabId = message.Internal.PlayFabId;
        // Only start sending location for a given player once they have been added.
        if (PlayFabMultiplayerAgentAPI.Players.ContainsKey(playFabId))
        {
            PlayFabMultiplayerAgentAPI.Players[playFabId].UpdateInfo(message.Internal);
            //Debug.Log($"Player {message.Internal.PlayFabId} location: {message.Internal.PlayerPosition}");
            SendEventToOtherClients(CustomGameServerMessageTypes.PlayerInfoMessage, message, ignoreId: playFabId);
        }
    }

    private void OnProjectileFired(ProjectileFiredMessage message)
    {
        if (PlayFabMultiplayerAgentAPI.Players.ContainsKey(message.PlayFabId))
        {
            //Debug.Log($"{message.ProjectileStartingPosition}, {message.ProjectileVelocity}");
            SendEventToOtherClients(CustomGameServerMessageTypes.ProjectileFiredMessage, message, ignoreId: message.PlayFabId);
        }
    }

    public void SendEventToOtherClients(short messageType, MessageBase message, string ignoreId = "")
    {
        foreach (var conn in UNetServer.Connections)
        {
            if (conn.PlayFabId != ignoreId)
            {
                conn.Connection.Send(messageType, message);
            }
        }
    }

    private void SendEventToClient(string playFabId, short messageType, MessageBase message)
    {
        foreach (var conn in UNetServer.Connections)
        {
            if (conn.PlayFabId == playFabId)
            {
                conn.Connection.Send(messageType, message);
            }
        }
    }

    private void OnAgentError(string error)
    {
        Debug.Log(error);
    }

    private void OnShutdown()
    {
        Debug.Log("Server is Shutting down");
        foreach(var conn in UNetServer.Connections)
        {
            conn.Connection.Send(CustomGameServerMessageTypes.ShutdownMessage, new ShutdownMessage());
        }
        StartCoroutine(Shutdown());
    }

    IEnumerator Shutdown()
    {
        yield return new WaitForSeconds(5f);
        Application.Quit();
    }

    private void OnMaintenance(DateTime? NextScheduledMaintenanceUtc)
    {
        Debug.LogFormat("Maintenance Scheduled for: {0}", NextScheduledMaintenanceUtc.Value.ToLongDateString());
        foreach (var conn in UNetServer.Connections)
        {
            conn.Connection.Send(CustomGameServerMessageTypes.ShutdownMessage, new MaintenanceMessage() {
                ScheduledMaintenanceUTC = (DateTime)NextScheduledMaintenanceUtc
            });
        }
    }
}
