using UnityEngine;
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
    public float topDownLerpSpeed = 5f;

    [Header("Chase Cam Settings")]
    [Tooltip("Offset from the head in local space (behind and above)")]
    public Vector3 chaseOffset = new Vector3(0f, 4f, -8f);
    [Tooltip("How quickly the chase camera follows position")]
    public float chasePosLerpSpeed = 8f;
    [Tooltip("How quickly the chase camera rotates to match the snake heading")]
    public float chaseRotLerpSpeed = 6f;

    [Header("Transition")]
    [Tooltip("Seconds to blend between camera modes.")]
    public float transitionDuration = 0.5f;

    //PRIVATE VARIABLES
    private CameraMode currentMode = CameraMode.TopDown;
    private CameraMode targetMode = CameraMode.TopDown;
    private float transitionT = 1f; // 0 = start of transition, 1 = done

    // Snapshot of camera state at the START of a transition (for blending).
    private Vector3 transitionFromPos;
    private Quaternion transitionFromRot;

    private Camera cam;

    //----------------------------------------------------------------------------------------------------------------

    void Awake()
    {
        cam = GetComponent<Camera>();
    }

    void LateUpdate()
    {
        if (snakeHead == null) return;

        if (transitionT < 1f)
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

        float posSpeed = (mode == CameraMode.TopDown) ? topDownLerpSpeed : chasePosLerpSpeed;
        float rotSpeed = (mode == CameraMode.TopDown) ? topDownLerpSpeed : chaseRotLerpSpeed;

        transform.position = Vector3.Lerp(transform.position, targetPos, posSpeed * Time.deltaTime);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotSpeed * Time.deltaTime);
    }

    //----------------------------------------------------------------------------------------------------------------
    
    //Target position / rotation per mode

    private Vector3 GetTargetPosition(CameraMode mode)
    {
        if(mode == CameraMode.TopDown)
        {
            //Directly above the snake head, ignoring Y of the head (fixed height)
            return new Vector3(snakeHead.position.x, snakeHead.position.y + topDownHeight, snakeHead.position.z);
        }
        else
        {
            //Chase: offset is in the snake's local space (behind and above)
            //but we only rotate around Y so the camera doesn't tilt with jumps
            Quaternion flatRot = Quaternion.Euler(0f, snakeHead.eulerAngles.y, 0f);
            Vector3 worldOff = flatRot * chaseOffset;
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
            //Look at a point slightly ahead of and above the head so the camera doesn't stare at the ground during jumps.
            Vector3 lookTarget = snakeHead.position + snakeHead.forward * 4f + Vector3.up * 0.5f;
            Vector3 dir = lookTarget - transform.position;
            if(dir.sqrMagnitude < 0.001f) return transform.rotation;
            return Quaternion.LookRotation(dir);
        }
    }

    //----------------------------------------------------------------------------------------------------------------

    //Utility functions

    private static float EaseInOut(float t) => t * t * (3f - 2f * t);
}
