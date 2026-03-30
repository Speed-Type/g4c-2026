using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;

public class RoguelikeCameraController : MonoBehaviour
{
    private PlayerInputActions input; //input action controller
    public SnakeCameraController snakeCameraController;

    //Variables in inspector
    [Header("Environment Parents")]
    public GameObject idleEnvironmentParent;
    public GameObject battleEnvironmentParent;

    [Header("Reference Transforms")]
    [SerializeField] private Transform targetTransform;
    [SerializeField] Transform freeCamIdleTransform;
    [SerializeField] private Transform savedFreeCamTransform;
    [SerializeField] private Transform mapCamTransform;
    [SerializeField] private Transform inventoryFocusTransform;
    [SerializeField] private Transform enemyCloseupTransform;
    [SerializeField] private Transform screenshakeUIRoot;

    [Header("Transition Settings")]
    [SerializeField] private GameObject cloudTransitionGO;
    [SerializeField] private float cloudTransitionLength;
    private Color defaultCloudColor;
    [SerializeField] private CanvasGroup battleTransitionCanvasGroup;

    [Header("Free Cam Rotation Parameters")]
    [SerializeField] private float yawDragSensitivity = 20f;
    [SerializeField] private float pitchDragSensitivity = 10f;
    [SerializeField] private float yawSmoothTime = 3f;
    [SerializeField] private float pitchSmoothTime = 6f;
    [SerializeField] private float idleRotationSpeed = 10f;

    [Header("Free Cam Vertical Clamp")]
    [SerializeField] private float maxPitch = 60f;
    [SerializeField] private float minPitch = -20f;

    [Header("Zoom Parameters")]
    [SerializeField] private float minZoom = 10f;
    [SerializeField] private float maxZoom = 30f;
    [SerializeField] private float zoomSpeed = 0.75f;
    [SerializeField] private float zoomSmoothTime = .15f;

    [Header("Inventory Cam Parameters")]
    [SerializeField] private float inventoryCamHorizontalOffset = 5;
    [SerializeField] private float inventoryCamVerticalOffset = 1;
    [SerializeField] private float inventoryCamTransitionAnimTime = 0.5f;

    [Header("Camera Transitions Timing")]
    public static float freeCamTransitionTiming = 1f;
    public static float generalDelayBetweenTransitions = 0.25f;
    public float enemyCloseupTransitionTiming = .5f;
    public float enemyCloseupPauseLength = 3f;

    //Internal logic variables
    private static Canvas canvas;
    private int freeCamIdleRotationDirection = 1;
    private float currentPitch;
    private float currentZoom;
    private float currentHorizontalOffset; //used in inventory cam focus shift
    private float currentVerticalOffset;
    private float targetZoom;
    private float yawVelocity;
    private float pitchVelocity;
    private float zoomVelocity;
    private bool pressing;
    private bool isFreeCam;
    private bool isOverUI;
    private int inventoryCamTransitionTween;
    private int horizontalOffsetTransitionTween;
    private int verticalOffsetTransitionTween;

    private static RoguelikeCameraController instance;

    void Awake()
    {
        //Set static instance
        instance = this;

        //Setup input actions
        input = new PlayerInputActions();

        input.Player.Drag.performed += ctx => pressing = true;
        input.Player.Drag.canceled += ctx => pressing = false;

        input.Player.Look.performed += OnLook;
        input.Player.Zoom.performed += OnZoom;

        //Initialize canvas
        canvas = FindFirstObjectByType<Canvas>();

        //Initialize freecam values
        currentPitch = 0;
        currentZoom = (maxZoom + minZoom) / 2f;
        targetZoom = currentZoom;

        //Initialize cloud transition color
        defaultCloudColor = cloudTransitionGO.GetComponent<Image>().color;

        //Hide battle transition
        battleTransitionCanvasGroup.gameObject.SetActive(false);

        //Hide battle environment
        battleEnvironmentParent.SetActive(false);
    }

    public void TransitionToFreeCam(bool immediate = false)
    {
        if(immediate)
        {
            //Immediate transition to freecam view
            transform.position = savedFreeCamTransform.position;
            transform.rotation = savedFreeCamTransform.rotation;
            input.Enable();
            isFreeCam = true;
        }
        else
        {
            //Smooth transition to freecam view
            LeanTween.move(this.gameObject, savedFreeCamTransform, freeCamTransitionTiming).setEase(LeanTweenType.easeInExpo).setOnComplete(() => {
                input.Enable();
                isFreeCam = true;
            });
            LeanTween.rotate(this.gameObject, savedFreeCamTransform.rotation.eulerAngles, freeCamTransitionTiming).setEase(LeanTweenType.easeInExpo);
        }

        //Set target transform position
        targetTransform.position = freeCamIdleTransform.position;
    }

    public void TransitionToMapCam()
    {
        //Save necessary positions
        savedFreeCamTransform.position = transform.position;
        savedFreeCamTransform.rotation = transform.rotation;
        input.Disable();
        isFreeCam = false;

        //Smooth transition to overhead map view
        LeanTween.move(this.gameObject, mapCamTransform, freeCamTransitionTiming)
            .setEase(LeanTweenType.easeOutExpo);
        LeanTween.rotate(this.gameObject, new Vector3(90, transform.localEulerAngles.y, 0), freeCamTransitionTiming)
            .setEase(LeanTweenType.easeOutExpo);
    
        //Also do fog transition
        CloudTransitionOut();
    }

    public void ShiftFreecamFocusToIdle()
    {
        //Tween target transform position
        LeanTween.cancel(inventoryCamTransitionTween);
        inventoryCamTransitionTween = LeanTween.move(targetTransform.gameObject, freeCamIdleTransform.position, inventoryCamTransitionAnimTime)
            .setEaseInOutSine()
            .id;

        //Reset offset
        LeanTween.cancel(horizontalOffsetTransitionTween);
        LeanTween.cancel(verticalOffsetTransitionTween);
        horizontalOffsetTransitionTween = LeanTween.value(currentHorizontalOffset, 0, inventoryCamTransitionAnimTime)
            .setOnUpdate((float val) =>
            {
                currentHorizontalOffset = val;
            })
            .setEaseInOutSine()
            .id;
        verticalOffsetTransitionTween = LeanTween.value(currentVerticalOffset, 0, inventoryCamTransitionAnimTime)
            .setOnUpdate((float val) =>
            {
                currentVerticalOffset = val;
            })
            .setEaseInOutSine()
            .id;

        //Reset freecam capabilities
        idleRotationSpeed *= 2f;
        maxPitch *= 2f;
    }

    public void ShiftFreecamFocusToCaterpillar()
    {
        //Tween target transform position
        LeanTween.cancel(inventoryCamTransitionTween);
        inventoryCamTransitionTween = LeanTween.move(targetTransform.gameObject, inventoryFocusTransform.position, inventoryCamTransitionAnimTime)
            .setEaseInOutSine()
            .id;

        //Set offset (so that caterpillar remains on right side of screen)
        LeanTween.cancel(horizontalOffsetTransitionTween);
        LeanTween.cancel(verticalOffsetTransitionTween);
        horizontalOffsetTransitionTween = LeanTween.value(currentHorizontalOffset, inventoryCamHorizontalOffset, inventoryCamTransitionAnimTime)
            .setOnUpdate((float val) =>
            {
                currentHorizontalOffset = val;
            })
            .setEaseInOutSine()
            .id;
        verticalOffsetTransitionTween = LeanTween.value(currentVerticalOffset, inventoryCamVerticalOffset, inventoryCamTransitionAnimTime)
            .setOnUpdate((float val) =>
            {
                currentVerticalOffset = val;
            })
            .setEaseInOutSine()
            .id;
        
        //Reduce freecam capabilities
        targetZoom = 17.5f;
        idleRotationSpeed /= 2f;
        maxPitch /= 2f;
    }

    public void SwitchToCombatCam(Action onPauseAction)
    {
        //Save necessary positions
        savedFreeCamTransform.position = transform.position;
        savedFreeCamTransform.rotation = transform.rotation;
        input.Disable();
        isFreeCam = false;

        //Do cool visual effects like fallen knight
        int totalLoops = 7;
        battleTransitionCanvasGroup.gameObject.SetActive(true);
        int battleStartEffectTween = LeanTween.value(0f, 1f, (enemyCloseupTransitionTiming + enemyCloseupPauseLength)  / totalLoops)
            .setLoopPingPong(totalLoops)
            .setOnUpdate((float val) =>
            {
                battleTransitionCanvasGroup.alpha = val;
            })
            .id;

        //Tween to close up view of enemy
        LeanTween.move(this.gameObject, enemyCloseupTransform, enemyCloseupTransitionTiming)
            .setEaseOutExpo();
        LeanTween.rotate(this.gameObject, enemyCloseupTransform.eulerAngles, enemyCloseupTransitionTiming)
            .setEaseOutExpo()
            .setOnComplete(() =>
            {
                //Delay a bit in that close up view
                LeanTween.delayedCall(enemyCloseupPauseLength, () =>
                {
                    //Continuous screenshake (there has to be a better way to do this bro...)
                    LeanTween.delayedCall(freeCamTransitionTiming, () => {})
                    .setOnUpdate((float val) =>
                        {
                            ScreenShake.instance.SetTrauma(0.03f); //Continuous screenshake while moving into combat closeup position too
                        });

                    //Do cloud transition out
                    CloudTransitionOut(() =>
                    {
                        //NOTE: The following code is called in the middle of the cloud transition

                        //Stop battle effect
                        LeanTween.cancel(battleStartEffectTween);
                        battleTransitionCanvasGroup.gameObject.SetActive(false);

                        battleEnvironmentParent.SetActive(true);
                        idleEnvironmentParent.SetActive(false);

                        //Activate snake cam controls
                        snakeCameraController.enabled = true;

                        //Do the inputted action
                        onPauseAction();
                    });
                })
                .setOnUpdate((float val) =>
                    {
                        ScreenShake.instance.SetTrauma(0.03f); //Continuous screenshake
                    });
            })
            .setOnUpdate((float val) =>
                {
                    ScreenShake.instance.SetTrauma(0.03f); //Continuous screenshake while moving into combat closeup position too
                });
    }

    public void CloudTransitionOut(Action middleAction = null, Action onCompleteAction = null)
    {
        cloudTransitionGO.SetActive(true);
        Image cloudTransitionImage = cloudTransitionGO.GetComponent<Image>();
        LeanTween.value(this.gameObject, (Color color) =>
        {
            cloudTransitionImage.color = color;
        }, new Color(0, 0, 0, 0), defaultCloudColor, freeCamTransitionTiming).setEaseOutQuad().setOnComplete(() =>
        {
            //Do middle action
            if(middleAction != null)
            {
                middleAction();
            }

            LeanTween.delayedCall(generalDelayBetweenTransitions, () =>
            {
                LeanTween.value(this.gameObject, (Color color) =>
                {
                    cloudTransitionImage.color = color;
                }, defaultCloudColor, new Color(0, 0, 0, 0), cloudTransitionLength).setEaseOutQuad().setOnComplete(() =>
                {
                    cloudTransitionGO.SetActive(false);

                    if(onCompleteAction != null)
                    {
                        onCompleteAction();
                    }
                });
            });
        });
    }

    public void CloudTransitionIn()
    {
        cloudTransitionGO.SetActive(true);
        Image cloudTransitionImage = cloudTransitionGO.GetComponent<Image>();
        LeanTween.value(this.gameObject, (Color color) =>
        {
            cloudTransitionImage.color = color;
        }, new Color(0, 0, 0, 0), defaultCloudColor, cloudTransitionLength).setOnComplete(() =>
        {
            LeanTween.value(this.gameObject, (Color color) =>
            {
                cloudTransitionImage.color = color;
            }, defaultCloudColor, new Color(0, 0, 0, 0), cloudTransitionLength).setEaseOutQuad().setOnComplete(() =>
            {
                cloudTransitionGO.SetActive(false);
                NodeManager.isMovingNodes = false;
            });
        });
    }

    public void OnLook(InputAction.CallbackContext ctx)
    {
        if(RoguelikeGameplayController.gameState == GameState.Loot || !isFreeCam) return; //look should be available in all cases where cam is in freecam mode (eg idle, inventory) EXCEPT looting
        if(!pressing) return;
        if(isOverUI) return; //only rotate when not hovered over UI

        Vector2 delta = ctx.ReadValue<Vector2>() * SettingsManager.instance.MouseSensitivity;
        yawVelocity = delta.x * yawDragSensitivity;
        pitchVelocity = delta.y * pitchDragSensitivity;

        if(delta.x > 0)
        {
            freeCamIdleRotationDirection = 1;
        }
        else if(delta.x < 0)
        {
            freeCamIdleRotationDirection = -1;
        }
    }

    public void OnZoom(InputAction.CallbackContext ctx)
    {
        if(RoguelikeGameplayController.gameState != GameState.Idle) return; //zoom should ONLY be available in idle freecam mode (i.e. not in inventory)
        
        float scroll = ctx.ReadValue<float>();

        targetZoom -= scroll * zoomSpeed;

        //Clamp it
        targetZoom = Mathf.Clamp(targetZoom, minZoom, maxZoom);
    }

    void LateUpdate()
    {
        if(!isFreeCam) return; //Only move the camera around when it's freecam
        
        isOverUI = EventSystem.current.IsPointerOverGameObject();

        //Rotation when not dragging
        if (!pressing || isOverUI)
        {
            //Smoothly transition yaw speed to idle speed
            yawVelocity = Mathf.Lerp(yawVelocity, freeCamIdleRotationDirection * idleRotationSpeed, Time.deltaTime * yawSmoothTime);
            
            //Smoothly transition pitch speed to 0
            pitchVelocity = Mathf.Lerp(pitchVelocity, 0, Time.deltaTime * pitchSmoothTime);
        }

        //Smoothly transition zoom
        currentZoom = Mathf.SmoothDamp(currentZoom, targetZoom, ref zoomVelocity, zoomSmoothTime);

        //Rotate around the target by yaw
        transform.RotateAround(targetTransform.position, Vector3.up, yawVelocity * Time.deltaTime);

        //---------SCREW THIS MATH---------//
        //Update pitch value
        currentPitch -= pitchVelocity * Time.deltaTime;
        currentPitch = Mathf.Clamp(currentPitch, minPitch, maxPitch); //Clamp

        //Get direction from target to camera, set distance to the current distance
        Vector3 offset = transform.position - targetTransform.position;
        float radius = currentZoom;

        //Calculate yaw direction (horizontal plane)
        Vector3 flatDir = new Vector3(offset.x, 0, offset.z).normalized;

        //Apply pitch vertically
        float pitchRad = currentPitch * Mathf.Deg2Rad;

        //Calculate offset for where camera should be
        Vector3 newOffset = new Vector3(
            flatDir.x * Mathf.Cos(pitchRad),
            Mathf.Sin(pitchRad),
            flatDir.z * Mathf.Cos(pitchRad)
        ) * radius; //Multiplied by the total distance from target

        //Apply the offset
        transform.position = targetTransform.position + newOffset;
        //---------SCREW THIS MATH---------//

        //Always look at the target
        transform.LookAt(targetTransform.position - transform.right * currentHorizontalOffset - transform.up * currentVerticalOffset);
    }

    public static GameObject SpawnParticle(Vector3 positionToSpawnAt, GameObject particleGO)
    {
        return Instantiate(particleGO, positionToSpawnAt, Quaternion.identity, instance.screenshakeUIRoot);
    }
}