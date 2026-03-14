using UnityEngine;
using UnityEngine.InputSystem;

public class RoguelikeCameraController : MonoBehaviour
{
    private PlayerInputActions input; //input action controlelr

    //Variables in inspector
    [Header("Reference Transforms")]
    [SerializeField] private Transform targetTransform;
    [SerializeField] private Transform savedFreeCamTransform;
    [SerializeField] private Transform savedMapCamPosition;

    [Header("Free Cam Rotation Parameters")]
    public float freeCamDragSensitivity = 20f;
    public float freeCamIdleRotationSpeed = 10f;
    public float freeCamRotationSmoothing = 3f;
    private int freeCamIdleRotationDirection = 1;

    private float rotationVelocity;
    private bool pressing;
    private bool isFreeCam;

    //[Header("Zoom Parameters")]

    [Header("Camera Transitions Timing")]
    public static float freeCamTransitionTiming = 1f;
    public static float generalDelayBetweenTransitions = 0.25f;

    void Awake()
    {
        //Setup input actions
        input = new PlayerInputActions();

        input.Player.Drag.performed += ctx => pressing = true;
        input.Player.Drag.canceled += ctx => pressing = false;

        input.Player.Look.performed += OnLook;
    }

    public void StartFreeCam()
    {
        //Smooth transition to freecam view
        LeanTween.move(this.gameObject, savedFreeCamTransform, freeCamTransitionTiming).setEase(LeanTweenType.easeInExpo).setOnComplete(() => {
            rotationVelocity = freeCamIdleRotationDirection * freeCamIdleRotationDirection;
            input.Enable();
            isFreeCam = true;
        });
        LeanTween.rotate(this.gameObject, savedFreeCamTransform.rotation.eulerAngles, freeCamTransitionTiming).setEase(LeanTweenType.easeInExpo);
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
    }

    public void OnLook(InputAction.CallbackContext ctx)
    {
        if (!pressing) return;

        Vector2 delta = ctx.ReadValue<Vector2>();
        rotationVelocity = delta.x * freeCamDragSensitivity;
        
        if(delta.x == 0)
        {
            freeCamIdleRotationDirection = 0;
        }
        else if(delta.x > 0)
        {
            freeCamIdleRotationDirection = 1;
        }
        else
        {
            freeCamIdleRotationDirection = -1;
        }
    }

    void LateUpdate()
    {
        if(isFreeCam)
        {
            //Rotation when not dragging
            if (!pressing)
            {
                rotationVelocity = Mathf.Lerp(rotationVelocity, freeCamIdleRotationDirection * freeCamIdleRotationSpeed, Time.deltaTime * freeCamRotationSmoothing);
            }

            //Rotate around the target
            transform.RotateAround(targetTransform.position, Vector3.up, rotationVelocity * Time.deltaTime);
        
            //Always look at the target
            transform.LookAt(targetTransform);
        }
    }
}
