using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class Fragment : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public string fragmentName;
    public Sprite icon;
    public bool[,] shape; //will be initialized in subclasses

    public bool isOnEdge;

    //Internal logic
    private RectTransform rectTransform;
    private Canvas canvas;
    private InventoryManager inventoryManager;
    private PlayerInputActions input; //input action controlelr
    private static int totalEquippedFragments = 0;
    private static int totalExistingFragments = 0;
    private bool isEquipped = false; //TODO: use for deleting unequipped fragments on node change
    private bool isBeingDragged = false;

    void Awake()
    {
        //Setup input
        input = new PlayerInputActions();
        input.UI.Rotate.performed += Rotate;

        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
        inventoryManager = FindFirstObjectByType<InventoryManager>();
        totalExistingFragments++;
    }

    void OnEnable()
    {
        input.Enable();
    }

    void OnDisable()
    {
        input.Disable();
    }

    void OnDestroy()
    {
        totalExistingFragments--;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        isBeingDragged = true;

        //When drag begins, make it the topmost item
        transform.SetAsLastSibling();

        //Remove from inventory manager
        inventoryManager.RemoveFragment(this);

        //Do small graphic shake
        Shake(15f, 0.3f);
    }

    //Actual dragging functionality
    public void OnDrag(PointerEventData eventData)
    {
        rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        isBeingDragged = false;

        //Snap to grid
        if(inventoryManager.SnapToGrid(this))
        {
            //Set layer order
            transform.SetSiblingIndex(1);
        }
    }

    //Method to rotate 90 degrees clockwise
    public void Rotate(InputAction.CallbackContext ctx)
    {
        if(!isBeingDragged)
        {
            return;
        }

        //Need to transpose matrix and then reverse each column
        int width = shape.GetLength(0);
        int height = shape.GetLength(1);

        bool[,] newShape = new bool[height, width]; //reversed dimensions

        for (int i = 0; i < height; i++)
        {
            for (int j = 0; j < width; j++)
            {
                int newRow = width - 1 - j;
                int newCol = height - 1 - i;
                newShape[newRow, newCol] = shape[j, i];
            }
        }

        shape = newShape;

        //Rotate visually
        transform.Rotate(0,0,90);
    }

    //Method to be overridden in children
    //The effects of a fragment should be here, as this is called when a fragment is equipped
    public virtual void ActivateEffects()
    {
        isEquipped = true;
        totalEquippedFragments++;
    }

    //Method to be overridden in children
    //Should deactivate the effects of a fragment, as this is called when a fragment is unequipped
    public virtual void DeactivateEffects()
    {
        isEquipped = false;
        totalEquippedFragments--;
    }

    //Note that the current number of Shake calls is 10; can be adjusted by changing this variable
    private int totalCalls = 10;
    private void Shake(float intensity, float duration, int callsSoFar = 0, float previousRotation = 0f)
    {
        if(callsSoFar < totalCalls)
        {
            float rotationAmount = UnityEngine.Random.Range(0, intensity * (1 - (callsSoFar / 10f)));
            if(callsSoFar % 2 == 1)
            {
                rotationAmount *= -1;
            }
            
            LeanTween.rotateAroundLocal(this.gameObject, Vector3.forward, rotationAmount - previousRotation, duration / totalCalls).setOnComplete(() =>
            {
                Shake(intensity, duration, callsSoFar + 1, rotationAmount);
            });
        }
        else
        {
            LeanTween.rotateAroundLocal(this.gameObject, Vector3.forward, -previousRotation, duration / totalCalls);
        }
    }
}
