using Assets.Scripts.PlayerScripts;
using Assets.Scripts.SharedScripts;
using Assets.Scripts.Utils;
using System.Diagnostics;
using UnityEngine;

namespace Assets.Scripts.Projectiles
{
    public class ProjectileController : MonoBehaviour
    {
        public float ProjectileSpeed = 10;
        private Stopwatch _timeToLive = new Stopwatch();

        void Start()
        {
            _timeToLive.Start();
        }

        private void Update()
        {
            if (_timeToLive.ElapsedMilliseconds >= 10000)
            {
                Destroy(gameObject);
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            // Don't do anything if colliding with a projectile
            if (TagList.ContainsTag(collision.gameObject, Tag.Projectile))
            {
                return;
            }

            // Player will report when they get hit.
            if (TagList.ContainsTag(collision.gameObject, Tag.Player))
            {
                UnityEngine.Debug.Log("Projectile hit player");
                collision.gameObject.GetComponent<HealthControllerBase>().TakeDamage(1);
            }

            Destroy(gameObject);
        }

        private bool IsCollidingWithEnemyPlayer(Collision collision)
        {
            return TagList.ContainsTag(collision.gameObject, Tag.Player);
        }
    }
}