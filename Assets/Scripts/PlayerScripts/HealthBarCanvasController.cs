using Assets.Scripts.SharedScripts;
using UnityEngine;

namespace Assets.Scripts.PlayerScripts
{
    class HealthBarCanvasController : MonoBehaviour
    {
        [SerializeField] private Transform _targetObject;

        private FollowerController _follower;

        private void Awake()
        {
            if (!_targetObject)
            {
                Debug.Log("No Target Object has been set for the player camera");
                return;
            }

            _follower = new FollowerController(_targetObject, transform);
        }

        private void FixedUpdate()
        {
            _follower.FollowTarget();
        }
    }
}
