using UnityEngine;
using UnityEngine.Networking;

namespace Assets.Scripts.ServiceHelpers
{
    public static class ServerCommunicator
    {
        private static UnityNetworkingClient _unc = UnityNetworkingClient.Instance;

        public static void SendLocation(string playfabId, Vector3 localPos)
        {
            _unc.Client?.connection?.Send(UnityNetworkingClient.CustomGameServerMessageTypes.PlayerLocationMessage, new UnityNetworkingClient.PlayerLocationMessage()
            {
                PlayFabId = playfabId,
                PlayerLocation = localPos
            });
        }

        public static void OnPlayerLocationReceived(NetworkMessage netMsg)
        {
            var message = netMsg.ReadMessage<UnityNetworkingClient.PlayerLocationMessage>();
            Debug.Log($"Player {message.PlayFabId} location: {message.PlayerLocation}");
        }
    }
}
