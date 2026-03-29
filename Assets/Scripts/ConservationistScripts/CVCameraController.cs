using UnityEngine;
using System.Collections;
using UnityEngine.InputSystem;

public class CVCameraController : MonoBehaviour
{
    [Header("Input")]
    public InputActionAsset inputActions;
    private InputAction rotateLeftAction;
    private InputAction rotateRightAction;

    [Header("Pivot & Rotation")]
    [Tooltip("The point the camera orbits around. Set to the center of the room")]
    public Transform pivot;

    [Tooltip("Degrees per second during the pan animation")]
    public float panSpeed = 120f;

    [Tooltip("Ease-in/out curve for the pan (should go 0 to 1)")]
    public AnimationCurve easeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    //State
    private int currentAngleIndex = 0; //0 = North wall, 1 = East, 2 = South, 3 = West
    private bool isPanning = false;

    //Event broadcast so other systems can react to wall changes
    public static event System.Action<int> OnWallChanged; //Passes new wall index (0-3)

    //------------------------------------------------------------------------------------------
    
    void Awake()
    {
        //Setup input
        var map = inputActions.FindActionMap("Conservationist");

        rotateLeftAction = map.FindAction("Left");
        rotateRightAction = map.FindAction("Right");

        rotateLeftAction.performed += _ => StartPan(-1);
        rotateRightAction.performed += _ => StartPan(1);
    }
    
    void Start()
    {
        //Snap camera to the initial position (looking at wall 0)
        SnapToAngle(currentAngleIndex, instant: true);
    }

    void OnEnable()
    {
        rotateLeftAction.Enable();
        rotateRightAction.Enable();
    }

    void OnDisable()
    {
        rotateLeftAction.Disable();
        rotateRightAction.Disable();
    }

    //------------------------------------------------------------------------------------------

    public void PanRight() => StartPan(+1);
    public void PanLeft() => StartPan(-1);

    public void JumpToWall(int wallIndex)
    {
        StopAllCoroutines();
        isPanning = false;
        currentAngleIndex = ((wallIndex % 4) + 4) % 4;
        SnapToAngle(currentAngleIndex, instant: true);
        OnWallChanged?.Invoke(currentAngleIndex);
    }

    //------------------------------------------------------------------------------------------

    private void StartPan(int direction)
    {
        if(isPanning) return;
        int nextIndex = ((currentAngleIndex + direction) % 4 + 4) % 4;
        StartCoroutine(PanCoroutine(nextIndex, direction));
    }

    private IEnumerator PanCoroutine(int targetIndex, int direction)
    {
        isPanning = true;

        float startY  = NormalizedAngle(transform.eulerAngles.y); // current Y
        float targetY = targetIndex * 90f; // destination Y

        //Handle wraparound so we always rotate the short way
        float delta = targetY - startY;
        if (direction > 0 && delta < 0)  delta += 360f;
        if (direction < 0 && delta > 0)  delta -= 360f;

        float elapsed = 0f;
        float duration = Mathf.Abs(delta) / panSpeed;

        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;

        //Compute final camera position by rotating around pivot
        Quaternion targetRot = Quaternion.Euler(
            transform.eulerAngles.x,
            startY + delta,
            transform.eulerAngles.z
        );
        Vector3 offset = transform.position - pivot.position;
        Vector3 targetPos = pivot.position + Quaternion.Euler(0, delta, 0) * offset;

        while(elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = easeCurve.Evaluate(t);

            transform.position = Vector3.Lerp(startPos, targetPos, eased);
            transform.rotation = Quaternion.Slerp(startRot, targetRot, eased);

            yield return null;
        }

        //Snap exactly at end
        transform.position = targetPos;
        transform.rotation = targetRot;

        currentAngleIndex = targetIndex;
        isPanning = false;

        OnWallChanged?.Invoke(currentAngleIndex);
    }

    private void SnapToAngle(int index, bool instant)
    {
        if(pivot == null) return;

        float angle = index * 90f;
        Quaternion rot = Quaternion.Euler(
            transform.eulerAngles.x,
            angle,
            transform.eulerAngles.z
        );

        //Reposition camera around pivot
        Vector3 localOffset = Quaternion.Inverse(transform.rotation) * (transform.position - pivot.position);
        transform.position = pivot.position + rot * localOffset;
        transform.rotation = rot;
    }

    private float NormalizedAngle(float angle)
    {
        angle %= 360f;
        if(angle < 0) angle += 360f;
        return angle;
    }
}
