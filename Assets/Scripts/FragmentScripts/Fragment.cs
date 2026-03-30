using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.VisualScripting;
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
    public int currentHealth;

    [Header("GameObject with Sprite for this fragment's graphics")]
    public GameObject graphicGO;
    [SerializeField] private Color destroyedFragmentColor;

    [Header("Radial Fill Health Indicator")]
    [SerializeField] private Image healthIndicator;

    [Header("Particle Effects for Fragment OR Flesh Destruction")]
    [SerializeField] protected GameObject deathParticles;

    [Header("3D Model for Fragment")]
    public GameObject fragmentModel;
    [HideInInspector] public GameObject existingFragmentModel;

    //Internal logic
    [HideInInspector] public bool[,] shape; //will be initialized in subclasses
    [HideInInspector] public bool isOnEdge;
    protected RectTransform rectTransform;
    protected Canvas canvas;
    protected RectTransform canvasRectTransform;
    [HideInInspector] public GeneralInventoryStructure belongedInventory;
    protected PlayerInputActions input; //input action controller
    protected bool isBeingDragged = false;
    protected int currentRotation = 0;
    private bool isFree;
    private bool effectsActive; //Used for effect logic
    [HideInInspector] public bool isEquipped; //Used for equipped/fragment filtering logic
    
    //LeanTween References
    private int wiggleTween;
    private int undulateTween;
    private int rotateTween;

    protected Card card;

    //Static list for all fragments
    private static List<Fragment> fragmentList;

    protected virtual void Awake()
    {
        //Setup input
        input = new PlayerInputActions();

        //Initialize variables
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
        canvasRectTransform = canvas.GetComponent<RectTransform>();
        isFree = false;
        effectsActive = false;
        isEquipped = false;
        currentHealth = maxHealth;

        //Add this instance to the fragment list
        fragmentList.Add(this);
    }

    public static void ResetFragmentList()
    {
        fragmentList = new List<Fragment>();
    }

    void OnEnable()
    {
        input.UI.Rotate.performed += Rotate;
    }

    void OnDisable()
    {
        input.UI.Rotate.performed -= Rotate;
    }

    public virtual void OnBeginDrag(PointerEventData eventData)
    {
        //Try to free this fragment if it is not already free
        if(!isFree)
        {
            TryFreeFragment();
        }

        if(!isFree) return; //If still not free, return

        isBeingDragged = true;

        //When drag begins, make it the topmost item
        transform.SetAsLastSibling();

        //Remove from corresponding manager
        belongedInventory.RemoveFragment(this);

        //Stop any previous tween from snapping to a position
        LeanTween.cancel(this.gameObject);

        //Do graphics stuff
        undulateTween = LeanTween.scale(rectTransform, Vector3.one * 1.05f, 0.45f) //Scale undulate
            .setEaseInOutSine()
            .setLoopPingPong()
            .id;

        wiggleTween = LeanTween.rotateZ(gameObject, 1.25f, 0.175f) //Wiggle rotation
            .setEaseShake()
            .setLoopPingPong()
            .id;

        Shake();

        //Start highlight coroutine
        StartCoroutine(TimedHighlightCoroutine());

        //Hide tooltip
        TooltipManager.HideTooltip_Static();
    }

    //Actual dragging functionality
    public virtual void OnDrag(PointerEventData eventData)
    {
        //Make sure this fragment is free
        if(!isFree)
        {
            //Do visual indicator that it isn't free
            Shake();
            return;
        }

        //Do the actual movement (with calculations so that it stays within screen bounds)
        Vector2 newPosition = rectTransform.anchoredPosition + eventData.delta / canvas.scaleFactor;

        Vector2 canvasSize = canvasRectTransform.rect.size;
        Vector2 elementSize = rectTransform.rect.size;

        float minX = -canvasSize.x / 2 + elementSize.x / 2;
        float maxX = canvasSize.x / 2 - elementSize.x / 2;

        float minY = -canvasSize.y / 2 + elementSize.y / 2;
        float maxY = canvasSize.y / 2 - elementSize.y / 2;

        newPosition.x = Mathf.Clamp(newPosition.x, minX, maxX);
        newPosition.y = Mathf.Clamp(newPosition.y, minY, maxY);

        rectTransform.anchoredPosition = newPosition;
    }

    public virtual void OnEndDrag(PointerEventData eventData)
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

        //Do graphics reset
        //Stop tweens
        LeanTween.cancel(wiggleTween);
        LeanTween.cancel(undulateTween);

        //Reset rotation and scale
        LeanTween.rotateZ(gameObject, 0f, 0.1f);
        LeanTween.scale(rectTransform, Vector3.one, 0.1f);

        //Clear inventory highlight
        belongedInventory.ClearHighlight();
    }

    //Method for starting tooltip hovering
    public virtual void OnPointerEnter(PointerEventData eventData)
    {
        if(isBeingDragged) return; //This should not run if it's being dragged

        //Determine what to display for fragment health
        string durabilityDescription = "Durability: " + Math.Max(currentHealth, 0) + "/" + maxHealth + "\n";
        if(currentHealth <= 0)
        {
            TooltipManager.ColorRed_Static();
        }
        else
        {
            TooltipManager.ColorDefault_Static();
        }

        TooltipManager.ShowTooltip_Static(displayBugGroup + " " + displayBodyPart, displayScientificName, displayCommonName, durabilityDescription, displayDescription);

        if(card != null) TooltipManager.ShowCard_Static(card);
    }

    //Method for stopping tooltip hovering
    public virtual void OnPointerExit(PointerEventData eventData)
    {
        TooltipManager.HideTooltip_Static();
    }

    //Method to remove fragment; nothing needs to be changed in subclasses for this (only flesh overrides it with an empty method)
    public virtual void TryFreeFragment()
    {
        //If there is any surrounding flesh, the method stops here
        if(belongedInventory.IsAdjacentFlesh(this)) return;

        //Otherwise, remove this fragment from the loot manager and then make this fragment belong to the player
        belongedInventory.RemoveFragment(this);
        belongedInventory = FindFirstObjectByType<InventoryManager>();
        isFree = true;

        //Change parent transform to inventory system parent transform
        transform.SetParent(belongedInventory.inventorySystemParentTransform);
    }

    //Overridden method to be called by input system
    public void Rotate(InputAction.CallbackContext ctx)
    {
        Debug.Log("rotating");
        if(!isBeingDragged)
        {
            return;
        }


        Debug.Log("Still rotating");
        Rotate();
    }

    //Method to rotate 90 degrees clockwise
    public virtual void Rotate()
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

        //Do small shake graphic
        Shake();

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
    }

    //Method to be overridden in children
    //The effects of a fragment should be here, as this is called when a fragment is equipped
    public virtual bool ActivateEffects()
    {
        if(effectsActive) return false; //Just in case, so as to avoid re-activating effects
        if(currentHealth <= 0) return false; //ActivateEffects only handles effect things when not dead
        
        effectsActive = true;
        Debug.Log("Effects activated");

        //Update stat display
        StatDisplay.UpdateStats_Static();

        //Do small screenshake
        ScreenShake.instance.AddTrauma(0.08f);

        return true;
    }

    //Method to be overridden in children
    //Should deactivate the effects of a fragment, as this is called when a fragment is unequipped
    public virtual bool DeactivateEffects()
    {
        if(!effectsActive) return false; //Just in case, so as to avoid re-deactivating effects
        if(currentHealth <= 0) return false; //DeactivateEffects only handles effect things when not dead

        effectsActive = false;
        Debug.Log("Effects deactivated");

        //Update stat display
        StatDisplay.UpdateStats_Static();
        
        return true;
    }

    protected void Shake()
    {
        //Stop current tween for shake effect
        LeanTween.cancel(rotateTween);
        transform.localRotation = Quaternion.Euler(0,0,0);

        //Do small graphic shake
        rotateTween = LeanTween.rotateZ(gameObject, 6f, 0.05f)
            .setEaseShake()
            .setLoopPingPong(2)
            .id;
    }

    protected IEnumerator TimedHighlightCoroutine()
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
        currentHealth -= damage; //Reduce health
        
        //Tween update the health indicator fill
        UpdateDamageIndicator(Math.Max((float)currentHealth / maxHealth, 0));

        //If dead
        if(currentHealth <= 0)
        {
            Die();
        }
    }

    private int wouldBeDamage = 0;
    public void ShowWouldBeDamage(int damage)
    {
        wouldBeDamage += damage;
        UpdateDamageIndicator(Math.Max((float)(currentHealth - wouldBeDamage) / maxHealth, 0));
    }

    public static void HideWouldBeDamage()
    {
        foreach(Fragment fragment in fragmentList)
        {
            if(!fragment.isFree && fragment.wouldBeDamage > 0)
            {
                fragment.wouldBeDamage = 0;
                fragment.UpdateDamageIndicator(Math.Max((float)fragment.currentHealth / fragment.maxHealth, 0));
            }
        }
    }
    
    public void UpdateDamageIndicator(float fillVal)
    {
        if(IsFlesh()) return; //No need to run this for flesh

        //NOTE: The gameobject cancelled is the health indicator, not this fragment itself, since that would cancel every other tween
        LeanTween.cancel(healthIndicator.gameObject);

        LeanTween.value(healthIndicator.gameObject, healthIndicator.fillAmount, fillVal, .2f).setEaseOutElastic().setOnUpdate((float val) =>
        {
            healthIndicator.fillAmount = val;
        });
    }

    public bool isDead = false;
    public virtual void Die()
    {
        if(isDead) return; //only want to call this once
        isDead = true;

        //If effects were already active, deactivate them
        if(effectsActive)
        {
            DeactivateEffects();
        }
        else
        {
            //If the fragment was shattered outside of inventory, increase alertness because it was destroyed while looting
            RoguelikeGameplayController.instance.currentSpiderAlertness += 3;
        }

        //Graphically update
        graphicGO.GetComponent<Image>().color = destroyedFragmentColor;

        Shake(); //Animate this fragment GO

        ScreenShake.instance.AddTrauma(0.04f); //Screenshake, on top of bite screenshake

        //Spawn Particles
        GameObject spawnedParticles = RoguelikeCameraController.SpawnParticle(transform.position + new Vector3(shape.GetLength(1) * belongedInventory.inventorySlotSize / 2f, -shape.GetLength(0) * belongedInventory.inventorySlotSize / 2f, 0), deathParticles);
    
        if(isFree)
        {
            //Parent the spawned particles under the inventory system
            spawnedParticles.transform.SetParent(RoguelikeGameplayController.instance.inventorySystemGO.transform);
        }
        else
        {
            //Parent the spawned particles under the loot system
            spawnedParticles.transform.SetParent(RoguelikeGameplayController.instance.lootSystemGO.transform);
        }
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

    public virtual bool IsFlesh()
    {
        return false;
    }

    //Returns center's grid offset from the top left corner pivot position
    public Vector2 GetCenterOffset()
    {
        return new Vector2(shape.GetLength(0) / 2f, shape.GetLength(1) / 2f);
    }
}
