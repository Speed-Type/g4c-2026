using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class Fragment : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler
{
    //Note: these variables are now protected (they used to be adjustable in the inspector)
    //Any names/popup details need to be configured in individual subclasses, for sake of making sure they don't get wiped by inspector
    protected string displayBugGroup;
    protected string displayBodyPart;
    protected string displayCommonName;
    protected string displayScientificName;
    protected string displayDescription;

    [Header("Fragment Data")]
    protected int maxHealth;
    protected int currentHealth;

    [Header("GameObject with Sprite for this fragment's graphics")]
    [SerializeField] private GameObject graphicGO;
    [SerializeField] private Color destroyedFragmentColor;

    [Header("Radial Fill Health Indicator")]
    [SerializeField] private Image healthIndicator;

    //Internal logic
    [HideInInspector] public bool[,] shape; //will be initialized in subclasses
    [HideInInspector] public bool isOnEdge;
    private RectTransform rectTransform;
    private Canvas canvas;
    [HideInInspector] public GeneralInventoryStructure belongedInventory;
    private PlayerInputActions input; //input action controlelr
    private bool isBeingDragged = false;
    private int currentRotation = 0;
    private bool isFree;
    private bool effectsActive; //Used for effect logic
    [HideInInspector] public bool isEquipped; //Used for equipped/fragment filtering logic
    private static List<Fragment> fragmentList;

    public static void ResetFragmentList()
    {
        fragmentList = new List<Fragment>();
    }

    void Awake()
    {
        //Setup input
        input = new PlayerInputActions();
        input.UI.Rotate.performed += Rotate;

        //Initialize variables
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
        isFree = false;
        effectsActive = false;
        isEquipped = false;
        currentHealth = maxHealth;

        //Add this instance to the fragment list
        fragmentList.Add(this);
    }

    public void FreeFragment()
    {
        belongedInventory = FindFirstObjectByType<InventoryManager>();
        isFree = true;

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
        FreeFragment();//DELETE LATER
        //Make sure this fragment is free
        if(!isFree) return;

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

        //Hide tooltip
        TooltipManager.HideTooltip_Static();
    }

    //Actual dragging functionality
    public void OnDrag(PointerEventData eventData)
    {
        //Make sure this fragment is free
        if(!isFree) return;

        rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        //Make sure this fragment is free
        if(!isFree) return;

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
        TakeDamage(1);//TEMPORARY
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
        if(effectsActive) return; //Just in case, so as to avoid re-activating effects
        if(currentHealth <= 0) return; //ActivateEffects only handles effect things when not dead
        
        effectsActive = true;
        Debug.Log("Effects activated");
    }

    //Method to be overridden in children
    //Should deactivate the effects of a fragment, as this is called when a fragment is unequipped
    public virtual void DeactivateEffects()
    {
        if(!effectsActive) return; //Just in case, so as to avoid re-deactivating effects
        if(currentHealth <= 0) return; //DeactivateEffects only handles effect things when not dead

        effectsActive = false;
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

    public void OnPointerEnter(PointerEventData eventData)
    {
        if(isBeingDragged) return; //This should not run if it's being dragged

        TooltipManager.ShowTooltip_Static(displayBugGroup + " " + displayBodyPart, displayScientificName, displayCommonName, displayDescription);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        TooltipManager.HideTooltip_Static();
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage; //Reduce health

        //NOTE: The gameobject cancelled is the health indicator, not this fragment itself, since that would cancel every other tween
        LeanTween.cancel(healthIndicator.gameObject);
        
        //Tween update the health indicator fill
        LeanTween.value(healthIndicator.gameObject, healthIndicator.fillAmount, Math.Max((float)currentHealth / maxHealth, 0), .25f).setEaseOutElastic().setOnUpdate((float val) =>
        {
            healthIndicator.fillAmount = val;
        });

        //If dead
        if(currentHealth <= 0)
        {
            Die();
        }
    }

    public void Die()
    {
        //If effects were already active, deactivate them
        if(effectsActive)
        {
            DeactivateEffects();
        }

        //TODO: Graphically update
        graphicGO.GetComponent<Image>().color = destroyedFragmentColor;
    }

    //Method to auto-delete all fragments that are either not free or not equipped
    public static void FilterFragments()
    {
        for(int i = 0; i < fragmentList.Count; i++)
        {
            if(fragmentList[i] == null)
            {
                fragmentList.RemoveAt(i);
                i--;
                continue;
            }

            if(!fragmentList[i].isFree || !fragmentList[i].isEquipped)
            {
                Destroy(fragmentList[i].gameObject); //Fragment is automatically removed from the list, as per OnDestroy() in Fragment
                fragmentList.RemoveAt(i);
                i--;
            }
        }
    }
}
