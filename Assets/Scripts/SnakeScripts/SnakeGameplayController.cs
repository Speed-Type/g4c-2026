using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

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
    public float startingMoveSpeed = 6f;
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

    [Header("Cards")]
    public float currentCharge;
    public Card currentCard;
    public Image chargeFillIndicator;
    public CardUI cardUI;
    public TMP_Text currentChargeText;

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
    [SerializeField] private float timerTextWarningSizeMultiplier;

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
    private InputAction useCardAction;
    private InputAction toggleCamAction;

    //Saved segment data
    public readonly List<GameObject> segments = new();
    /*
    Each sample stores the head's world position and the total distance the head has traveled since the game started 
    (Like an odometer reading). Segments look up their position by how far behind the head they should be in distance
    making the whole system speed-agnostic. Speed boosts, slow effects, anything that changes moveSpeed wont't (I hope)
    cause segments to drift out of position
    */
    private struct PosSnapshot
    {
        public Vector3 position;
        public float odometer; //Total XZ distance traveled by the head
    }
    private readonly List<PosSnapshot> posHistory = new();
 
    //Running odometer, incremented in SamplePosition every frame
    private float odometer;

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
    private bool jumpQueued;

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
    public CameraMode camMode = CameraMode.TopDown; //For now, default to top down

    //For integration with rg controller
    public bool isPlaying = false;
    public float currentFightTime;
    [HideInInspector] public int totalDamage;

    //Static instance
    public static SnakeGameplayController instance;

    //----------------------------------------------------------------------------------------------------------------

    void Awake()
    {
        instance = this;

        //Setup input
        var map = inputActions.FindActionMap("Snake");

        moveAction = map.FindAction("Move");
        lookAction = map.FindAction("Look");
        useCardAction = map.FindAction("PlayCard");
        toggleCamAction = map.FindAction("ToggleCam");

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
        timerText.text = TimeSpan.FromSeconds(Math.Max(currentFightTime, 0f)).ToString(@"mm\:ss\.ff");
        if(currentFightTime < 15f)
        {
            timerText.color = Color.red;
            float tempSize = 1 + timerTextWarningSizeMultiplier * ((15f - currentFightTime) / 15f);
            timerText.gameObject.transform.localScale = new Vector3(tempSize, tempSize, 1);
        }
        else
        {
            timerText.color = Color.white;
            timerText.gameObject.transform.localScale = new Vector3(1,1,1);
        }

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
            }

            HandleTurning();
            HandleJump();
            MoveHead();
            UpdateSegmentPositions();
            CheckCollisions();

            currentCharge += Time.deltaTime; //Only increase charge when not stunned
            chargeFillIndicator.fillAmount = Mathf.Clamp01((currentCard.cooldown - currentCharge) / currentCard.cooldown);
            currentChargeText.text = Math.Min(currentCharge, currentCard.cooldown).ToString("F2") + " / " + currentCard.cooldown.ToString("F2");
        }

        AdjustSegmentCount();
    }

    //----------------------------------------------------------------------------------------------------------------

    //Input stuff

    private void EnableInput()
    {
        moveAction.Enable();
        lookAction.Enable();

        useCardAction.performed += PlayCard;
        toggleCamAction.performed += ToggleCameraMode;
    }

    private void DisableInput()
    {
        moveAction.Disable();
        lookAction.Disable();

        useCardAction.performed -= PlayCard;
        toggleCamAction.performed -= ToggleCameraMode;

        ReleaseCursor();
    }

    //----------------------------------------------------------------------------------------------------------------
    
    //Card logic

    public void PlayCard(InputAction.CallbackContext ctx)
    {
        PlayCard();
    }

    public void PlayCard()
    {
        if(currentCharge < currentCard.cooldown) return;
        if(!isPlaying) return;
        if(stunTimer > 0) return;

        //Play card
        currentCard.ActivateCard();
        currentCard = Deck.instance.DrawCard();
        cardUI.UpdateCardDisplay(currentCard);
        currentCharge = 0;
    }

    //----------------------------------------------------------------------------------------------------------------
    
    //Camera stuff

    //Method to toggle camera between modes
    private void ToggleCameraMode(InputAction.CallbackContext ctx)
    {
        if(!isPlaying) return;
        if(Mathf.Approximately(Time.timeScale, 0)) return;

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
            Cursor.visible = false;

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

    public static void ReleaseCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
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
            //W = north (0), D = east (90), S = south (180), A = west (270)
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
        Vector2 look = lookAction.ReadValue<Vector2>() * SettingsManager.instance.MouseSensitivity;
        desiredYaw += look.x * mouseSensitivity;

        //A/D (or stick X) also contribute in chase cam, on top of mouse
        //Uses a separate sensitivity so keyboard feel can be tuned independently
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
        if(jumpQueued && !jumpConsumed && coyoteTimer > 0f)
        {
            //Formula is v = sqrt(2 * g * h); thanks AP physics
            verticalVelocity = Mathf.Sqrt(2f * gravity * jumpHeight);
            jumpConsumed = true;
            coyoteTimer = 0f;
            isGrounded  = false;
            jumpQueued = false;
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
        Vector3 currentFlat = new Vector3(transform.position.x, 0f, transform.position.z);
 
        if(posHistory.Count > 0)
        {
            Vector3 lastFlat = new Vector3(posHistory[0].position.x, 0f, posHistory[0].position.z);
            float moved = Vector3.Distance(currentFlat, lastFlat);
            odometer += moved;
            distSinceSample += moved;
        }
        else
        {
            distSinceSample = positionSampleInterval; //Force first sample to avoid errors with other methods
        }
 
        //Slam as many samples as the distance warrants this frame
        //At low FPS the snake may travel multiple intervals in one step
        //Without this, a single sample leaves a gap in history and segments visibly stretch apart which is annoying
        while(distSinceSample >= positionSampleInterval)
        {
            posHistory.Insert(0, new PosSnapshot
            {
                position = transform.position,
                odometer = odometer,
            });
            distSinceSample -= positionSampleInterval;
        }
 
        TrimHistory();
    }

    //Method to remove any no longer necessary saved position snapshots
    private void TrimHistory()
    {
        //Keep enough history to cover the full snake length in distance
        float keepDistance = (segments.Count + 4) * segmentSpacing;
        float cutoff = odometer - keepDistance;
 
        int last = posHistory.Count - 1;
        while(last > 0 && posHistory[last].odometer < cutoff)
        {
            posHistory.RemoveAt(last);
            last--;
        }
    }

    //----------------------------------------------------------------------------------------------------------------
    
    //Segment following

    //Method that updates every segment's position according to the snapshots in the posHistory
    private void UpdateSegmentPositions()
    {
        //Each segment trails the head by (i+1) * segmentSpacing distance units
        //We look up where the head was when it was that far behind its current odometer reading
        //NOTE: Entirely independent of speed
        for (int i = 0; i < segments.Count; i++)
        {
            float targetOdometer = odometer - (i + 1) * segmentSpacing;
            Vector3 targetPos = SampleAtOdometer(targetOdometer);
 
            segments[i].transform.position = targetPos;

            //Face towrd the segment ahead (or the head)
            Vector3 lookAt = (i == 0) ? transform.position : segments[i - 1].transform.position;
            Vector3 dir = lookAt - targetPos;
            if(dir.sqrMagnitude > 0.001f)
                segments[i].transform.rotation = Quaternion.LookRotation(dir);
        }
    }

    //Returns the interpolated head position at a given odometer reading by finding the two nearest samples and lerping between them.
    private Vector3 SampleAtOdometer(float targetOdo)
    {
        if(posHistory.Count == 0) return transform.position;
 
        //History is newest-first (highest odometer first)
        for (int i = 0; i < posHistory.Count - 1; i++)
        {
            PosSnapshot newer = posHistory[i];
            PosSnapshot older = posHistory[i + 1];
 
            if (older.odometer <= targetOdo && targetOdo <= newer.odometer)
            {
                float span = newer.odometer - older.odometer;
                if(span < 0.0001f) return newer.position;
                float frac = (targetOdo - older.odometer) / span;
                return Vector3.Lerp(older.position, newer.position, frac);
            }
        }
 
        //Target is further back than all history, so return oldest sample.
        return posHistory[posHistory.Count - 1].position;
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
        Vector3 spawnPos = idx < posHistory.Count ? posHistory[idx].position : transform.position;

        GameObject seg = Instantiate(segmentPrefab, spawnPos, Quaternion.identity, transform.parent); //Parented under same transform as snake head
        seg.name = $"Segment_{segments.Count}"; //Set name

        //Set legs on this segment
        seg.GetComponent<CaterpillarFootAnim>().phaseOffset = idx * 0.6f;
        seg.GetComponent<CaterpillarFootAnim>().speed = moveSpeed * 3f;

        //Keep the collider enabled but set it as a trigger so it doesn't interfere with physics
        //Opponent's overlap queries need to find the trigger
        //Self-collision is still handled manually via distance checks
        if(seg.TryGetComponent<Collider>(out var col))
        {
            col.isTrigger = segments.Count > 0; //WE WANT ALL OF THEM TRIGGERS EXCEPT INDEX 0
            if(segments.Count == 0) col.gameObject.layer = gameObject.layer; //Make the first segment the same layer as the head
        }

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
 
        //World obstacle collisions for both head and first segment with near-miss timer
        bool touchingObstacle = false;

        bool headHit = Physics.OverlapSphere(head, collisionRadius, obstacleLayerMask).Length > 0;
        bool seg0Hit = false;
        if(segments.Count > 0 && segments[0] != null)
            seg0Hit = Physics.OverlapSphere(segments[0].transform.position, collisionRadius, obstacleLayerMask).Length > 0;

        if(headHit || seg0Hit)
        {
            touchingObstacle = true;
            obstacleContactTimer += Time.deltaTime;
            if(obstacleContactTimer >= collisionGracePeriod)
            {
                //Debug.Log("Player hit obstacle.");
                OnPlayerDamage(3, shrinkTime, true);
                return;
            }
        }

        if(!touchingObstacle) //Reset near-miss timer
            obstacleContactTimer = 0f;
 
        //Items/pickups (no grace period, food collection is always instant)
        Collider[] food = Physics.OverlapSphere(head, collisionRadius, collectibleLayerMask);
        foreach(var f in food)
            OnItemCollected(f.gameObject);

        //Enemies (no grace period here either)
        //Note that we only want to handle these collisions for when the enemy isn't doing a special attack
        //In the cases of special attacks, the snake enemy should handle it itself
        if(snakeEnemy.state != SnakeEnemy.State.JumpAtk && snakeEnemy.state != SnakeEnemy.State.Charge)
        {
            headHit = Physics.OverlapSphere(head, collisionRadius, enemyLayerMask).Length > 0;
            seg0Hit = false;
            if(segments.Count > 0 && segments[0] != null)
                seg0Hit = Physics.OverlapSphere(segments[0].transform.position, collisionRadius, enemyLayerMask).Length > 0;
            
            if(headHit || seg0Hit)
            {
                //Debug.Log("Player hit enemy in non-special attack.");
                OnPlayerDamage(3, shrinkTime, true);
                return;
            }
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
                    //Debug.Log("Player hit self.");
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

    public void OnPlayerDamage(int damage, float stunApplied, bool removeSegments)
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
        odometer = 0f;
        jumpQueued = false;
        moveSpeed = startingMoveSpeed;
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
        cameraController.ResetCameraTarget();
        ApplyCameraMode(camMode);

        //Make [title card]
        ActivateInvincibility();

        //Draw first card
        Deck.instance.ResetDeck();
        currentCard = Deck.instance.DrawCard();
        cardUI.UpdateCardDisplay(currentCard);
    }

    //----------------------------------------------------------------------------------------------------------------
    
    //Helper method to seed positions
    private void SeedPositionHistory()
    {
        Vector3 back  = -transform.forward;
        int count = (initialSegmentCount + 12) * Mathf.CeilToInt(segmentSpacing / positionSampleInterval);
 
        for(int i = 0; i <= count; i++)
        {
            posHistory.Add(new PosSnapshot
            {
                position = transform.position + back * (i * positionSampleInterval),

                //Seed odometer as if the head has already traveled backward, so segments start at the correct distance offsets immediately
                odometer = odometer - i * positionSampleInterval,
            });
        }
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
            //Zoom in fully
            cameraController.VictoryZoom(snakeEnemy.transform);
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
            Gizmos.DrawWireSphere(posHistory[i].position, 0.04f);
    }
#endif
}
