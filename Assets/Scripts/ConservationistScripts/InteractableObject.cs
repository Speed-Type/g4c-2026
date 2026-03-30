using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Collider))]
public class InteractableObject : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Identity")]
    [Tooltip("Unique ID used by the ObjectiveManager to track completion")]
    public string objectID = "object_01";

    [Tooltip("Friendly name shown in UI tooltips")]
    public string displayName = "Object";

    [Tooltip("Short description shown in tooltip on hover")]
    [TextArea(2, 4)]
    public string description = "Click to interact.";

    [Header("Interaction Settings")]
    [Tooltip("Can this object be interacted with more than once?")]
    public bool repeatable = false;

    [Tooltip("Only interactable when the player is viewing this wall index (-1 = any wall)")]
    public int requiredWallIndex = -1;

    [Tooltip("Scale pulse amount on hover (0 = no pulse)")]
    [Range(0f, 0.2f)]
    public float hoverScalePulse = 0.05f;

    [Header("Events")]
    [Tooltip("Fired the first time (or every time if repeatable) the object is clicked")]
    public UnityEvent onInteract;

    [Tooltip("Fired when the mouse enters the object's collider")]
    public UnityEvent onHoverEnter;

    [Tooltip("Fired when the mouse leaves the object's collider")]
    public UnityEvent onHoverExit;

    //State
    public bool canBeInteracted = true;
    private bool hasBeenInteracted = false;
    private Outline highlight;
    private Vector3 originalScale;
    private int currentWall = 0;

    //------------------------------------------------------------------------------------------

    private void Awake()
    {
        originalScale = transform.localScale;
        highlight = GetComponent<Outline>();
    }

    private void OnEnable()
    {
        CVCameraController.OnWallChanged += HandleWallChanged;
    }

    private void OnDisable()
    {
        CVCameraController.OnWallChanged -= HandleWallChanged;
    }

    //------------------------------------------------------------------------------------------

    public void OnPointerEnter(PointerEventData eventData)
    {
        if(!IsInteractable()) return;

        ApplyHighlight(true);
        onHoverEnter?.Invoke();
        TooltipManager.ShowTooltip_Static(displayName, "", "", "", description);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        ApplyHighlight(false);
        onHoverExit?.Invoke();
        TooltipManager.HideTooltip_Static();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if(!IsInteractable()) return;

        hasBeenInteracted = true;
        onInteract?.Invoke();

        //Notify the objective system
        ObjectiveManager.instance?.NotifyInteraction(objectID);

        if(!repeatable)
            ApplyHighlight(false);
    }

    //------------------------------------------------------------------------------------------

    //Methods to trigger interactions directly from script

    public void TriggerInteraction()
    {
        if(!IsInteractable()) return;
        OnPointerClick(null);
    }

    public void ResetInteraction()
    {
        hasBeenInteracted = false;
        ApplyHighlight(false);
    }

    //------------------------------------------------------------------------------------------

    private bool IsInteractable()
    {
        if(!canBeInteracted) return false;
        if(!repeatable && hasBeenInteracted) return false;
        if(requiredWallIndex >= 0 && currentWall != requiredWallIndex) return false;
        return true;
    }

    private void HandleWallChanged(int wallIndex)
    {
        currentWall = wallIndex;
    }

    private void ApplyHighlight(bool active)
    {
        if(active)
        {
            highlight.enabled = true;
        }
        else
        {
            highlight.enabled = false;
        }

        //Scale pulse
        if(hoverScalePulse > 0f)
        {
            transform.localScale = active
                ? originalScale * (1f + hoverScalePulse)
                : originalScale;
        }
    }
}
