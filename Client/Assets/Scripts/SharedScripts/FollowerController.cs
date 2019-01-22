using UnityEngine;

namespace Assets.Scripts.SharedScripts
{
    /// <summary>
    /// A controller that follows a target object
    /// </summary>
    class FollowerController
    {        
        private Transform _target;
        private Transform _follower;

        public FollowerController(Transform targetObject, Transform followerObject)
        {
            _target = targetObject;
            _follower = followerObject;
        }
        
        public void FollowTarget()
        {
            _follower.position = new Vector3(_target.position.x, _follower.position.y, _target.position.z);
        }

        public void FollowTargetLerp(float moveSpeed)
        {
            _follower.position = Vector3.Lerp(_follower.position, _target.position, Time.deltaTime * moveSpeed);
        }
    }
}
