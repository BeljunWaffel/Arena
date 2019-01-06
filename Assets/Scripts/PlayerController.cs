using System;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    // Movement
    [SerializeField] private float _movementMultiplier;
    [SerializeField] private Transform _cameraRig;
    [SerializeField] private Transform _projectile;

    private Rigidbody _player;
    private float _distToGround;

    // Auto-Attack
    [Range(1f, 100f)] [SerializeField] private float _timeBetweenAutoAttacksMs;
    private DateTime _lastAttackTime;

    void Start ()
    {
        _player = GetComponent<Rigidbody>();
    }

    // Applied before physics
    void FixedUpdate()
    {
        PerformHorizontalMovement();
        LookAtMouse();
        LeftClick();
    }

    private void PerformHorizontalMovement()
    {
        var moveHorizontal = Input.GetAxis("Horizontal");
        var moveVertical = Input.GetAxis("Vertical");

        if (moveHorizontal == 0 && moveVertical == 0)
        {
            _player.velocity = new Vector3(0f, _player.velocity.y, 0f);
        }
        else
        {
            // Horizontal movement
            Vector3 movement;
            var multiplier = _movementMultiplier;

            movement = new Vector3(moveHorizontal * multiplier,
                                   _player.velocity.y,
                                   moveVertical * multiplier);

            _player.velocity = movement;
        }
    }

    public void LookAtMouse()
    {
        var mouse = Input.mousePosition;
        var playerCamera = _cameraRig.GetComponentInChildren<Camera>();

        Ray camRay = playerCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit floorHit;

        Debug.DrawRay(camRay.origin, camRay.direction * 100, Color.yellow);

        if (Physics.Raycast(camRay, out floorHit, 200f, LayerMask.GetMask("Floor")))
        {
            Debug.DrawLine(floorHit.point, floorHit.point + new Vector3(1, 1, 1));
            Debug.DrawLine(floorHit.point, floorHit.point + new Vector3(-1, 1, -1));
            transform.LookAt(new Vector3(floorHit.point.x, transform.position.y, floorHit.point.z));
        }
    }

    public void LeftClick()
    {
        var autoAttack = Input.GetMouseButton(0);
        if (autoAttack && DateTime.Now - _lastAttackTime > TimeSpan.FromMilliseconds(_timeBetweenAutoAttacksMs))
        {
            _lastAttackTime = DateTime.Now;
            var projectile = Instantiate(_projectile);

            // Determine where the projective starts relative to the player
            projectile.transform.SetPositionAndRotation(transform.position + transform.forward * .75f, Quaternion.identity);

            var projectileController = projectile.GetComponent<ProjectileController>();
            projectile.GetComponent<Rigidbody>().velocity = transform.forward * projectileController.ProjectileSpeed;
            
            // Ensure projectile does not collide with player or the enemy
            Physics.IgnoreCollision(projectile.GetComponent<Collider>(), _player.GetComponent<Collider>());
        }
    }
}
