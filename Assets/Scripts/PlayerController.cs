using System;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    // Movement
    [SerializeField] private float _movementMultiplier;
    [SerializeField] public Transform CameraRig;
    [SerializeField] public Transform Projectile;

    private Rigidbody _player;
    private float _distToGround;
    private bool _isMovingHorizontally = false;

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
        RotatePlayerToFaceMouse();
        LeftClick();
    }

    private void PerformHorizontalMovement()
    {
        var moveHorizontal = Input.GetAxis("Horizontal");
        var moveVertical = Input.GetAxis("Vertical");

        if (moveHorizontal == 0 && moveVertical == 0)
        {
            _player.velocity = new Vector3(0f, _player.velocity.y, 0f);
            _isMovingHorizontally = false;
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
            _isMovingHorizontally = true;
        }
    }

    public void RotatePlayerToFaceMouse()
    {
        var camera = CameraRig.GetComponentInChildren<Camera>(false);
        Ray camRay = camera.ScreenPointToRay(Input.mousePosition);

        RaycastHit floorHit;
        // Perform the raycast and if it hits something on the floor layer...
        if (Physics.Raycast(camRay, out floorHit, 100f))
        {
            // Create a vector from the player to the point on the floor the raycast from the mouse hit.
            Vector3 playerToMouse = floorHit.point - transform.position;

            // Ensure the vector is horizontal to the player
            playerToMouse.y = transform.position.y;

            Debug.Log(playerToMouse);
            transform.LookAt(playerToMouse);
            //Quaternion newRotation = Quaternion.LookRotation(playerToMouse);
        }

        ////var mouseX = Input.GetAxis("Mouse X");
        ////var mouseY = Input.GetAxis("Mouse Y");
        //var mouse = Input.mousePosition;
        
        //var camera = CameraRig.GetComponentInChildren<Camera>(false);
        //var mouseWorldPoint = camera.ScreenToWorldPoint(new Vector3(mouse.x, mouse.y, transform.position.y));
        ////var forward = mouseWorldPoint - transform.position;

        //// Rotate player to face the direction of movement
        //transform.LookAt(mouseWorldPoint);
        //Debug.Log(mouseWorldPoint);
        ////transform.forward = Vector3.Lerp(transform.forward, new Vector3(mouseX, 0f, mouseY), 10 * Time.deltaTime);
    }

    public void LeftClick()
    {
        var autoAttack = Input.GetMouseButton(0);
        if (autoAttack && DateTime.Now - _lastAttackTime > TimeSpan.FromMilliseconds(_timeBetweenAutoAttacksMs))
        {
            _lastAttackTime = DateTime.Now;
            var projectile = Instantiate(Projectile);

            // Determine where the projective starts relative to the player
            projectile.transform.SetPositionAndRotation(transform.position + transform.forward * .75f, Quaternion.identity);

            var projectileController = projectile.GetComponent<ProjectileController>();
            projectile.GetComponent<Rigidbody>().velocity = transform.forward * projectileController.ProjectileSpeed;
            
            // Ensure projectile does not collide with player or the enemy
            Physics.IgnoreCollision(projectile.GetComponent<Collider>(), _player.GetComponent<Collider>());
        }
    }
}
