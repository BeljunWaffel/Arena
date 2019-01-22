using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.PlayerScripts
{
    public class PlayerHealth : MonoBehaviour
    {
        [SerializeField] private int _startingHealth;
        private int _currentHealth;
        [SerializeField] private Image _healthBar;
        [SerializeField] private Image _damageImage;
        [SerializeField] private float _flashSpeed = 5f;
        [SerializeField] private Color _flashColour = new Color(1f, 0f, 0f, 0.1f);

        private bool _isDead;
        private bool _justGotDamaged;

        private System.Diagnostics.Stopwatch s;

        void Awake()
        {
            _currentHealth = _startingHealth;
            _isDead = false;
        }

        private void Start()
        {
            s = new System.Diagnostics.Stopwatch();
            s.Start();
        }

        void Update()
        {
            //if (s.ElapsedMilliseconds > 2500 && _currentHealth > 1)
            //{
            //    TakeDamage(1);
            //    s.Restart();
            //}

            if (_justGotDamaged)
            {
                _damageImage.color = _flashColour;
            }
            else
            {
                _damageImage.color = Color.Lerp(_damageImage.color, Color.clear, _flashSpeed * Time.deltaTime);
            }

            _justGotDamaged = false;
        }

        public void TakeDamage(int amount)
        {
            _justGotDamaged = true;

            _currentHealth -= amount;
            _healthBar.fillAmount = _currentHealth / (1.0f * _startingHealth);

            if (_currentHealth <= 0 && !_isDead)
            {
                _isDead = true;

                Debug.Log("Player dead");
                Destroy(gameObject);
            }
        }
    }
}