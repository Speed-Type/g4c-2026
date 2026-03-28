using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class SnakeGameplayController : MonoBehaviour
{
    //PUBLIC VARIABLES

    [Header("Segments")]
    public GameObject segmentPrefab;
    public int initialSegmentCount = 5;
    public float segmentSpacing = 1.0f;
    [Tooltip("World units traveled between each recorded position sample; Should be <= segmentSpacing. Smaller = smoother curves, but more memory")]
    public float positionSampleInterval = 0.4f;

    [Header("Movement")]
    public float moveSpeed = 6f;

    [Header("Chase Cam / Mouse-Aim")]
    [Tooltip("Multiplier applied to raw mouse delta x to get correct yaw change")]
    public float mouseSensitivity = 0.15f;
    [Tooltip("Degrees per sec A/D keys rotate in chase cam mode")]
    public float keyboardTurnSensitivity = 120f;
    [Tooltip("Max degrees per second the head can physically rotate toward the desired heading, regardless of control scheme")]
    public float maxTurnRate = 220f;

    [Header("Jump")]
    public float jumpHeight = 3.0f;
    [Tooltip("Gravity magnitude (must be positive for downward force)")]
    public float gravity = 18f;
    [Tooltip("Seconds after leaving ground the player can still jump (coyote time)")]
    public float coyoteTime = 0.12f;

    [Header("Collision")]
    public LayerMask obstacleLayerMask;
    public LayerMask collectibleLayerMask;
    public LayerMask enemyLayerMask;
    public float collisionRadius = 0.4f;
    [Tooltip("Seconds the player has to escape after first touching an obstacle before dying")]
    public float collisionGracePeriod = 0.2f;
    [Tooltip("Number of tail segments near the head to skip for self-collision, preventing false positives on the neck.")]
    public int selfCollisionIgnoredTailSegments = 3;
    public float maxInvincibility;

    [Header("Effects")]
    public Volume volume;
    public float hitEffectLength;
    private ChromaticAberration chromaticAberration;
    private Vignette vignette;
    public float maxVignetteIntensity;
    private float originalVignetteIntensity;
    public GameObject stunEffect;

    [Header("Enemy")]
    public float enemySpawnRange;
    public Bugs enemyBugEnum;
    public SnakeEnemy snakeEnemy;

    [Header("Input")]
    public InputActionAsset inputActions;

    [Header("Camera")]
    public SnakeCameraController cameraController;

    [Header("UI References")]
    [SerializeField] private TMP_Text enemyNameText;
    [SerializeField] private RectTransform enemyHealthBarFill;
    private float enemyHealthBarMaxWidth;
    [SerializeField] private Color enemyHealthBarFlashColor;
    [SerializeField] private TMP_Text timerText;

    [Header("Misc Variables for Resetting Game")]
    public Transform snakeStartTransform;
    public RectTransform gameOverGraphicRT;
    [Tooltip("Time it takes for the game over graphic to appear")]
    public float gameOverGraphicAppearT;
    private float gameOverGraphicWidth;
    [Tooltip("Delay before calling the EndGame function and switching back to roguelike gameplay")]
    public float endGameDelay;

    //----------------------------------------------------------------------------------------------------------------

    //PRIVATE VARIABLES

    //Input action references
    private InputAction moveAction;
    private InputAction lookAction;
    private InputAction jumpAction;
    private InputAction growAction;
    private InputAction shrinkAction;
    private InputAction toggleCamAction;

    //Saved segment data
    public readonly List<GameObject> segments = new();
    private readonly List<Vector3> posHistory = new();
    private int targetSegmentCount;
    private float distSinceSample;

    //Heading/rotation
    private float currentYaw; //The world-space Y rotation (degrees) the head actually has
    private float desiredYaw; //target yaw accumulated from mouse input (chase cam only)
    
    //Jumps
    private float verticalVelocity;
    private bool isGrounded;
    private float groundY; // Y of the ground plane (set at Start)
    private float coyoteTimer;
    private bool jumpConsumed;

    //Collision grace period: tracks how long the head has been in contact
    //Resets to 0 whenever contact clears
    //Damage fires only when it reaches collisionGracePeriod so a brief graze doesn't kill the player
    private float obstacleContactTimer = 0f;
    private float selfContactTimer = 0f;

    //Invincibility logic
    private float invincibilityTimer;
    private InvincibilityFlash invincibilityFlash;

    //Status
    [HideInInspector] public float stunTimer;
    private float hitEffectTimer;
    
    //Camera mode
    public enum CameraMode {TopDown, Chase}
    private CameraMode camMode = CameraMode.TopDown; //For now, default to top down

    //For integration with rg controller
    public bool isPlaying = false;
    public float currentFightTime;
    [HideInInspector] public int totalDamage;

    //----------------------------------------------------------------------------------------------------------------

    void Awake()
    {
        //Setup input
        var map = inputActions.FindActionMap("Snake");

        moveAction = map.FindAction("Move");
        lookAction = map.FindAction("Look");
        jumpAction = map.FindAction("Jump");
        growAction = map.FindAction("Grow");
        shrinkAction = map.FindAction("Shrink");
        toggleCamAction = map.FindAction("ToggleCam");

        growAction.performed   += _ => AddSegments(1);
        shrinkAction.performed += _ => BeginShrink(1);

        toggleCamAction.performed += _ => ToggleCameraMode();

        //Initialize health bar variables
        enemyHealthBarMaxWidth = enemyHealthBarFill.sizeDelta.x;
        gameOverGraphicWidth = gameOverGraphicRT.sizeDelta.x;
        
        //Initialize post-processing references
        if(volume.profile.TryGet(out chromaticAberration))
        {
            chromaticAberration.intensity.value = 0f;
        }

        if(volume.profile.TryGet(out vignette))
        {
            originalVignetteIntensity = vignette.intensity.value;
        }
        

        //Initialize other references
        invincibilityFlash = GetComponent<InvincibilityFlash>();
    }

    void Update()
    {
        hitEffectTimer -= Time.deltaTime;

        if(hitEffectTimer > 0)
        {
            chromaticAberration.intensity.value = hitEffectTimer / hitEffectLength;
            vignette.intensity.value = originalVignetteIntensity + (hitEffectTimer / hitEffectLength) * (maxVignetteIntensity - originalVignetteIntensity);
        }

        if(!isPlaying) return;

        //Update timers
        currentFightTime -= Time.deltaTime;
        invincibilityTimer -= Time.deltaTime;
        stunTimer -= Time.deltaTime;
        
        //Update fight timer display
        timerText.text = TimeSpan.FromSeconds(Math.Max(currentFightTime, 0f)).ToString(@"mm\:ss\.fff");
        timerText.color = (currentFightTime < 15f) ? Color.red : Color.white;
        if(currentFightTime < 0)
        {
            //End fight
            EndGame(false);
            return;
        }

        if(stunTimer <= 0)
        {
            if(stunEffect.activeSelf)
            {
                SetLegMovement(true); //Reactivate legs
                stunEffect.SetActive(false);
                Debug.Log("stun hid");
            }

            HandleTurning();
            HandleJump();
            MoveHead();
            UpdateSegmentPositions();
            CheckCollisions();
        }

        AdjustSegmentCount();
    }

    //----------------------------------------------------------------------------------------------------------------

    //Input stuff

    private void EnableInput()
    {
        moveAction.Enable();
        lookAction.Enable();
        jumpAction.Enable();
        growAction.Enable();
        shrinkAction.Enable();
        toggleCamAction.Enable();
    }

    private void DisableInput()
    {
        moveAction.Disable();
        lookAction.Disable();
        jumpAction.Disable();
        growAction.Disable();
        shrinkAction.Disable();
        toggleCamAction.Disable();
        ReleaseCursor();
    }

    //----------------------------------------------------------------------------------------------------------------
    
    //Camera stuff

    //Method to toggle camera between modes
    private void ToggleCameraMode()
    {
        SetCameraMode(camMode == CameraMode.TopDown ? CameraMode.Chase : CameraMode.TopDown);
    }

    //Does the actual mode switch
    private void SetCameraMode(CameraMode mode)
    {
        camMode = mode;
        ApplyCameraMode(mode);
    }

    //Applies changes to camera
    private void ApplyCameraMode(CameraMode mode)
    {
        if(mode == CameraMode.Chase)
        {
            //For chase cam, lock cursor
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible   = false;

            //Sync desired yaw to current so there's no weird jitter
            desiredYaw = currentYaw;
        }
        else
        {
            //Unlock cursor for top-down cam
            ReleaseCursor();
        }

        cameraController.SetMode(mode);
    }

    private static void ReleaseCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;
    }

    //----------------------------------------------------------------------------------------------------------------
    
    //Turn handling

    //Splits turning functions depending on cam mode
    private void HandleTurning()
    {
        if(camMode == CameraMode.TopDown)
            TurnTopDown();
        else
            TurnMouseAim();
    }

    //Top-down turning
    //WASD vector sets the desired world-space heading.
    //The snake then turns toward it at maxTurnRate (same as mouse aim, just different input source)
    //No input means hold the last desired heading
    private void TurnTopDown()
    {
        Vector2 input = moveAction.ReadValue<Vector2>();

        if(input.sqrMagnitude >= 0.01f) //Check that there is any input at all
        {
            //W = north (0), D = east (90), S = south (180), A = west (270).
            desiredYaw = Mathf.Atan2(input.x, input.y) * Mathf.Rad2Deg;
        }

        //Regardless of whether or not there is input, turn towards the desired heading

        ApplyTurnTowardDesired();
    }

    //Chase cam turning
    //Mouse delta x accumulates onto desiredYaw (think fps games)
    //CurrentYaw turns toward desiredYaw at maxTurnRate (same as top down, just different input source)
    private void TurnMouseAim()
    {
        Vector2 look = lookAction.ReadValue<Vector2>();
        desiredYaw += look.x * mouseSensitivity;

        //A/D (or stick X) also contribute in chase cam, on top of mouse.
        //Uses a separate sensitivity so keyboard feel can be tuned independently.
        float keyboardX = moveAction.ReadValue<Vector2>().x;
        desiredYaw += keyboardX * keyboardTurnSensitivity * Time.deltaTime;

        ApplyTurnTowardDesired();
    }

    //Rotates currentYaw toward desiredYaw at maxTurnRate
    //Used by both modes
    private void ApplyTurnTowardDesired()
    {
        float diff = Mathf.DeltaAngle(currentYaw, desiredYaw);
        float maxDelta = maxTurnRate * Time.deltaTime;
        currentYaw += Mathf.Clamp(diff, -maxDelta, maxDelta);

    }

    //----------------------------------------------------------------------------------------------------------------
    
    //Jump handling (Note: does not use rigidbody, just physics calculations)

    //Method to handle jump every frame
    private void HandleJump()
    {
        //Save and update whether we're grounded or not
        bool wasGrounded = isGrounded;
        isGrounded = transform.position.y <= groundY + 0.01f;

        //Reset jump on landing
        if(isGrounded && !wasGrounded)
        {
            verticalVelocity = 0f;
            jumpConsumed = false;
            Vector3 p = transform.position;
            p.y = groundY;
            transform.position = p;
        }

        //Coyote time countdown
        if(isGrounded)
            coyoteTimer = coyoteTime;
        else
            coyoteTimer -= Time.deltaTime;

        //Actual jumping
        if(jumpAction.IsPressed() && !jumpConsumed && coyoteTimer > 0f)
        {
            //Formula is v = sqrt(2 * g * h); thanks AP physics
            verticalVelocity = Mathf.Sqrt(2f * gravity * jumpHeight);
            jumpConsumed = true;
            coyoteTimer = 0f;
            isGrounded  = false;
        }

        //Apply gravity when in the air
        if (!isGrounded) verticalVelocity -= gravity * Time.deltaTime;

        //Apply velocity (just integrating it basically); thanks AP calc
        Vector3 pos = transform.position;
        pos.y += verticalVelocity * Time.deltaTime;
        if(pos.y < groundY)
        {
            pos.y = groundY;
            verticalVelocity = 0f;
        }
        transform.position = pos; //Apply calculated position
    }

    //----------------------------------------------------------------------------------------------------------------
    
    //Movement of snake head

    private void MoveHead()
    {
        //Apply heading rotation (y only cause vertical is handled by HandleJump)
        transform.rotation = Quaternion.Euler(0f, currentYaw, 0f);

        //Translate on the xz plane at constant speed (again, y handled separately)
        Vector3 flatFwd = new Vector3(transform.forward.x, 0f, transform.forward.z).normalized;
        Vector3 pos = transform.position;
        pos.x += flatFwd.x * moveSpeed * Time.deltaTime;
        pos.z += flatFwd.z * moveSpeed * Time.deltaTime;
        transform.position = pos;

        SamplePosition(); //Store position
    }

    //Method to save snapshot of head position; necessary to handle tail movement
    private void SamplePosition()
    {
        //Use horizontal distance only, so vertical movement doesn't trigger extra samples and stretch the segments oddly mid-jump
        if(posHistory.Count > 0)
        {
            Vector3 last = posHistory[0];
            float hDist = Vector3.Distance(
                new Vector3(transform.position.x, 0, transform.position.z),
                new Vector3(last.x, 0, last.z));
            distSinceSample += hDist;
        }
        else
        {
            distSinceSample = positionSampleInterval; //Force first sample to avoid errors with other methods
        }

        if(distSinceSample >= positionSampleInterval)
        {
            posHistory.Insert(0, transform.position); //Full 3D, captures jump arc
            distSinceSample = 0f;
            TrimHistory();
        }
    }

    //Method to remove any no longer necessary saved position snapshots
    private void TrimHistory()
    {
        int maxSamples = (segments.Count + 12) * Mathf.CeilToInt(segmentSpacing / positionSampleInterval);
        if(posHistory.Count > maxSamples)
            posHistory.RemoveRange(maxSamples, posHistory.Count - maxSamples);
    }

    //----------------------------------------------------------------------------------------------------------------
    
    //Segment following

    //Method that updates every segment's position according to the snapshots in the posHistory
    private void UpdateSegmentPositions()
    {
        int samplesPerSeg = Mathf.Max(1, Mathf.RoundToInt(segmentSpacing / positionSampleInterval));

        for(int i = 0; i < segments.Count; i++)
        {
            int histIdx = (i+1) * samplesPerSeg;
            if(histIdx >= posHistory.Count) break;

            Vector3 targetPos = posHistory[histIdx];
            segments[i].transform.position = targetPos;

            //Face toward the segment ahead (or the head for segment 0)
            Vector3 lookAt = (i == 0) ? transform.position : segments[i - 1].transform.position;
            Vector3 dir = lookAt - targetPos;
            if(dir.sqrMagnitude > 0.001f)
                segments[i].transform.rotation = Quaternion.LookRotation(dir);
        }
    }

    //----------------------------------------------------------------------------------------------------------------
    
    //Length management (Adding, removing, etc.)

    //Method to immediately queue more segments to grow
    public void AddSegments(int count) => targetSegmentCount += count;

    //Method to instantly remove segments (no animation)
    public void RemoveSegments(int count) =>
        targetSegmentCount = Mathf.Max(1, targetSegmentCount - count);

    //Method for animated removal
    //Pops one segment at a time
    public void BeginShrink(int count) => StartCoroutine(ShrinkCoroutine(count));

    //Coroutine for shrinking a segment
    private bool isShrinking = false;
    private float shrinkTime = 2f;
    private IEnumerator ShrinkCoroutine(int count)
    {
        isShrinking = true;
        for(int i = 0; i < count; i++)
        {
            targetSegmentCount = Mathf.Max(1, targetSegmentCount - 1);
            //Small visual pause between each popped segment
            yield return new WaitForSeconds(shrinkTime / count);
        }

        isShrinking = false;
    }

    //Method to set length by setting the target segments
    public void SetLength(int count) => targetSegmentCount = Mathf.Max(1, count);

    //Method to adjust count of segments by calling spawn/despawn
    private void AdjustSegmentCount()
    {
        while(segments.Count < targetSegmentCount) SpawnSegment();
        while(segments.Count > targetSegmentCount) DespawnLastSegment();
    }

    //Method to spawn a segment
    private void SpawnSegment()
    {
        int sps = Mathf.Max(1, Mathf.RoundToInt(segmentSpacing / positionSampleInterval));
        int idx = segments.Count * sps;
        Vector3 spawnPos = idx < posHistory.Count ? posHistory[idx] : transform.position;

        GameObject seg = Instantiate(segmentPrefab, spawnPos, Quaternion.identity, transform.parent); //Parented under same transform as snake head
        seg.name = $"Segment_{segments.Count}"; //Set name

        //Set legs on this segment
        seg.GetComponent<CaterpillarFootAnim>().phaseOffset = idx * 0.6f;
        seg.GetComponent<CaterpillarFootAnim>().speed = moveSpeed * 3f;

        //Keep the collider enabled but set it as a trigger so it doesn't interfere with physics
        //Opponent's overlap queries need to find the trigger
        //Self-collision is still handled manually via distance checks
        if(seg.TryGetComponent<Collider>(out var col))
            col.isTrigger = true;

        segments.Add(seg);
    }

    //Method to despawn the last segment
    private void DespawnLastSegment()
    {
        if(segments.Count == 0) return; //Don't run if there's nothing to remove
        int last = segments.Count - 1;
        Destroy(segments[last]);
        segments.RemoveAt(last);
    }

    //----------------------------------------------------------------------------------------------------------------
    
    //Collision handling

    private void CheckCollisions()
    {
        Vector3 head = transform.position;
 
        //World obstacle collisions with near-miss timer
        bool touchingObstacle = false;
        Collider[] hits = Physics.OverlapSphere(head, collisionRadius, obstacleLayerMask);
        if(hits.Length > 0)
        {
            touchingObstacle = true;
            obstacleContactTimer += Time.deltaTime;
            if(obstacleContactTimer >= collisionGracePeriod)
            {
                OnPlayerDamage(3, shrinkTime, true);
                return;
            }
        }
        if(!touchingObstacle)
            obstacleContactTimer = 0f;
 
        //Items/pickups (no grace period, food collection is always instant)
        Collider[] food = Physics.OverlapSphere(head, collisionRadius, collectibleLayerMask);
        foreach(var f in food)
            OnItemCollected(f.gameObject);

        //Enemies (no grace period here either)
        hits = Physics.OverlapSphere(head, collisionRadius, enemyLayerMask);
        if(hits.Length > 0)
        {
            Debug.Log("player has collided with enemy");
            OnPlayerDamage(3, shrinkTime, true);
        }
 
        //Self-collision
        bool touchingSelf = false;
        for(int i = selfCollisionIgnoredTailSegments; i < segments.Count; i++)
        {
            if(Vector3.Distance(head, segments[i].transform.position) < collisionRadius * 2f)
            {
                touchingSelf = true;
                selfContactTimer += Time.deltaTime;
                if(selfContactTimer >= collisionGracePeriod)
                {
                    OnPlayerDamage(3, shrinkTime, true);
                    return;
                }
                break;
            }
        }
        if(!touchingSelf)
            selfContactTimer = 0f;
    }

    private void OnItemCollected(GameObject item)
    {
        AddSegments(1);
        Destroy(item);

        //TODO: ADD STUFF FOR ITEM COLLECT
    }

    private void OnPlayerDamage(int damage, float stunApplied, bool removeSegments)
    {
        if(invincibilityTimer > 0) return; //If i frames are active, no damage

        //Add i frames
        ActivateInvincibility();

        //Apply stun
        if(stunApplied > 0)
        {
            stunTimer = Math.Max(0, stunTimer); //Reset to 0 if negative
            stunTimer += stunApplied;

            SetLegMovement(false); //Stop legs
            stunEffect.SetActive(true); //Display stun effect
            Debug.Log("stun shown");
        }

        //Remove all of snake segments aside from initial ones
        if(removeSegments && !isShrinking)
        {
            BeginShrink(targetSegmentCount - initialSegmentCount);
        }

        //Screenshake
        ScreenShake.instance.AddTrauma(0.25f);

        //Hit effects
        hitEffectTimer = hitEffectLength;

        //Add to total hits
        totalDamage += damage;
    }

    private void ActivateInvincibility()
    {
        invincibilityTimer = maxInvincibility;
        invincibilityFlash.StartInvincibilityFlash(maxInvincibility, 0.25f, 0.05f);
    }

    //----------------------------------------------------------------------------------------------------------------
    
    //Reset method

    public void ResetSnake(Vector3 position, Quaternion rotation)
    {
        //Reset necessary variables
        isPlaying = true;
        verticalVelocity = 0f;
        isGrounded = true;
        jumpConsumed = false;
        stunTimer = 0;
        isShrinking = false;
        totalDamage = 0;
        gameOverGraphicRT.sizeDelta = new Vector2(0, gameOverGraphicRT.sizeDelta.y);
        ResetHitEffects();

        //Clear segments
        foreach(var s in segments) Destroy(s);
        segments.Clear();
        posHistory.Clear();
        distSinceSample = 0f;

        //Reposition snake
        transform.SetPositionAndRotation(position, rotation);
        groundY = position.y;
        currentYaw = rotation.eulerAngles.y;
        desiredYaw = currentYaw;

        //Spawn bug
        GameObject enemyGOToInstantiate = LootManager.instance.BugEnumToSnakeEnemy(enemyBugEnum);
        snakeEnemy = Instantiate(enemyGOToInstantiate, transform.parent).GetComponent<SnakeEnemy>();
        snakeEnemy.snakeController = this;
        snakeEnemy.snakeHead = transform;
        float randomAngle = UnityEngine.Random.Range(0, (float)Math.PI); //Set at random position at specified radius
        snakeEnemy.ResetOpponent(new Vector3((float)Math.Cos(randomAngle) * enemySpawnRange, snakeStartTransform.position.y, (float)Math.Sin(randomAngle) * enemySpawnRange));

        //Pull data from bug
        currentFightTime = snakeEnemy.battleLength;
        enemyNameText.text = snakeEnemy.enemyName;

        //Update health bar
        UpdateEnemyHealthBar(1);

        //Respawn initial segments
        targetSegmentCount = initialSegmentCount;
        SeedPositionHistory();
        for(int i = 0; i < initialSegmentCount; i++) SpawnSegment();

        //Setup camera
        ApplyCameraMode(camMode);

        //Make [title card]
        ActivateInvincibility();
    }

    //----------------------------------------------------------------------------------------------------------------
    
    //Helper method to seed positions
    private void SeedPositionHistory()
    {
        Vector3 back  = -transform.forward;
        int count = (initialSegmentCount + 12) * Mathf.CeilToInt(segmentSpacing / positionSampleInterval);
        for(int i = 0; i <= count; i++)
            posHistory.Add(transform.position + back * (i * positionSampleInterval));
    }

    //----------------------------------------------------------------------------------------------------------------
    
    //Misc

    public void UpdateEnemyHealthBar(float desiredPercentage)
    {
        StatDisplay.UpdateFillBar(enemyHealthBarFill, desiredPercentage, enemyHealthBarMaxWidth, enemyHealthBarFlashColor, Color.white);
    }

    //Method to set all legs' scripts active or inactive
    private void SetLegMovement(bool enable)
    {
        foreach(GameObject segment in segments)
        {
            segment.GetComponent<CaterpillarFootAnim>().enabled = enable;
        }
    }

    private void ResetHitEffects()
    {
        chromaticAberration.intensity.value = 0f;
        vignette.intensity.value = originalVignetteIntensity;
    }

    //----------------------------------------------------------------------------------------------------------------
    
    //Integration with rg gameplay

    public void PlayGame()
    {
        ResetSnake(snakeStartTransform.position, snakeStartTransform.rotation);

        //Open input again
        EnableInput();
    }

    public void EndGame(bool playerWon)
    {
        isPlaying = false;

        //End inputs
        DisableInput();
        ReleaseCursor();

        //Stop feet movement
        SetLegMovement(false);

        if(playerWon) //Case 1: Player wins
        {
            //TODO: MAKE COOL GRAPHIC ON WIN
        }
        else //Case 2: Player loses
        {
            //Display game over graphic
            LeanTween.value(0, gameOverGraphicWidth, gameOverGraphicAppearT).setEaseOutElastic()
            .setOnUpdate((float val) =>
            {
                gameOverGraphicRT.sizeDelta = new Vector2(val, gameOverGraphicRT.sizeDelta.y);
            });
        }

        //End game after a delay
        LeanTween.delayedCall(endGameDelay, () =>
        {
            RoguelikeGameplayController.instance.EndCombat(playerWon);
            ResetHitEffects(); //Reset any residual changes
        });
    }

    //----------------------------------------------------------------------------------------------------------------
    
    //Nice editor gizmo to help visualize everything

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, collisionRadius);

        if(posHistory == null) return;
        Gizmos.color = new Color(0f, 1f, 1f, 0.5f);
        for(int i = 0; i < Mathf.Min(posHistory.Count, 150); i++)
            Gizmos.DrawWireSphere(posHistory[i], 0.04f);
    }
#endif
}
