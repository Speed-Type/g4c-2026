using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class RoguelikeCameraController : MonoBehaviour
{
    private PlayerInputActions input; //input action controlelr

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

        //Initialize cloud transition color
        defaultCloudColor = cloudTransitionGO.GetComponent<Image>().color;
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
        rotationVelocity = delta.x * freeCamDragSensitivity;
        
        if(delta.x > 0)
        {
            freeCamIdleRotationDirection = 1;
        }
        else if(delta.x < 0)
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
