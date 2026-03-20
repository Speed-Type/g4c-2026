using UnityEngine;

using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using System.Collections;


public class Fragment_Bite : Fragment
{
    private int damage;
    private int shakeTween;

    public Fragment_Bite()
    {
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
        //Setup input
        input = new PlayerInputActions();
        input.UI.Rotate.performed += Rotate;

        //Initialize variables
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
        canvasRectTransform = canvas.GetComponent<RectTransform>();
        belongedInventory = FindFirstObjectByType<LootManager>();

        //Set bite
        UpdateBite(new bool[,]
        {
            {true, true},
            {true, true}
        }, 1);
    }

    public override void OnBeginDrag(PointerEventData eventData)
    {
        isBeingDragged = true;

        //Do continual shake
        shakeTween = LeanTween.rotateZ(gameObject, 3f, 0.08f)
            .setEaseShake()
            .setLoopPingPong()
            .id;
        
        //Also scale up a teense
        LeanTween.scale(rectTransform, Vector3.one * 1.05f, 0.1f);

        //Start highlight coroutine
        StartCoroutine(TimedBiteHighlightCoroutine());
    }

    public override void OnDrag(PointerEventData eventData)
    {
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
        isBeingDragged = false;

        //Do the actual biting here
        belongedInventory.TryBite(this, damage);

        //Clear inventory highlight
        belongedInventory.ClearBiteHighlight(); //TODO: make sure this clears the bite's unique higlights too

        //Stop shake
        LeanTween.cancel(shakeTween);
        rectTransform.rotation = Quaternion.identity; //Reset rotation

        //Reset scale
        LeanTween.scale(rectTransform, Vector3.one, 0.1f);
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



    //METHODS UNIQUE TO BITE
    public void UpdateBite(bool[,] newShape, int newDamage)
    {
        shape = newShape;
        damage = newDamage;
    }

    private IEnumerator TimedBiteHighlightCoroutine()
    {
        belongedInventory.BiteHighlight(this, damage); //call highlight

        yield return new WaitForSeconds(.15f); //wait; Can adjust this timing as necessary

        if(isBeingDragged)
        {
            StartCoroutine(TimedBiteHighlightCoroutine());
        }
    }
}
