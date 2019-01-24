using System.Collections;
using UnityEngine;
using PlayFab;
using System;
using PlayFab.Networking;
using UnityEngine.Networking;

public class AgentListener : MonoBehaviour {
    public UnityNetworkServer UNetServer;
    public bool Debugging = false;
    private float _timer = 0f;

    void Start () {

        PlayFabMultiplayerAgentAPI.Start();
        PlayFabMultiplayerAgentAPI.IsDebugging = Debugging;
        PlayFabMultiplayerAgentAPI.OnMaintenance += OnMaintenance;
        PlayFabMultiplayerAgentAPI.OnShutDown += OnShutdown;
        PlayFabMultiplayerAgentAPI.OnServerActive += OnServerActive;
        PlayFabMultiplayerAgentAPI.OnAgentError += OnAgentError;

        UNetServer.OnPlayerAdded.AddListener(OnPlayerAdded);
        UNetServer.OnPlayerLocationReceived.AddListener(OnPlayerLocationReceived);
        UNetServer.OnPlayerRemoved.AddListener(OnPlayerRemoved);

        StartCoroutine(ReadyForPlayers());
    }

    // FOR DEBUGGING AND SENDING FAKE EVENTS

    //private void Update()
    //{
    //    _timer += Time.deltaTime;
    //    var jump = Input.GetAxis("Jump");
    //    if (jump != 0 && _timer > 5f)
    //    {
    //        _timer = 0f;
    //        Debug.Log("Sending player added message");
    //        var message = new PlayerLocationMessage()
    //        {
    //            PlayFabId = "1234",
    //            PlayerLocation = new Vector3(2, .5f, 2)
    //        };
    //        SendEventToOtherClients(CustomGameServerMessageTypes.PlayerAddedMessage, message);
    //    }        
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

    private void OnPlayerAdded(PlayerLocationMessage message)
    {
        Debug.Log($"Player added! {message.PlayFabId}");
        PlayFabMultiplayerAgentAPI.AddPlayer(message.PlayFabId);
        SendEventToOtherClients(CustomGameServerMessageTypes.PlayerAddedMessage, message, 
            ignoreId: message.PlayFabId);
    }

    private void OnPlayerLocationReceived(PlayerLocationMessage message)
    {
        // Only start sending location for a given player once they have been added.
        if (PlayFabMultiplayerAgentAPI.HasPlayer(message.PlayFabId))
        {
            Debug.Log($"Player {message.PlayFabId} location: {message.PlayerLocation}");
            SendEventToOtherClients(CustomGameServerMessageTypes.PlayerLocationMessage, message,
                ignoreId: message.PlayFabId);
        }
    }

    private void SendEventToOtherClients(short messageType, MessageBase message, string ignoreId = "")
    {
        foreach (var conn in UNetServer.Connections)
        {
            if (conn.PlayFabId != ignoreId)
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
