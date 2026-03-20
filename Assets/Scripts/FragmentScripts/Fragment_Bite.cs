using UnityEngine;

using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using System.Collections;


public class Fragment_Bite : Fragment
{
    public Fragment_Bite()
    {
        shape = new bool[,] //default bite
        {
            {true, true},
            {true, true}
        };

        maxHealth = 999;

        displayBugGroup = "";
        displayBodyPart = "";
        displayCommonName = "";
        displayScientificName = "";
        displayDescription = "";
    }

    //OVERRIDDEN/HIDDEN METHODS

    //Separate Awake method
    protected override void Awake() //SHOULD HIDE THE FRAGMENT BASE CLASS'S AWAKE
    {
        //STILL CHANGING

        //Setup input
        input = new PlayerInputActions();
        input.UI.Rotate.performed += Rotate;

        //Initialize variables
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
        canvasRectTransform = canvas.GetComponent<RectTransform>();
        //isFree = false;
        //effectsActive = false;
        //isEquipped = false;
        //currentHealth = maxHealth;

        ////Add this instance to the fragment list
        //fragmentList.Add(this);
    }

    public override void OnBeginDrag(PointerEventData eventData)
    {
        isBeingDragged = true;

        // //When drag begins, make it the topmost item
        // transform.SetAsLastSibling();

        // //Remove from corresponding manager
        // belongedInventory.RemoveFragment(this);

        // //Stop any previous tween from snapping to a position
        // LeanTween.cancel(this.gameObject);

        //Do small graphic shake
        Shake(15f, 0.3f);

        //Start highlight coroutine
        StartCoroutine(TimedHighlightCoroutine());

        // //Hide tooltip
        // TooltipManager.HideTooltip_Static();
    }

    public override void OnDrag(PointerEventData eventData)
    {
        // //Make sure this fragment is free
        // if(!isFree) return;

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

    public override void OnEndDrag(PointerEventData eventData)
    {
        // //Make sure this fragment is free
        // if(!isFree) return;

        isBeingDragged = false;

        //TODO: ADD THE ACTUAL BITING HERE

        // //Snap to grid
        // if(belongedInventory.SnapToGrid(this))
        // {
        //     //Set layer order
        //     transform.SetSiblingIndex(1);
        // }

        //Clear inventory highlight
        belongedInventory.ClearHighlight();
    }

    public override void Rotate()
    {
        //Should not be able to rotate
    }

    public override void ActivateEffects()
    {
        //Has no effect
    }

    public override void DeactivateEffects()
    {
        //Has no effect
    }

    //Method for starting tooltip hovering
    public override void OnPointerEnter(PointerEventData eventData)
    {
        //no tooltip
    }

    //Method for stopping tooltip hovering
    public override void OnPointerExit(PointerEventData eventData)
    {
        //no tooltip
    }



    //METHODS UNIQUE TO BIT
    public void UpdateShape(bool[,] newShape)
    {
        shape = newShape;
    }
}
