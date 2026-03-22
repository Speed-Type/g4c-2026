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
    public float pitchDragSensitivity = 10f;
    public float yawSmoothTime = 3f;
    public float pitchSmoothTime = 6f;
    public float idleRotationSpeed = 10f;

    [Header("Free Cam Vertical Clamp")]
    public float maxPitch = 60f;
    public float minPitch = -20f;

    [Header("Zoom Parameters")]
    public float minDistance = 10f;
    public float maxDistance = 30f;
    public float zoomSpeed = 0.75f;
    public float zoomSmoothTime = .15f;

    [Header("Camera Transitions Timing")]
    public static float freeCamTransitionTiming = 1f;
    public static float generalDelayBetweenTransitions = 0.25f;

    //Internal logic variables
    private static Canvas canvas;
    private int freeCamIdleRotationDirection = 1;
    private float currentPitch;
    private float currentDistance;
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

        //Initialize canvas
        canvas = FindFirstObjectByType<Canvas>();

        //Initialize freecam values
        currentPitch = 0;
        currentDistance = (maxDistance + minDistance) / 2f;
        targetDistance = currentDistance;

        //Initialize cloud transition color
        defaultCloudColor = cloudTransitionGO.GetComponent<Image>().color;
    }

    public void StartFreeCam()
    {
        //Smooth transition to freecam view
        LeanTween.move(this.gameObject, savedFreeCamTransform, freeCamTransitionTiming).setEase(LeanTweenType.easeInExpo).setOnComplete(() => {
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
                NodeManager.isMovingNodes = false;
            });
        });
    }

    public void OnLook(InputAction.CallbackContext ctx)
    {
        if(RoguelikeGameplayController.gameState != GameState.Idle) return;
        if(!pressing) return;

        Vector2 delta = ctx.ReadValue<Vector2>();
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
        if(RoguelikeGameplayController.gameState != GameState.Idle) return;
        
        float scroll = ctx.ReadValue<float>();

        targetDistance -= scroll * zoomSpeed;

        //Clamp it
        targetDistance = Mathf.Clamp(targetDistance, minDistance, maxDistance);
    }

    void LateUpdate()
    {
        if(isFreeCam)
        {
            //Rotation when not dragging
            if (!pressing)
            {
                //Smoothly transition yaw speed to idle speed
                yawVelocity = Mathf.Lerp(yawVelocity, freeCamIdleRotationDirection * idleRotationSpeed, Time.deltaTime * yawSmoothTime);
                
                //Smoothly transition pitch speed to 0
                pitchVelocity = Mathf.Lerp(pitchVelocity, 0, Time.deltaTime * pitchSmoothTime);
            }

            //Smoothly transition zoom
            currentDistance = Mathf.SmoothDamp(currentDistance, targetDistance, ref zoomVelocity, zoomSmoothTime);

            //Rotate around the target by yaw
            transform.RotateAround(targetTransform.position, Vector3.up, yawVelocity * Time.deltaTime);

            //---------SCREW THIS MATH---------//
            //Update pitch value
            currentPitch -= pitchVelocity * Time.deltaTime;
            currentPitch = Mathf.Clamp(currentPitch, minPitch, maxPitch); //Clamp

            //Get direction from target to camera, set distance to the current distance
            Vector3 offset = transform.position - targetTransform.position;
            float radius = currentDistance;

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
            transform.LookAt(targetTransform);
        }
    }

    public static void SpawnParticle(Vector3 positionToSpawnAt, GameObject particleGO)
    {
        Instantiate(particleGO, positionToSpawnAt, Quaternion.identity, canvas.transform);
    }
}