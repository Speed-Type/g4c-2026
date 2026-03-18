using System;
using System.Collections;
using Unity.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class Fragment : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Fragment Data")]
    public string displayBodyPart;
    public string displayBug;
    public string displayScientificName;
    [TextArea(3,5)] public string displayDescription;
    public int maxHealth;
    public int currentHealth;

    [Header("GameObject with Sprite for this fragment's graphics")]
    public GameObject graphicGO;

    //Internal logic
    [HideInInspector] public bool[,] shape; //will be initialized in subclasses
    [HideInInspector] public bool isOnEdge;
    private RectTransform rectTransform;
    private Canvas canvas;
    [HideInInspector] public GeneralInventoryStructure belongedInventory;
    private PlayerInputActions input; //input action controlelr
    private bool isBeingDragged = false;
    private int currentRotation = 0;
    [HideInInspector] public bool belongsToPlayer;

    void Awake()
    {
        //Setup input
        input = new PlayerInputActions();
        input.UI.Rotate.performed += Rotate;

        //Initialize variables
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
        belongsToPlayer = false;
        currentHealth = maxHealth;

        //FreeFragment(); //DELETE LATER; THIS IS TEMPORARY TO TEST OUT FRAGMENT FREEING
    }

    //TODO: free the fragment here
    public void FreeFragment()
    {
        belongedInventory = FindFirstObjectByType<InventoryManager>();
        belongsToPlayer = true;

        //Change parent transform to inventory system parent transform
        transform.SetParent(belongedInventory.inventorySystemParentTransform);

        //TODO: Possibly set sibling index here as well
    }

    void OnEnable()
    {
        input.Enable();
    }

    void OnDisable()
    {
        input.Disable();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        //Make sure this fragment is free
        if(!belongsToPlayer) return;

        isBeingDragged = true;

        //When drag begins, make it the topmost item
        transform.SetAsLastSibling();

        //Remove from corresponding manager
        belongedInventory.RemoveFragment(this);

        //Stop any previous tween from snapping to a position
        LeanTween.cancel(this.gameObject);

        //Do small graphic shake
        Shake(15f, 0.3f);

        //Start highlight coroutine
        StartCoroutine(TimedHighlightCoroutine());
    }

    //Actual dragging functionality
    public void OnDrag(PointerEventData eventData)
    {
        //Make sure this fragment is free
        if(!belongsToPlayer) return;

        rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        //Make sure this fragment is free
        if(!belongsToPlayer) return;

        isBeingDragged = false;

        //Snap to grid
        if(belongedInventory.SnapToGrid(this))
        {
            //Set layer order
            transform.SetSiblingIndex(1);
        }

        //Clear inventory highlight
        belongedInventory.ClearHighlight();
    }

    //Overridden method to be called by input system
    public void Rotate(InputAction.CallbackContext ctx)
    {
        if(!isBeingDragged)
        {
            return;
        }

        Rotate();
    }

    //Method to rotate 90 degrees clockwise
    public void Rotate()
    {
        //Need to transpose matrix and then reverse each column
        int numRows = shape.GetLength(0);
        int numCols = shape.GetLength(1);

        bool[,] newShape = new bool[numCols, numRows]; //reversed dimensions

        for(int i = 0; i < numRows; i++)
        {
            for(int j = 0; j < numCols; j++)
            {
                //int newRow = width - 1 - j;
                //int newCol = height - 1 - i;
                newShape[numCols - 1 - j, i] = shape[i, j];
            }
        }

        shape = newShape;

        //GRAPHICALLY

        //Stop current tween for shake effect
        LeanTween.cancel(this.gameObject);
        transform.localRotation = Quaternion.Euler(0,0,0);

        //Change current rotation positioning
        currentRotation = (currentRotation + 1) % 4;

        //Rotate
        graphicGO.transform.localRotation *= Quaternion.Euler(0,0,90);
        
        //Calculate offset
        Vector2 offset;
        numRows = shape.GetLength(0);
        numCols = shape.GetLength(1);

        switch(currentRotation)
        {
            case 0:
                offset = new Vector2(0,0);
                break;
            case 1:
                offset = new Vector2(0, -numRows);
                break;
            case 2:
                offset = new Vector2(numCols, -numRows);
                break;
            case 3:
                offset = new Vector2(numCols, 0);
                break;
            default:
                offset = new Vector2(0,0);
                break;
        }

        offset *= belongedInventory.inventorySlotSize;

        //Apply offset
        graphicGO.transform.localPosition = new Vector3(offset.x, offset.y, 0);

        //Do small graphic shake
        Shake(15f, 0.3f);
    }

    //Method to be overridden in children
    //The effects of a fragment should be here, as this is called when a fragment is equipped
    public virtual void ActivateEffects()
    {
        Debug.Log("Effects activated");
    }

    //Method to be overridden in children
    //Should deactivate the effects of a fragment, as this is called when a fragment is unequipped
    public virtual void DeactivateEffects()
    {
        Debug.Log("Effects deactivated");
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

    IEnumerator TimedHighlightCoroutine()
    {
        belongedInventory.Highlight(this); //call highlight

        yield return new WaitForSeconds(.15f); //wait; Can adjust this timing as necessary

        if(isBeingDragged)
        {
            StartCoroutine(TimedHighlightCoroutine());
        }
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        if(currentHealth <= 0)
        {
            
        }
    }
}
