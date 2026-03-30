using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[RequireComponent(typeof(Collider))]
public class ComputerInteractable : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The Camera to animate (usually Main Camera)")]
    public Camera targetCamera;
    [Tooltip("The world-space Canvas that acts as the monitor screen")]
    public Canvas screenCanvas;
    [Tooltip("The ComputerOS script that drives the desktop UI")]
    public ComputerOS operatingSystem;

    [Header("Zoom Target")]
    [Tooltip("Empty GameObject placed where the camera should sit when using the computer")]
    public Transform zoomTarget;
    [Tooltip("How long the zoom-in / zoom-out tween takes")]
    public float zoomDuration = 0.6f;
    [Tooltip("Ease curve for the zoom (0 to 1)")]
    public AnimationCurve zoomEase = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Screen FX")]
    [Tooltip("Optional Renderer on the monitor mesh whose emission colour we animate")]
    public Renderer monitorRenderer;
    [Tooltip("Emission colour when the screen is on")]
    public Color screenOnEmission = new Color(0.1f, 0.4f, 1f);
    [Tooltip("How fast the emission fades in/out")]
    public float emissionFadeSpeed = 3f;

    [Header("Input")]
    public InputActionAsset inputActions;
    private InputAction escapeAction;

    [Tooltip("Optional on-screen 'Exit' button")]
    public Button exitButton;

    //------------------------------------------------------------------------------------------

    private bool inUse = false;
    private bool isAnimating = false;

    //Saved camera state so we can restore it on exit
    private Vector3 savedCamPos;
    private Quaternion savedCamRot;

    private CVCameraController cameraController;
    private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");

    //------------------------------------------------------------------------------------------

    void Awake()
    {
        //Setup input
        escapeAction = inputActions.FindActionMap("Conservationist").FindAction("Escape");
        
        //Setup camera
        targetCamera = Camera.main;
        cameraController = targetCamera.GetComponent<CVCameraController>();

        //Make sure the screen starts off
        SetScreenActive(false, instant: true);

        if(exitButton != null)
            exitButton.onClick.AddListener(ExitComputer);
    }

    void OnEnable()
    {
        escapeAction.performed += ExitComputerFromAction;
    }

    void OnDisable()
    {
        escapeAction.performed -= ExitComputerFromAction;
    }

    //------------------------------------------------------------------------------------------

    //Zoom in and power on the computer
    //Called by interactable object
    public void EnterComputer()
    {
        if(inUse || isAnimating) return;
        EnterSequence();
        GetComponent<InteractableObject>().OnPointerExit(null);
        GetComponent<InteractableObject>().enabled = false;
    }

    public void ExitComputerFromAction(InputAction.CallbackContext ctx)
    {
        ExitComputer();
    }

    //Power off and zoom back out; Wire to UI exit button
    public void ExitComputer()
    {
        if(!inUse || isAnimating) return;
        ExitSequence();
        GetComponent<InteractableObject>().enabled = true;
    }

    //------------------------------------------------------------------------------------------

    //Enter/exit sequences

    private void EnterSequence()
    {
        isAnimating = true;
        TooltipManager.HideTooltip_Static();
        ObjectiveUI.instance.ToggleVisibility();

        //Disable room panning while we're at the computer
        cameraController.enabled = false;

        //Save camera state
        savedCamPos = targetCamera.transform.position;
        savedCamRot = targetCamera.transform.rotation;

        //Zoom in
        TweenCamera(zoomTarget.position, zoomTarget.rotation, () => //On finish
        {
            //Turn screen on
            SetScreenActive(true, instant: false);
            operatingSystem?.PowerOn();

            inUse = true;
            isAnimating = false;
        });
    }

    private void ExitSequence()
    {
        isAnimating = true;
        ObjectiveUI.instance.ToggleVisibility();

        //Turn screen off
        operatingSystem?.PowerOff();
        SetScreenActive(false, instant: false);

        //Short pause so fade is visible
        LeanTween.delayedCall(0.15f, () =>
        {
            //Zoom out
            TweenCamera(savedCamPos, savedCamRot, () =>
            {
                //Re-enable room panning
                cameraController.enabled = true;

                inUse = false;
                isAnimating = false;
            });
        });
    }

    //------------------------------------------------------------------------------------------

    //Helper methods

    private void TweenCamera(Vector3 targetPosition, Quaternion targetRotation, Action onCompleteAction = null)
    {

        LeanTween.move(targetCamera.gameObject, targetPosition, zoomDuration).setEaseOutQuad();
        LeanTween.rotate(targetCamera.gameObject, targetRotation.eulerAngles, zoomDuration).setEaseOutQuad()
        .setOnComplete(() =>
        {
            //Do on complete action
            if(onCompleteAction != null) onCompleteAction();
        });
    }

    private void SetScreenActive(bool on, bool instant)
    {
        //Toggle canvas interactability
        var group = screenCanvas.GetComponent<CanvasGroup>();

        if(instant)
        {
            group.alpha = on ? 1f : 0f;
            group.interactable = on;
            group.blocksRaycasts = on;
        }
        else
        {
            StopCoroutine(nameof(FadeCanvas));
            StartCoroutine(FadeCanvas(group, on ? 1f : 0f, on));
        }

        //Animate monitor emission
        if(monitorRenderer != null)
        {
            StopCoroutine(nameof(FadeEmission));
            StartCoroutine(FadeEmission(on));
        }
    }

    private IEnumerator FadeCanvas(CanvasGroup group, float targetAlpha, bool interactable)
    {
        while(!Mathf.Approximately(group.alpha, targetAlpha))
        {
            group.alpha = Mathf.MoveTowards(group.alpha, targetAlpha, emissionFadeSpeed * Time.deltaTime);
            yield return null;
        }
        group.alpha = targetAlpha;
        group.interactable = interactable;
        group.blocksRaycasts = interactable;
    }

    private IEnumerator FadeEmission(bool on)
    {
        //Requires "_EmissionColor" property on the material (Standard / URP Lit)
        Material mat = monitorRenderer.materials[3]; //instance copy
        Color target = on ? screenOnEmission : Color.black;
        Color current = mat.GetColor(EmissionColor);

        if(on) mat.EnableKeyword("_EMISSION");

        float t = 0f;
        while(t < 1f)
        {
            t += Time.deltaTime * emissionFadeSpeed;
            mat.SetColor(EmissionColor, Color.Lerp(current, target, t));
            yield return null;
        }

        mat.SetColor(EmissionColor, target);
        if(!on) mat.DisableKeyword("_EMISSION");
    }
}
