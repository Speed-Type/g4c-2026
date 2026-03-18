using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class RoguelikeCameraController : MonoBehaviour
{
    private PlayerInputActions input; //input action controller

    //Variables in inspector
    [Header("Reference Transforms")]
    [SerializeField] private Transform targetTransform;
    [SerializeField] private Transform savedFreeCamTransform;
    [SerializeField] private Transform savedMapCamPosition;

    [Header("Transition Settings")]
    [SerializeField] private GameObject cloudTransitionGO;
    [SerializeField] private float cloudTransitionLength;
    private Color defaultCloudColor;

    [Header("Free Cam Rotation Parameters")]
    public float yawDragSensitivity = 20f;
    public float pitchDragSensitivity = 1f;
    public float yawSmoothTime = 3f;
    public float pitchSmoothTime = .3f;
    public float idleRotationSpeed = 10f;

    [Header("Free Cam Vertical Clamp")]
    public float maxPitch = 75f;
    public float minPitch = -20f;

    [Header("Zoom Parameters")]
    public float minDistance = 10f;
    public float maxDistance = 30f;
    public float zoomSpeed = 1f;
    public float zoomSmoothTime = .15f;

    [Header("Camera Transitions Timing")]
    public static float freeCamTransitionTiming = 1f;
    public static float generalDelayBetweenTransitions = 0.25f;

    //Internal logic variables
    private int freeCamIdleRotationDirection = 1;
    private float currentPitch;
    private float currentDistance;
    private float targetPitch;
    private float targetDistance;
    private float yawVelocity;
    private float pitchVelocity;
    private float zoomVelocity;
    private bool pressing;
    private bool isFreeCam;

    void Awake()
    {
        //Setup input actions
        input = new PlayerInputActions();

        input.Player.Drag.performed += ctx => pressing = true;
        input.Player.Drag.canceled += ctx => pressing = false;

        input.Player.Look.performed += OnLook;
        input.Player.Zoom.performed += OnZoom;

        currentDistance = (maxDistance + minDistance) / 2f;
        targetDistance = currentDistance;

        //Initialize cloud transition color
        defaultCloudColor = cloudTransitionGO.GetComponent<Image>().color;
    }

    public void StartFreeCam()
    {
        //Smooth transition to freecam view
        LeanTween.move(this.gameObject, savedFreeCamTransform, freeCamTransitionTiming).setEase(LeanTweenType.easeInExpo).setOnComplete(() => {
            //TO FIX: rotationVelocity = freeCamIdleRotationDirection * freeCamIdleRotationDirection;
            input.Enable();
            isFreeCam = true;
        });
        LeanTween.rotate(this.gameObject, savedFreeCamTransform.rotation.eulerAngles, freeCamTransitionTiming).setEase(LeanTweenType.easeInExpo);
    
        //Also do cloud transition
        CloudTransitionIn();
    }

    public void StopFreeCam()
    {
        //Save necessary positions
        savedFreeCamTransform.position = transform.position;
        savedFreeCamTransform.rotation = transform.rotation;
        input.Disable();
        isFreeCam = false;

        //Smooth transition to overhead map view
        LeanTween.move(this.gameObject, savedMapCamPosition, freeCamTransitionTiming).setEase(LeanTweenType.easeOutExpo);
        LeanTween.rotate(this.gameObject, new Vector3(90, transform.localEulerAngles.y, 0), freeCamTransitionTiming).setEase(LeanTweenType.easeOutExpo);
    
        //Also do fog transition
        CloudTransitionOut();
    }

    private void CloudTransitionOut()
    {
        cloudTransitionGO.SetActive(true);
        Image cloudTransitionImage = cloudTransitionGO.GetComponent<Image>();
        LeanTween.value(this.gameObject, (Color color) =>
        {
            cloudTransitionImage.color = color;
        }, new Color(0, 0, 0, 0), defaultCloudColor, freeCamTransitionTiming).setEaseOutQuad().setOnComplete(() =>
        {
            LeanTween.delayedCall(generalDelayBetweenTransitions, () =>
            {
                LeanTween.value(this.gameObject, (Color color) =>
                {
                    cloudTransitionImage.color = color;
                }, defaultCloudColor, new Color(0, 0, 0, 0), cloudTransitionLength).setEaseOutQuad().setOnComplete(() =>
                {
                    cloudTransitionGO.SetActive(false);
                });
            });
        });
    }

    private void CloudTransitionIn()
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
            });
        });
    }

    public void OnLook(InputAction.CallbackContext ctx)
    {
        if(RoguelikeGameplayController.gameState != GameState.Idle) return;
        if(!pressing) return;

        Vector2 delta = ctx.ReadValue<Vector2>();
        yawVelocity = delta.x * yawDragSensitivity;

        if(delta.x > 0)
        {
            freeCamIdleRotationDirection = 1;
        }
        else if(delta.x < 0)
        {
            freeCamIdleRotationDirection = -1;
        }

        // if(newSystem)
        // {
        //     targetPitch -= delta.y * pitchDragSensitivity;
            
        //     if(delta.x > 0)
        //     {
        //         freeCamIdleRotationDirection = 1;
        //     }
        //     else if(delta.x < 0)
        //     {
        //         freeCamIdleRotationDirection = -1;
        //     }
        // }
    }

    public void OnZoom(InputAction.CallbackContext ctx)
    {
        float scroll = ctx.ReadValue<float>();

        targetDistance -= scroll * zoomSpeed;
    }

    void LateUpdate()
    {
        if(isFreeCam)
        {
            //Clamp
            targetPitch = Mathf.Clamp(targetPitch, minPitch, maxPitch);
            targetDistance = Mathf.Clamp(targetDistance, minDistance, maxDistance);

            //Rotation when not dragging
            if (!pressing)
            {
                yawVelocity = Mathf.Lerp(yawVelocity, freeCamIdleRotationDirection * idleRotationSpeed, Time.deltaTime * yawSmoothTime);
            }

            //Rotate around the target
            transform.RotateAround(targetTransform.position, Vector3.up, yawVelocity * Time.deltaTime);
        
            //Always look at the target
            transform.LookAt(targetTransform);
        }

        // //Smoothly adjust rotate
        // currentPitch = Mathf.SmoothDampAngle(currentPitch, targetPitch, ref pitchVelocity, pitchSmoothTime);
    

        // //Smoothly adjust zoom (this should always be smooth)
        // currentDistance = Mathf.SmoothDamp(currentDistance, targetDistance, ref zoomVelocity, zoomSmoothTime);

        // //Apply these
        // Quaternion rotation = Quaternion.Euler(currentPitch, currentYaw, 0);
        // Vector3 offset = rotation * new Vector3(0, 0, -currentDistance);
    }
}