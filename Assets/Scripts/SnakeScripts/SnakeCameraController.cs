using UnityEngine;
using UnityEngine.InputSystem;
using static SnakeGameplayController;

public class SnakeCameraController : MonoBehaviour
{
    //PUBLIC VARIABLES
    [Header("Target")]
    [Tooltip("The snake head transform to follow")]
    public Transform snakeHead;

    [Header("Top-Down Settings")]
    [Tooltip("Height above the snake head for the overhead camera")]
    public float topDownHeight = 20f;
    [Tooltip("How quickly the camera moves to the overhead position")]
    public float topDownTransitionLerpSpeed = 5f;

    [Header("Chase Cam Settings")]
    [Tooltip("Offset from the head in local space (behind and above)")]
    public Vector3 chaseOffset = new Vector3(0f, 4f, -8f);
    [Tooltip("How quickly the chase camera follows position")]
    public float chasePosLerpSpeed = 8f;
    [Tooltip("How quickly the chase camera rotates to match the snake heading")]
    public float chaseRotLerpSpeed = 6f;

    [Header("Zoom")]
    public float topDownZoomSensitivity = 1.5f;
    public float chaseCamZoomSensitivity = 0.6f;
    [Tooltip("How quickly the camera lerps to the zoomed top down position")]
    public float topDownZoomLerpSpeed = 8f;
    [Tooltip("How quickly the camera lerps to the zoomed chase cam position")]
    public float chaseCamZoomLerpSpeed = 8f;
    [Tooltip("Zoom range for top-down height (min, max)")]
    public Vector2 topDownZoomRange = new Vector2(8f, 40f);
    [Tooltip("Zoom range for chase cam offset scale (min, max)")]
    public Vector2 chaseZoomRange = new Vector2(0.4f, 2.5f);

    [Header("Transition")]
    [Tooltip("Seconds to blend between camera modes.")]
    public float transitionDuration = 0.5f;

    [Header("Input")]
    [Tooltip("Same InputActionAsset assigned on SnakeController.")]
    public InputActionAsset inputActions;

    //PRIVATE VARIABLES
    private CameraMode currentMode = CameraMode.TopDown;
    private CameraMode targetMode = CameraMode.TopDown;
    private float transitionT = 1f; // 0 = start of transition, 1 = done

    //Snapshot of camera state at the START of a transition (for blending)
    private Vector3 transitionFromPos;
    private Quaternion transitionFromRot;

    private Camera cam;
    private InputAction zoomAction;
    [HideInInspector] public Transform cameraTarget;

    //Zoom: one value per mode, each in its own range
    //topDownZoom = current camera height  (within topDownZoomRange)
    //chaseZoom = offset scale multiplier (within chaseZoomRange)
    private float topDownZoom;
    private float chaseZoom;

    //----------------------------------------------------------------------------------------------------------------

    void Awake()
    {
        cam = GetComponent<Camera>();
        topDownZoom = topDownHeight;
        chaseZoom = 1f;

        zoomAction = inputActions.FindActionMap("Snake").FindAction("Zoom");
    }

    private void OnEnable()  => zoomAction?.Enable();
    private void OnDisable() => zoomAction?.Disable();

    void LateUpdate()
    {
        if(snakeHead == null) return;

        HandleZoomInput();

        if(transitionT < 1f)
        {
            //Blend between modes
            transitionT = Mathf.MoveTowards(transitionT, 1f, Time.deltaTime / transitionDuration);
            float t = EaseInOut(transitionT);

            Vector3 toPos = GetTargetPosition(targetMode);
            Quaternion toRot = GetTargetRotation(targetMode);

            transform.position = Vector3.Lerp(transitionFromPos, toPos, t);
            transform.rotation = Quaternion.Slerp(transitionFromRot, toRot, t);

            if(transitionT >= 1f)
                currentMode = targetMode;
        }
        else
        {
            //Steady-state follow for the active mode
            FollowSteady(currentMode);
        }
    }

    //----------------------------------------------------------------------------------------------------------------

    //Method to handle zoom
    private void HandleZoomInput()
    {
        float scroll = zoomAction.ReadValue<Vector2>().y;
        if(Mathf.Approximately(scroll, 0f)) return;

        if(currentMode == CameraMode.TopDown || targetMode == CameraMode.TopDown)
        {
            //Scrolling up (positive) = zoom in = lower height
            topDownZoom = Mathf.Clamp(
                topDownZoom - scroll * topDownZoomSensitivity,
                topDownZoomRange.x,
                topDownZoomRange.y);
        }
        else
        {
            //Chase cam: scrolling up = zoom in = smaller offset multiplier
            chaseZoom = Mathf.Clamp(
                chaseZoom - scroll * (chaseCamZoomSensitivity * 0.1f),
                chaseZoomRange.x,
                chaseZoomRange.y);
        }
    }

    public void VictoryZoom(Transform enemyTransform)
    {
        chaseZoom = chaseZoomRange.x;
        topDownZoom = topDownZoomRange.x;

        cameraTarget = enemyTransform;
    }

    public void ResetCameraTarget()
    {
        chaseZoom = (chaseZoomRange.x + chaseZoomRange.y) / 2f;
        topDownZoom = (topDownZoomRange.x + topDownZoomRange.y) / 2f;
        cameraTarget = snakeHead;
    }

    //----------------------------------------------------------------------------------------------------------------

    //Method to set mode as called in SnakeGameplayController
    public void SetMode(CameraMode mode)
    {
        if(mode == targetMode) return; //Do nothing if already the same

        //Snapshot current camera state for smooth blending.
        transitionFromPos = transform.position;
        transitionFromRot = transform.rotation;
        transitionT = 0f;
        targetMode = mode;
    }

    //----------------------------------------------------------------------------------------------------------------

    //Follow logic, split between camera mode
    private void FollowSteady(CameraMode mode)
    {
        Vector3 targetPos = GetTargetPosition(mode);
        Quaternion targetRot = GetTargetRotation(mode);

        float posSpeed = (mode == CameraMode.TopDown) ? topDownTransitionLerpSpeed : chasePosLerpSpeed;
        float rotSpeed = (mode == CameraMode.TopDown) ? topDownTransitionLerpSpeed : chaseRotLerpSpeed * chaseZoom;

        transform.position = Vector3.Lerp(transform.position, targetPos, posSpeed * Time.deltaTime);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotSpeed * Time.deltaTime);
    }

    //----------------------------------------------------------------------------------------------------------------
    
    //Target position / rotation per mode

    private Vector3 GetTargetPosition(CameraMode mode)
    {
        if(mode == CameraMode.TopDown)
        {
            //topDownZoom drives height; lerped smoothly so scroll feels fluid
            float currentHeight = Mathf.Lerp(
                transform.position.y - cameraTarget.position.y,
                topDownZoom,
                topDownZoomLerpSpeed * Time.deltaTime);

            return new Vector3(
                cameraTarget.position.x,
                cameraTarget.position.y + currentHeight,
                cameraTarget.position.z);
        }
        else
        {
            //chaseZoom scales the entire offset vector, so both distance and height scale together (angle stays the same at any zoom level)
            Quaternion flatRot = Quaternion.Euler(0f, snakeHead.eulerAngles.y, 0f);
            Vector3 scaledOff = chaseOffset * chaseZoom;
            float targetScale = Mathf.Lerp(
                chaseZoom,
                chaseZoom,
                chaseCamZoomLerpSpeed * Time.deltaTime);
            Vector3 worldOff = flatRot * scaledOff;
            return snakeHead.position + worldOff;
        }
    }

    private Quaternion GetTargetRotation(CameraMode mode)
    {
        if(mode == CameraMode.TopDown)
        {
            return Quaternion.Euler(90f, 0f, 0f); // straight down
        }
        else
        {
            //Look at a point slightly ahead of and above the head so the camera doesn't stare at the ground during jumps
            Vector3 lookTarget;
            if(cameraTarget == snakeHead)
            {
                lookTarget = cameraTarget.position + cameraTarget.forward * 4f + Vector3.up * 1f;
            }
            else
            {
                lookTarget = cameraTarget.position;
            }
            
            Vector3 dir = lookTarget - transform.position;
            if(dir.sqrMagnitude < 0.001f) return transform.rotation;
            return Quaternion.LookRotation(dir);
        }
    }

    //----------------------------------------------------------------------------------------------------------------

    //Utility functions

    private static float EaseInOut(float t) => t * t * (3f - 2f * t);
}
