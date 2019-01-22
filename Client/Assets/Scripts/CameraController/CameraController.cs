using Assets.Scripts.SharedScripts;
using UnityEngine;

namespace Assets.Scripts.CameraController
{
    public class CameraController : MonoBehaviour
    {
        [SerializeField] private Transform _targetObject;    // The object to follow
        [SerializeField] private int _cameraMoveSpeed;

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
            _follower.FollowTargetLerp(_cameraMoveSpeed);
        }
    }
}