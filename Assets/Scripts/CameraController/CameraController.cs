using System.Collections;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    // This script is designed to be placed on the root object of a camera rig,
    // comprising 3 gameobjects, each parented to the next:
    // 	Camera Rig
    // 		Pivot
    // 			Camera

    [SerializeField] public Transform TargetObject;    // The object to follow

    [Range(0f, 10f)] [SerializeField] private float _turnSpeed = 1.5f;   // How fast the rig will rotate from user input.
    [SerializeField] private int _cameraMoveSpeed = 5;
    [SerializeField] private float _tiltMin = 45f;  // The min/max values of the x-axis rotation of the pivot. (Determines how far up/down the camera can look)
    [SerializeField] private float _tiltMax = 75f;

    // Pivot
    private Transform _pivot;
    private Quaternion _pivotStartingRotation;
    private Vector3 _pivotStartingLocalPosition;
    private Vector3 _pivotEulers;

    // Camera
    private Transform _camera;
    private Vector3 _cameraStartingLocalPosition;

    // Transform
    private float _lookAngle;
    private float _tiltAngle;
    private Quaternion _transformStartRotation;

    void Start()
    {
        if (!TargetObject)
        {
            return;
        }

        // Pivot
        _pivot = transform.GetChild(0);
        _pivotStartingLocalPosition = _pivot.localPosition;
        _pivotStartingRotation = _pivot.localRotation;
        _pivotEulers = _pivot.rotation.eulerAngles;

        // Camera
        _camera = GetComponentInChildren<Camera>().transform;
        _cameraStartingLocalPosition = _camera.localPosition;

        // Transform
        _transformStartRotation = transform.localRotation;
    }

    private void FixedUpdate()
    {
        FollowTarget(Time.deltaTime);
    }

    private void FollowTarget(float deltaTime)
    {
        if (TargetObject == null)
        {
            return;
        }

        // Move the rig towards target position.
        transform.position = Vector3.Lerp(transform.position, TargetObject.position, deltaTime * _cameraMoveSpeed);
    }

    private void HandleRotationMovement()
    {
        if (Time.timeScale < float.Epsilon)
        {
            return;
        }

        if (Input.GetMouseButton(0))
        {
            var x = Input.GetAxis("Mouse X");
            var y = Input.GetAxis("Mouse Y");

            // Adjust the look angle by an amount proportional to the turn speed and horizontal input.
            _lookAngle += x * _turnSpeed;

            // Rotate the rig (the root object) around Y axis only:
            var transformTargetRot = Quaternion.Euler(0f, _lookAngle, 0f);

            _tiltAngle -= y * _turnSpeed;
            _tiltAngle = Mathf.Clamp(_tiltAngle, -_tiltMin, _tiltMax);

            // Tilt input around X is applied to the pivot (the child of this object)
            var pivotTargetRot = Quaternion.Euler(_tiltAngle, _pivotEulers.y, _pivotEulers.z);

            _pivot.localRotation = Quaternion.Slerp(_pivot.localRotation, pivotTargetRot, 10 * Time.deltaTime);
            transform.localRotation = Quaternion.Slerp(transform.localRotation, transformTargetRot, 10 * Time.deltaTime);
        }
    }
}