using Assets.Scripts.PlayerScripts;
using UnityEngine;
using UnityEngine.Networking;

namespace Assets.Scripts.ServiceHelpers
{
    public static class ServerCommunicator
    {
        private static UnityNetworkingClient _unc = UnityNetworkingClient.Instance;

        private static GameObject _enemiesContainerBacking;
        private static GameObject _enemiesContainer
        {
            get
            {
                if (_enemiesContainerBacking == null)
                {
                    _enemiesContainerBacking = GameObject.Find("Enemies");
                }
                return _enemiesContainerBacking;
            }
        }

        public static void SendLocation(string playfabId, Vector3 localPos, Quaternion localRot)
        {
            _unc.Client?.connection?.Send(UnityNetworkingClient.CustomGameServerMessageTypes.PlayerLocationMessage, 
                new UnityNetworkingClient.PlayerLocationMessage(
                    playfabId,
                    localPos,
                    localRot
                ));
        }

        public static void OnPlayerLocationReceived(NetworkMessage netMsg)
        {
            var message = netMsg.ReadMessage<UnityNetworkingClient.PlayerLocationMessage>();
            Debug.Log($"Player {message.PlayFabId} location: {message.PlayerPosition}");

            var enemies = _enemiesContainer.transform;
            foreach (Transform enemy in enemies)
            {
                var playerMetadata = enemy.GetComponentInChildren<PlayerMetadata>();
                var id = playerMetadata.PlayFabId;
                var name = enemy.name;
                if (id == message.PlayFabId)
                {
                    enemy.localPosition = message.PlayerPosition;
                    enemy.localRotation = message.PlayerRotation;
                }
            }
        }
    }
}
