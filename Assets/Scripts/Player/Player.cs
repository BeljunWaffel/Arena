using Assets.Scripts.Utils;
using UnityEngine;

namespace Assets.Scripts.Player
{
    class Player : MonoBehaviour
    {
        public int Health;

        void Start()
        {
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (TagList.ContainsTag(collision.gameObject, Tag.Projectile))
            {
                Health--;

                Debug.Log(GetComponent<Rigidbody>().velocity);
            }
        }
    }
}