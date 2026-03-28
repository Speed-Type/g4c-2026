using UnityEngine;

public class SnakeProjectile : MonoBehaviour
{
    [Header("Projectile Function")]
    [Tooltip("If true, this component moves the projectile itself. " +
             "Set false if the prefab has its own movement (Rigidbody, animator, etc).")]
    public bool moveManually = true;

    [Tooltip("Max seconds before the projectile destroys itself if no hit occurs.")]
    public float lifetime = 4f;

    [Tooltip("Radius for head contact detection.")]
    public float hitRadius = 0.5f;

    [Header("Damage")]
    public int damage;

    //----------------------------------------------------------------------------------------------------------------

    //Runtime state (set by SnakeOpponent.FireProjectile via Init)

    private Vector3 direction;
    private float speed;
    private Transform headTransform;
    private SnakeEnemy owner;
    private float age;

    //----------------------------------------------------------------------------------------------------------------

    //Initialization called by SnakeEnemy upon spawning a projectile

    public void Init(Vector3 dir, float spd, Transform head, SnakeEnemy opponentOwner)
    {
        direction = dir;
        speed = spd;
        headTransform = head;
        owner = opponentOwner;
    }

    //----------------------------------------------------------------------------------------------------------------

    void Update()
    {
        age += Time.deltaTime;
        if(age >= lifetime) {Destroy(gameObject); return;}

        if(moveManually)
        {
            //Flat XZ movement; projectiles don't arc
            Vector3 move = direction * speed * Time.deltaTime;
            move.y = 0f;
            transform.position += move;
        }

        CheckHit();
    }

    private void CheckHit()
    {
        if(headTransform == null || owner == null) return;

        //Flatten both to XZ so a jumping head isn't hit from below
        Vector3 projFlat = transform.position; projFlat.y = 0f;
        Vector3 headFlat = headTransform.position; headFlat.y = 0f;

        if(Vector3.Distance(projFlat, headFlat) < hitRadius)
        {
            owner.NotifyProjectileHit(damage);
            Destroy(gameObject);
        }
    }
}
