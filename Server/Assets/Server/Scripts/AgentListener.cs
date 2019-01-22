using System.Collections;
using UnityEngine;
using PlayFab;
using System;
using PlayFab.Networking;
using UnityEngine.Networking;

public class AgentListener : MonoBehaviour {
    public UnityNetworkServer UNetServer;
    public bool Debugging = false;
    // Use this for initialization
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
        SendEventToOtherClients(CustomGameServerMessageTypes.PlayerAddedMessage, message, ""/*message.PlayFabId*/);
    }

    private void OnPlayerLocationReceived(PlayerLocationMessage message)
    {
        Debug.Log($"Player {message.PlayFabId} location: {message.PlayerLocation}");
        SendEventToOtherClients(CustomGameServerMessageTypes.PlayerLocationMessage, message, message.PlayFabId);
    }

    private void SendEventToOtherClients(short messageType, MessageBase message, string ignoreId)
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
