using Assets.Scripts.SharedScripts;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.PlayerScripts
{
    public class PlayerHealth : HealthControllerBase
    {
        [SerializeField] private Image _damageImage;
        [SerializeField] private float _flashSpeed = 5f;
        [SerializeField] private Color _flashColour = new Color(1f, 0f, 0f, 0.1f);

        private bool _justGotDamaged;

        protected override void Update()
        {
            //if (s.ElapsedMilliseconds > 2500 && _currentHealth > 1)
            //{
            //    TakeDamage(1);
            //    s.Restart();
            //}
            base.Update();

            _damageImage.color = _justGotDamaged ? _flashColour : 
                Color.Lerp(_damageImage.color, Color.clear, _flashSpeed * Time.deltaTime);

            _justGotDamaged = false;
        }

        public override void TakeDamage(int amount)
        {
            _justGotDamaged = true;
            base.TakeDamage(amount);
        }
    }
}