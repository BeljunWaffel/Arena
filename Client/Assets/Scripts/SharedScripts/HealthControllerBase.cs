using Assets.Scripts.Game_Scripts;
using Assets.Scripts.PlayerScripts;
using Assets.Scripts.ServiceHelpers;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.SharedScripts
{
    public class HealthControllerBase : MonoBehaviour
    {
        public int CurrentHealth { get; private set; }
        public int StartingHealth;
        [SerializeField] protected Image _healthBar;

        protected bool _isDead;

        protected virtual void Awake()
        {
            CurrentHealth = StartingHealth;
            _isDead = false;
        }

        protected virtual void Update()
        {
        }

        public virtual void SetCurrentHealth(int health)
        {
            if (CurrentHealth == health)
            {
                return;
            }

            CurrentHealth = health;
            _healthBar.fillAmount = CurrentHealth / (1.0f * StartingHealth);
            Debug.Log($"player health = {CurrentHealth}");

            if (CurrentHealth <= 0 && !_isDead)
            {
                _isDead = true;

                // Ensure we send one last event before we declare player dead
                var playerId = transform.GetComponent<PlayerMetadata>().PlayFabId;
                Debug.Log($"Player {playerId} dead.");
                GameState.KillPlayer(playerId, notifyServer: true);
            }
        }

        public virtual void TakeDamage(int amount)
        {
            SetCurrentHealth(CurrentHealth - amount);
        }
    }
}