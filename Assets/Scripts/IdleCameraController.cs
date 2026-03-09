using UnityEngine;
using UnityEngine.InputSystem;

public class IdleCameraController : MonoBehaviour
{
    //Variables in inspector
    [SerializeField] private Transform targetTransform;
    [SerializeField] private GameObject savedIdlePosition;

    [Header("Rotation Parameters")]
    public float dragSensitivity = 3f;
    public float idleRotationSpeed = 10f;
    private int idleRotationDirection = 1;
    public float smoothing = 5f;

    [Header("Zoom Parameters")]

    //Private variables
    private PlayerInputActions input;

    private float rotationVelocity;
    private bool pressing;
    private bool isFreeCam;

    void Awake()
    {
        input = new PlayerInputActions();

        input.Player.Drag.performed += ctx => pressing = true;
        input.Player.Drag.canceled += ctx => pressing = false;

        input.Player.Look.performed += OnLook;
    }

    public void StartFreeCam()
    {
        rotationVelocity = idleRotationSpeed * idleRotationDirection;
        input.Enable();
        isFreeCam = true;
    }

    public void StopFreeCam()
    {
        savedIdlePosition.transform.position = transform.position;
        input.Disable();
        isFreeCam = false;
    }

    public void OnLook(InputAction.CallbackContext ctx)
    {
        if (!pressing) return;

        Vector2 delta = ctx.ReadValue<Vector2>();
        rotationVelocity = delta.x * dragSensitivity;
        
        if(delta.x == 0)
        {
            idleRotationDirection = 0;
        }
        else if(delta.x > 0)
        {
            idleRotationDirection = 1;
        }
        else
        {
            idleRotationDirection = -1;
        }
    }

    void LateUpdate()
    {
        if(isFreeCam)
        {
            //Rotation when not dragging
            if (!pressing)
            {
                rotationVelocity = Mathf.Lerp(rotationVelocity, idleRotationDirection * idleRotationSpeed, Time.deltaTime * smoothing);
            }

            //Rotate around the target
            transform.RotateAround(targetTransform.position, Vector3.up, rotationVelocity * Time.deltaTime);
        }

        //Always look at the target
        transform.LookAt(targetTransform);
    }
}
