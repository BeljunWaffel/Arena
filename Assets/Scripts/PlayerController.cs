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
        AutoAttack();
        //PerformVerticalMovement();
        //FireProjectile();
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

            // Rotate player to face the direction of movement
            transform.forward = Vector3.Lerp(transform.forward, new Vector3(movement.x, 0f, movement.z), 10 * Time.deltaTime);

            // Rotate player to face the direction of movement
            //transform.forward = Vector3.Lerp(transform.forward, new Vector3(movement.x, 0f, movement.z), 10 * Time.deltaTime);

            _player.velocity = movement;
            _isMovingHorizontally = true;
        }
    }

    public void AutoAttack()
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

            // If you find an enemy, lob a projectile at it. Else shoot straight forward
            //var enemy = FindObjectOfType<EnemyController>();
            //if (enemy)
            //{
            //    var secondsToEnemy = 2;
            //    var enemyExpectedPosition = enemy.GetExpectedPositionInMilliSeconds(secondsToEnemy * 1000);
            //    var directionToEnemy = enemyExpectedPosition - projectile.transform.position;
            //    var distanceToEnemy = Mathf.Sqrt(Mathf.Pow((enemy.transform.position.x - projectile.transform.position.x), 2) + Mathf.Pow((enemy.transform.position.z - projectile.transform.position.z), 2));

            //    projectile.GetComponent<Rigidbody>().mass = 5;
            //    projectile.GetComponent<Rigidbody>().useGravity = true;
            //    projectile.GetComponent<Rigidbody>().velocity = new Vector3(directionToEnemy.x / secondsToEnemy, -1 * Physics.gravity.y, directionToEnemy.z / secondsToEnemy);
            //}
            //else
            //{
            //    projectile.GetComponent<Rigidbody>().velocity = transform.forward * projectileController.ProjectileSpeed;
            //}

            // Ensure projectile does not collide with player or the enemy
            Physics.IgnoreCollision(projectile.GetComponent<Collider>(), _player.GetComponent<Collider>());
            //Physics.IgnoreCollision(projectile.GetComponent<Collider>(), enemy.GetComponent<Collider>());
        }
    }
}
