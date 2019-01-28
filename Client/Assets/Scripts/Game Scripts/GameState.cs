using Assets.Scripts.PlayerScripts;
using Assets.Scripts.Projectiles;
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

        private void Awake()
        {
            _players = new Dictionary<string, Transform>();
            _enemiesContainer = EnemiesContainerBacking;
            _enemyPrefab = EnemyPrefabBacking;
            _projectilePrefab = ProjectilePrefabBacking;

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

        private static bool TryGetPlayer(string playerId, out Transform player)
        {
            return _players.TryGetValue(playerId, out player);
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
                    break;
                }
            }
        }

        public static void RemovePlayer(string playFabId)
        {
            _players.Remove(playFabId);
        }

        public static void UpdatePlayerInfo(PlayerInfo info)
        {
            Transform player;
            if (TryGetPlayer(info.PlayFabId, out player))
            {
                player.transform.localPosition = info.PlayerPosition;
                player.transform.localRotation = info.PlayerRotation;
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
            if (TryGetPlayer(message.PlayFabId, out shootingPlayer))
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
