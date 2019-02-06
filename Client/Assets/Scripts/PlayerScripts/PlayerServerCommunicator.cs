using Assets.Scripts.ServiceHelpers;
using UnityEngine;

namespace Assets.Scripts.PlayerScripts
{
    public class PlayerServerCommunicator : MonoBehaviour
    {
        // Communication with server
        [SerializeField] private ServerCommunicator _serverCommunicator;

        private PlayerMetadata _playerMetadata;

        void Start()
        {
            _playerMetadata = GetComponent<PlayerMetadata>();
        }

        private void Update()
        {
            _serverCommunicator.SendPlayerInfo(_playerMetadata.PlayFabId, transform);
        }
    }
}