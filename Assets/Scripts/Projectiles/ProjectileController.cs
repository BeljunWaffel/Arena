using System.Diagnostics;
using UnityEngine;
using static Constants;

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

    void OnCollisionStay(Collision collision)
    {
        // Don't do anything if colliding with a projectile
        if (TagList.ContainsTag(collision.gameObject, Tag.Projectile))
        {
            return;
        }

        if (TagList.ContainsTag(collision.gameObject, Tag.Player))
        {
            UnityEngine.Debug.Log("Projectile hit other player");
            Destroy(gameObject);
        }
        else
        {
            UnityEngine.Debug.Log("Projectile hit a non-player");
        }

        Destroy(gameObject);
    }

    private bool IsCollidingWithEnemyPlayer(Collision collision)
    {
        return TagList.ContainsTag(collision.gameObject, Tag.Player);
    }
}
