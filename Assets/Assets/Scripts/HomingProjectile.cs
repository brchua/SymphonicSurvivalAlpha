using UnityEngine;

// Put  on a small prefab (e.g. a sphere or a sprite quad). No collider or Rigidbody needed.
// Curves toward its target on the X-Z plane and damages it when close enough.
public class HomingProjectile : MonoBehaviour
{
    [SerializeField] private float speed = 12f;
    [SerializeField] private float turnSpeed = 540f;     // degrees per second, higher = sharper homing
    [SerializeField] private float hitRadius = 0.6f;     // how close counts as a hit
    [SerializeField] private float lifetime = 5f;        // disappears after this many seconds
    [SerializeField] private float retargetInterval = 0.2f;

    private Enemy target;
    private float damage;
    private Vector3 direction = Vector3.right;
    private float retargetTimer;

    public void Launch(Enemy newTarget, float newDamage, Vector3 startDirection)
    {
        target = newTarget;
        damage = newDamage;

        startDirection.y = 0f;
        if (startDirection.sqrMagnitude > 0.001f)
            direction = startDirection.normalized;

        Destroy(gameObject, lifetime);
    }

    private void Update()
    {
        // target died? look for the nearest enemy instead (checked a few times per second, not every frame)
        if (target == null)
        {
            retargetTimer -= Time.deltaTime;
            if (retargetTimer <= 0f)
            {
                retargetTimer = retargetInterval;
                target = FindNearestEnemy();
            }
        }

        if (target != null)
        {
            Vector3 toTarget = target.transform.position - transform.position;
            toTarget.y = 0f;

            if (toTarget.magnitude <= hitRadius)
            {
                target.TakeDamage(damage);
                Destroy(gameObject);
                return;
            }

            direction = Vector3.RotateTowards(
                direction, toTarget.normalized, turnSpeed * Mathf.Deg2Rad * Time.deltaTime, 0f);
        }

        transform.position += direction * speed * Time.deltaTime;
    }

    private Enemy FindNearestEnemy()
    {
        Enemy best = null;
        float bestDist = float.MaxValue;

        foreach (Enemy e in FindObjectsByType<Enemy>(FindObjectsSortMode.None))
        {
            float d = (e.transform.position - transform.position).sqrMagnitude;
            if (d < bestDist)
            {
                bestDist = d;
                best = e;
            }
        }
        return best;
    }
}