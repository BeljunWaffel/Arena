using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField] private Transform _targetObject;    // The object to follow
    [SerializeField] private int _cameraMoveSpeed;

    void Start()
    {
        if (!_targetObject)
        {
            Debug.Log("No Target Object has been set for the player camera");
            return;
        }
    }

    private void FixedUpdate()
    {
        FollowTarget(Time.deltaTime);
    }

    private void FollowTarget(float deltaTime)
    {
        if (_targetObject == null)
        {
            return;
        }

        // Move the rig towards target position.
        var newCameraPos = Vector3.Lerp(transform.position, _targetObject.position, deltaTime * _cameraMoveSpeed);
        transform.position = new Vector3(newCameraPos.x, transform.position.y, newCameraPos.z);
    }
}