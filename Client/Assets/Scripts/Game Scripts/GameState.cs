using Assets.Scripts.PlayerScripts;
using Assets.Scripts.Projectiles;
using Assets.Scripts.ServiceHelpers;
using Assets.Scripts.SharedScripts;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using static Assets.Scripts.ServiceHelpers.UnityNetworkingClient;

namespace Assets.Scripts.Game_Scripts
{
    public class GameState : MonoBehaviour
    {
        [SerializeField] private GameObject EnemiesContainerBacking;
        [SerializeField] private Transform EnemyPrefabBacking;
        [SerializeField] private Transform ProjectilePrefabBacking;

        private static Dictionary<string, Transform> _players;
        private static MessageWindow _messageWindow;
        private static GameObject _enemiesContainer;
        private static Transform _enemyPrefab;
        private static Transform _projectilePrefab;

        private static ServerCommunicator _serverCommunicator;

        private void Awake()
        {
            _players = new Dictionary<string, Transform>();
            _enemiesContainer = EnemiesContainerBacking;
            _enemyPrefab = EnemyPrefabBacking;
            _projectilePrefab = ProjectilePrefabBacking;

            _serverCommunicator = GetComponent<ServerCommunicator>();

            if (_enemiesContainer == null)
            {
                throw new ArgumentNullException("EnemiesContainer not properly passed into GameState script.");
            }

            if (_enemyPrefab == null)
            {
                throw new ArgumentNullException("EnemyPrefab not properly passed into GameState script.");
            }
        }

        private void Start()
        {
            _messageWindow = MessageWindow.Instance;
        }

        public static void AddCurrentPlayerInfo(string playFabId, Transform currentPlayer)
        {
            _players.Add(playFabId, currentPlayer);
        }

        public static void CreatePlayer(PlayerInfo playerInfo)
        {
            var playFabId = playerInfo.PlayFabId;

            _messageWindow.Title.text = $"Player {playFabId} joined the game!";
            _messageWindow.Message.text = string.Empty;
            _messageWindow.gameObject.SetActive(true);
            Debug.Log($"Player {playFabId} joined game! Location: {playerInfo.PlayerPosition}");

            // Create enemy
            var enemyContainer = Instantiate(_enemyPrefab, _enemiesContainer.transform);
            enemyContainer.gameObject.SetActive(true);
            enemyContainer.name = $"enemy{playerInfo.PlayFabId}";
            enemyContainer.localPosition = Vector3.zero;
            enemyContainer.localRotation = Quaternion.identity;                      

            // Set enemy playfabId + pos/rot
            for (int i = 0; i < enemyContainer.childCount; i++)
            {
                var child = enemyContainer.GetChild(i);
                var playerMetadata = child.GetComponent<PlayerMetadata>();
                if (playerMetadata != null)
                {
                    playerMetadata.PlayFabId = playFabId;
                    child.localPosition = playerInfo.PlayerPosition;
                    child.localRotation = playerInfo.PlayerRotation;
                    _players[playFabId] = child;
                    
                    var enemyHealth = child.GetComponent<HealthControllerBase>();
                    enemyHealth.StartingHealth = playerInfo.Health;
                    enemyHealth.SetCurrentHealth(playerInfo.Health);
                    break;
                }
            }
        }

        public static void RemovePlayer(string playFabId)
        {
            Transform player;
            if (_players.TryGetValue(playFabId, out player))
            {
                _serverCommunicator.SendPlayerDead(playFabId);
                Destroy(player.parent.gameObject);
                _players.Remove(playFabId);
            }
            else
            {
                Debug.Log($"Player {playFabId} has already been removed.");
            }
        }

        public static void UpdatePlayerInfo(PlayerInfo info)
        {
            Transform player;
            if (_players.TryGetValue(info.PlayFabId, out player))
            {
                player.transform.localPosition = info.PlayerPosition;
                player.transform.localRotation = info.PlayerRotation;
                player.GetComponent<HealthControllerBase>().SetCurrentHealth(info.Health);
            }
            else
            {
                Debug.Log($"Player {info.PlayFabId} does not exist / was not added to players dictionary");
            }
        }

        public static void CreateProjectile(ProjectileFiredMessage message)
        {
            var projectile = Instantiate(_projectilePrefab);

            // Determine where the projective starts relative to the player
            projectile.transform.SetPositionAndRotation(message.ProjectileStartingPosition, message.ProjectileStartingRotation);
            projectile.GetComponent<Rigidbody>().velocity = message.ProjectileVelocity;

            // Ensure projectile does not collide with player or the enemy
            Transform shootingPlayer;
            if (_players.TryGetValue(message.PlayFabId, out shootingPlayer))
            {
                Physics.IgnoreCollision(projectile.GetComponent<Collider>(), shootingPlayer.GetComponent<Collider>());
            }
            else
            {
                Debug.Log("Couldn't find player that fired projectile.");
            }
        }

        void FixedUpdate()
        {
            //var escape = Input.GetAxis("Escape");
            //if (escape > 0)
            //{
            //    SceneManager.LoadScene("MainMenu");
            //}
        }
    }
}
