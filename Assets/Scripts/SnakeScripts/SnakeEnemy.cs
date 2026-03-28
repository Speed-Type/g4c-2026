using System.Collections.Generic;
using UnityEngine;

public class SnakeEnemy : MonoBehaviour
{
    //Public variables

    [Header("References")]
    public SnakeGameplayController snakeController;
    public Transform snakeHead;

    [Header("Movement (General)")]
    public float moveSpeed    = 3.5f;
    public float turnSpeed    = 90f;

    [Header("Movement (Wander)")]
    public float wanderDistance = 2f;
    public float wanderRadius = 1.2f;
    public float wanderJitter = 40f;

    [Header("Movement (Flee)")]
    [Tooltip("Flee activates when nearest body segment is within this range")]
    public float fleeRadius = 5f;
    public float fleeWeight = 2.5f;
    [Tooltip("If a segment is closer than this while fleeing, attempt a flee jump")]
    public float fleeJumpTriggerDist = 2.5f;
    [Tooltip("Minimum seconds between flee jumps")]
    public float fleeJumpCooldown = 1.5f;

    [Header("Jump (Shared)")]
    public float jumpHeight = 3f;
    public float gravity = 18f;

    [Header("Wall avoidance")]
    public Vector3 arenaCenter = Vector3.zero;
    public Vector3 arenaHalfExtents = new Vector3(20f, 0f, 20f);
    public float wallMargin = 4f;
    public float wallWeight = 3f;

    [Header("Attack (General)")]
    [Tooltip("Seconds the opponent idles in Windup before attacking")]
    public float windupDuration = 0.6f;
    [Tooltip("Seconds in Cooldown after any attack")]
    public float cooldownDuration = 1.2f;
    [Tooltip("Minimum distance to head before the opponent considers attacking")]
    public float attackRange = 8f;
    [Tooltip("Minimum seconds between attack attempts (across all attack types)")]
    public float attackInterval = 3f;

    [Header("Attack (Charge)")]
    public float chargeMoveSpeed = 14f;
    [Tooltip("How long the charge lasts (seconds)")]
    public float chargeDuration = 0.5f;
    [Tooltip("Head must be within this radius during charge for a hit")]
    public float chargeHitRadius = 1.2f;

    [Header("Attack (Projectile)")]
    public GameObject projectilePrefab;
    [Tooltip("Speed of fired projectiles")]
    public float projectileSpeed = 10f;
    [Tooltip("How far ahead of the head to lead the shot (0 = aim at current pos)")]
    public float projectileLeadFactor = 0.4f;

    [Header("Attack (Jump attack)")]
    [Tooltip("How far ahead to predict the head position for the arc target")]
    public float jumpAtkLeadFactor = 0.6f;
    [Tooltip("Head must be within this radius on landing for a hit")]
    public float jumpAtkHitRadius = 1.5f;

    [Header("Collision detection (Body contact)")]
    public float contactRadius = 0.6f;

    [Header("Health")]
    public int maxHealth;
    public int currentHealth;

    //----------------------------------------------------------------------------------------------------------------
    
    //State machine

    private enum State
    {
        Wander,
        Flee,
        Windup,
        Charge,
        Shoot,
        JumpAtk,
        Cooldown,
    }

    private enum AttackType {Charge, Shoot, JumpAtk}

    private State state = State.Wander;
    private AttackType nextAttack = AttackType.Charge;

    //----------------------------------------------------------------------------------------------------------------
    
    //Private variables

    //Movement
    private float groundY;
    private Vector3 heading;
    private float wanderAngle;

    //Jump
    private float verticalVelocity;
    private bool isGrounded = true;
    private float fleeJumpTimer; //counts down; jump allowed when <= 0

    //Attack shared
    private float attackCooldownTimer; //global timer between attack attempts
    private float stateTimer; //generic per-state elapsed timer
    private Vector3 lockedTarget; //position locked at windup start

    //Charge
    private Vector3 chargeDirection;

    //Misc
    [HideInInspector] public bool isActive = true;

    //----------------------------------------------------------------------------------------------------------------
    
    void Start()
    {
        groundY = transform.position.y;
        heading = Random.insideUnitSphere;
        heading.y = 0f;
        heading = heading.sqrMagnitude < 0.01f ? Vector3.forward : heading.normalized;
        wanderAngle = Random.Range(0f, 360f);
    }

    void Update()
    {
        if(!isActive) return;
        if(snakeController.isPlaying) return;

        attackCooldownTimer -= Time.deltaTime;
        fleeJumpTimer -= Time.deltaTime;

        Debug.Log("Snake running");
        UpdateState();
        ApplyGravity();
        ClampToGround();
        CheckBodyContact();
    }

    //----------------------------------------------------------------------------------------------------------------

    //State machine

    //Splits one function call into different ones depending on the current state
    private void UpdateState()
    {
        switch(state)
        {
            case State.Wander: UpdateWander(); break;
            case State.Flee: UpdateFlee(); break;
            case State.Windup: UpdateWindup(); break;
            case State.Charge: UpdateCharge(); break;
            case State.Shoot: UpdateShoot(); break;
            case State.JumpAtk: UpdateJumpAtk(); break;
            case State.Cooldown: UpdateCooldown(); break;
        }
    }

    //Method to transition to state
    private void TransitionTo(State next)
    {
        stateTimer = 0f;
        state = next;
    }

    //----------------------------------------------------------------------------------------------------------------

    //Wander state

    private void UpdateWander()
    {
        //Check flee condition first
        if(NearestSegmentDistance(out _) < fleeRadius)
        {
            TransitionTo(State.Flee);
            return;
        }

        //Check attack condition
        if(attackCooldownTimer <= 0f && DistToHead() < attackRange)
        {
            PickNextAttack();
            LockTarget();
            TransitionTo(State.Windup);
            return;
        }

        Vector3 steer = WanderSteering() + WallSteering();
        MoveWithSteering(steer);
    }


    //----------------------------------------------------------------------------------------------------------------

    //Flee state

    private void UpdateFlee()
    {
        float nearestDist = NearestSegmentDistance(out Vector3 nearestPos);

        // Exit flee when clear.
        if(nearestDist >= fleeRadius)
        {
            TransitionTo(State.Wander);
            return;
        }

        //Flee jump: if very close to a segment and grounded, leap away
        if(nearestDist < fleeJumpTriggerDist && isGrounded && fleeJumpTimer <= 0f)
        {
            verticalVelocity = Mathf.Sqrt(2f * gravity * jumpHeight);
            isGrounded       = false;
            fleeJumpTimer    = fleeJumpCooldown;
        }

        Vector3 fleeDir = (transform.position - nearestPos);
        fleeDir.y = 0f;
        if(fleeDir.sqrMagnitude < 0.001f)
            fleeDir = new Vector3(Random.value - 0.5f, 0f, Random.value - 0.5f);

        float strength = 1f - Mathf.Clamp01(nearestDist / fleeRadius);
        Vector3 steer = fleeDir.normalized * strength * fleeWeight + WallSteering();
        MoveWithSteering(steer);
    }

    //----------------------------------------------------------------------------------------------------------------

    //Windup state (pauses to telegraph attacks)

    private void UpdateWindup()
    {
        stateTimer += Time.deltaTime;

        //Face the locked target during windup so the player can read intent
        Vector3 toTarget = lockedTarget - transform.position;
        toTarget.y = 0f;
        if (toTarget.sqrMagnitude > 0.001f)
            FaceDirection(toTarget.normalized, turnSpeed * 2f);

        if(stateTimer >= windupDuration)
        {
            //Refresh target lock just before the attack fires
            LockTarget();
            TransitionTo(AttackStateFor(nextAttack));
        }
    }

    //----------------------------------------------------------------------------------------------------------------

    //Charge state (the attack)

    private void UpdateCharge()
    {
        stateTimer += Time.deltaTime;

        //Dash in the locked direction
        transform.position += chargeDirection * chargeMoveSpeed * Time.deltaTime;

        //Hit check against head.
        if(DistToHead() < chargeHitRadius)
        {
            OnPlayerHit?.Invoke(AttackType.Charge.ToString());
            EnterCooldown();
            return;
        }

        //End charge after duration.
        if (stateTimer >= chargeDuration)
            EnterCooldown();
    }

    //----------------------------------------------------------------------------------------------------------------

    //Shoot state (the attack)

    //Imediate shoot
    private void UpdateShoot()
    {
        FireProjectile();
        EnterCooldown();
    }


    //----------------------------------------------------------------------------------------------------------------

    //Jump state (the attack)

    private void UpdateJumpAtk()
    {
        stateTimer += Time.deltaTime;

        if(stateTimer <= Time.deltaTime + 0.001f) //first frame
        {
            //Launch a ballistic arc toward the predicted head position
            float travelTime = EstimateJumpArcTime();
            Vector3 predictedHeadPos = PredictHeadPosition(travelTime * jumpAtkLeadFactor);
            predictedHeadPos.y = groundY;

            Vector3 toTarget = predictedHeadPos - transform.position;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude > 0.001f)
                heading = toTarget.normalized;

            verticalVelocity = Mathf.Sqrt(2f * gravity * jumpHeight);
            isGrounded = false;
        }

        //Move horizontally toward target while in the air
        MoveWithSteering(heading);

        //On landing, check if head is within hit radius
        if(isGrounded && stateTimer > 0.1f)
        {
            if(DistToHead() < jumpAtkHitRadius)
                OnPlayerHit?.Invoke(AttackType.JumpAtk.ToString());

            EnterCooldown();
        }
    }

    //----------------------------------------------------------------------------------------------------------------

    //Cooldown state (typically after attacks)
    
    private void UpdateCooldown()
    {
        stateTimer += Time.deltaTime;

        //Keep some gentle movement during cooldown
        Vector3 steer = WanderSteering() + WallSteering();
        MoveWithSteering(steer);

        //Get out of cooldown state when time is up
        if(stateTimer >= cooldownDuration)
        {
            attackCooldownTimer = attackInterval;
            TransitionTo(State.Wander);
        }
    }

    //----------------------------------------------------------------------------------------------------------------

    //Steering helper methods

    //Steering with random wander
    private Vector3 WanderSteering()
    {
        wanderAngle += Random.Range(-wanderJitter, wanderJitter) * Time.deltaTime;
        Vector3 circleCenter = transform.position + heading * wanderDistance;
        float rad = wanderAngle * Mathf.Deg2Rad;
        Vector3 target = circleCenter + new Vector3(Mathf.Cos(rad), 0f, Mathf.Sin(rad)) * wanderRadius;
        return (target - transform.position).normalized;
    }

    //Avoid wall
    private Vector3 WallSteering()
    {
        Vector3 pos = transform.position;
        Vector3 result = Vector3.zero;

        float distPosX = (arenaCenter.x + arenaHalfExtents.x) - pos.x;
        float distNegX = pos.x - (arenaCenter.x - arenaHalfExtents.x);
        float distPosZ = (arenaCenter.z + arenaHalfExtents.z) - pos.z;
        float distNegZ = pos.z - (arenaCenter.z - arenaHalfExtents.z);

        if (distPosX < wallMargin) result.x -= (1f - distPosX / wallMargin);
        if (distNegX < wallMargin) result.x += (1f - distNegX / wallMargin);
        if (distPosZ < wallMargin) result.z -= (1f - distPosZ / wallMargin);
        if (distNegZ < wallMargin) result.z += (1f - distNegZ / wallMargin);

        return result * wallWeight;
    }

    //Moving with a specific direction
    private void MoveWithSteering(Vector3 desiredDir)
    {
        desiredDir.y = 0f;
        if(desiredDir.sqrMagnitude < 0.001f) return;

        desiredDir = desiredDir.normalized;

        float currentYaw = Mathf.Atan2(heading.x, heading.z) * Mathf.Rad2Deg;
        float desiredYaw = Mathf.Atan2(desiredDir.x, desiredDir.z) * Mathf.Rad2Deg;
        float newYaw = Mathf.MoveTowardsAngle(currentYaw, desiredYaw, turnSpeed * Time.deltaTime);
        float rad = newYaw * Mathf.Deg2Rad;

        heading = new Vector3(Mathf.Sin(rad), 0f, Mathf.Cos(rad));

        transform.position += heading * moveSpeed * Time.deltaTime;
        FaceDirection(heading, turnSpeed);
    }

    //Just facing a direction
    private void FaceDirection(Vector3 dir, float speed)
    {
        if(dir.sqrMagnitude < 0.001f) return;
        Quaternion target = Quaternion.LookRotation(dir, Vector3.up);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, target, speed * Time.deltaTime);
    }

    //----------------------------------------------------------------------------------------------------------------

    //Jump gravity

    //Method to apply gravity (just some calc)
    private void ApplyGravity()
    {
        if(isGrounded) return;

        verticalVelocity -= gravity * Time.deltaTime;
        Vector3 pos = transform.position;
        pos.y  += verticalVelocity * Time.deltaTime;

        if(pos.y <= groundY)
        {
            pos.y  = groundY;
            verticalVelocity = 0f;
            isGrounded = true;
        }

        transform.position = pos;
    }

    //Make sure bug doesn't get out of bounds
    private void ClampToGround()
    {
        //Hard clamp inside arena bounds (XZ only)
        Vector3 p = transform.position;
        p.x = Mathf.Clamp(p.x, arenaCenter.x - arenaHalfExtents.x, arenaCenter.x + arenaHalfExtents.x);
        p.z = Mathf.Clamp(p.z, arenaCenter.z - arenaHalfExtents.z, arenaCenter.z + arenaHalfExtents.z);
        transform.position = p;
    }

    //----------------------------------------------------------------------------------------------------------------

    //Attack helper methods

    //Randomly pick from the three attack types
    private void PickNextAttack()
    {
        //TODO
        // You can weight these however you like, or make them conditional
        // (e.g. only JumpAtk if head is elevated, only Shoot beyond a range).
        int roll = Random.Range(0, 3);
        nextAttack = roll == 0 ? AttackType.Charge
                   : roll == 1 ? AttackType.Shoot
                   :             AttackType.JumpAtk;
    }

    //Lock onto a target
    private void LockTarget()
    {
        lockedTarget = snakeHead != null ? snakeHead.position : transform.position + transform.forward;
        chargeDirection = (lockedTarget - transform.position);
        chargeDirection.y = 0f;
        chargeDirection = chargeDirection.sqrMagnitude > 0.001f
            ? chargeDirection.normalized
            : transform.forward;
    }

    //Converts attack type to a State
    private State AttackStateFor(AttackType type) => type switch
    {
        AttackType.Charge  => State.Charge,
        AttackType.Shoot   => State.Shoot,
        AttackType.JumpAtk => State.JumpAtk,
        _                  => State.Cooldown,
    };

    //Call cooldown
    private void EnterCooldown()
    {
        TransitionTo(State.Cooldown);
    }

    //Firing projectiles
    private void FireProjectile()
    {
        if(projectilePrefab == null)
        {
            Debug.LogWarning("[SnakeOpponent] projectilePrefab not assigned — Shoot attack skipped.");
            return;
        }

        Vector3 predicted = PredictHeadPosition(projectileLeadFactor);
        Vector3 fireDir = (predicted - transform.position);
        fireDir.y = 0f;
        if(fireDir.sqrMagnitude < 0.001f) fireDir = transform.forward;
        fireDir = fireDir.normalized;

        GameObject proj = Instantiate(projectilePrefab, transform.position + fireDir * 0.8f, Quaternion.LookRotation(fireDir));

        //Add a SnakeProjectile component that handles movement and hit detection
        SnakeProjectile sp = proj.GetComponent<SnakeProjectile>();
        if(sp == null) sp = proj.AddComponent<SnakeProjectile>(); //If not already on script
        sp.Init(fireDir, projectileSpeed, snakeHead, this);
    }

    //----------------------------------------------------------------------------------------------------------------

    //General utility methods

    //Calculate position of nearest segment
    private float NearestSegmentDistance(out Vector3 nearestPos)
    {
        nearestPos = transform.position;
        var segs = snakeController.segments;
        if(segs == null || segs.Count == 0) return float.MaxValue;

        float closest = float.MaxValue;
        foreach(var seg in segs)
        {
            if(seg == null) continue;
            float d = Vector3.Distance(transform.position, seg.transform.position);
            if(d < closest) {closest = d; nearestPos = seg.transform.position;}
        }
        return closest;
    }

    //Calculates distance to snake head
    private float DistToHead() =>
        snakeHead != null ? Vector3.Distance(transform.position, snakeHead.position) : float.MaxValue;

    //Predicts where the head will be in <leadSeconds> seconds, based on the head's current forward direction and the snake's move speed
    private Vector3 PredictHeadPosition(float leadSeconds)
    {
        if(snakeHead == null) return transform.position;
        Vector3 flatFwd = snakeHead.forward;
        flatFwd.y = 0f;
        return snakeHead.position + flatFwd.normalized * snakeController.moveSpeed * leadSeconds;
    }

    //Estimates time for a ballistic arc of jumpHeight under gravity to land
    //Formula: t = 2 * sqrt(2h/g)
    private float EstimateJumpArcTime() => 2f * Mathf.Sqrt(2f * jumpHeight / gravity);

    //----------------------------------------------------------------------------------------------------------------

    //Collision handling

    private void CheckBodyContact()
    {
        var segs = snakeController.segments;
        if (segs == null) return;

        Vector3 myPos = transform.position;
        foreach (var seg in segs)
        {
            if (seg == null) continue;
            Vector3 segFlat = seg.transform.position;
            segFlat.y = myPos.y;
            if (Vector3.Distance(myPos, segFlat) < contactRadius)
            {
                OnCaught();
                return;
            }
        }
    }

    //IMPORTANT: This is the method that is called when the enemy hits the player
    private void OnCaught()
    {
        //TODO: Add i-frames for enemy after being hit

        currentHealth--;
        if(currentHealth <= 0)
        {
            isActive = false;
            snakeController.EndGame(true);
        }
        Debug.Log("[SnakeOpponent] Caught — player wins.");
    }

    //----------------------------------------------------------------------------------------------------------------

    //Collision handling

    //Fired when an attack hits the snake head
    //Arg is the attack type name ("Charge", "Shoot", "JumpAtk")
    //Subscribe here and apply damage / effects as needed
    public event System.Action<string> OnPlayerHit;
    
    public void NotifyProjectileHit(int damage) => OnPlayerHit?.Invoke(AttackType.Shoot.ToString());

    public void ResetOpponent(Vector3 position)
    {
        transform.position = position;
        groundY = position.y;
        isActive = true;
        verticalVelocity = 0f;
        isGrounded = true;
        attackCooldownTimer = attackInterval;
        fleeJumpTimer = 0f;
        wanderAngle = Random.Range(0f, 360f);
        heading = Random.insideUnitSphere;
        heading.y = 0f;
        heading = heading.normalized;
        currentHealth = maxHealth;
        TransitionTo(State.Wander);
    }

    //----------------------------------------------------------------------------------------------------------------

    //Gizmos

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.4f, 0.1f, 0.25f);
        Gizmos.DrawWireSphere(transform.position, fleeRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, contactRadius);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        Gizmos.color = new Color(1f, 1f, 0f, 0.15f);
        Gizmos.DrawWireCube(arenaCenter, arenaHalfExtents * 2f);

        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(transform.position, heading * 2f);
    }
#endif
}
