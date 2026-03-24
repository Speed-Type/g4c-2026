using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using System.Collections;
using UnityEngine.UI;


public class Fragment_Bite : Fragment
{
    public int damage = 1;
    public Sprite openBiteSprite;
    public Sprite closedBiteSprite;

    //Scale factor should be the multiplier necessary to make the 100x100 ver. of this fragment into its actual size
    public float scaleFactor;
    private int shakeTween;
    private Image graphicImage;
    private RectTransform graphicRectTransform;

    public Fragment_Bite()
    {
        shape = new bool[,] 
        {
            {true, true},
            {true, true}
        };

        damage = 1;
    }

    //OVERRIDDEN/HIDDEN METHODS

    //Separate Awake method
    protected override void Awake() //Hides the base class' awake
    {
        //Initialize dummy values, since this is a fragment
        maxHealth = 999;
        displayBugGroup = "";
        displayBodyPart = "";
        displayCommonName = "";
        displayScientificName = "";
        displayDescription = "";

        //Setup input
        input = new PlayerInputActions();
        input.UI.Rotate.performed += Rotate;

        //Initialize variables
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
        canvasRectTransform = canvas.GetComponent<RectTransform>();
        belongedInventory = FindFirstObjectByType<LootManager>();
        graphicImage = graphicGO.GetComponent<Image>();
        graphicRectTransform = graphicGO.GetComponent<RectTransform>();

        //Set size to shrunken size
        graphicGO.transform.localScale = Vector3.one / scaleFactor;
    }

    public override void OnBeginDrag(PointerEventData eventData)
    {
        if(isBiting) return;

        isBeingDragged = true;

        //Do continual shake
        shakeTween = LeanTween.rotateZ(gameObject, 3f, 0.08f)
            .setEaseShake()
            .setLoopPingPong()
            .id;
        
        //Scale up graphic to normal size
        LeanTween.scale(graphicRectTransform, Vector3.one, 0.1f);

        //Start highlight coroutine
        StartCoroutine(TimedBiteHighlightCoroutine());
    }

    //NOTE: OnDrag does need to stay overridden
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

    private bool isBiting = false;
    public override void OnEndDrag(PointerEventData eventData)
    {
        if(isBiting) return;

        isBiting = true;
        isBeingDragged = false;

        //Do the actual biting here
        if(belongedInventory.TryBite(this, damage))
        {
            RoguelikeGameplayController.ReduceBites(); //if successful, have the rg gameplay controller process bites remaining
        }

        //Stop shake
        LeanTween.cancel(shakeTween);
        rectTransform.rotation = Quaternion.identity; //Reset rotation

        //Clear inventory highlight
        belongedInventory.ClearBiteHighlight();

        //Now do visual for bite

        graphicImage.sprite = closedBiteSprite; //change sprite

        //Spawn particles
        GameObject newParticles = Instantiate(deathParticles, transform.parent.transform);
        newParticles.GetComponent<RectTransform>().position = rectTransform.position;
        newParticles.transform.SetAsFirstSibling(); //Move under the bite
        
        //Tween crunch
        LeanTween.rotateZ(gameObject, 12f, 0.05f)
            .setEaseShake()
            .setLoopPingPong(2)
            .setOnComplete(() =>
            {
                //And then reposition
                RoguelikeGameplayController.ReloadBite_Static();
                graphicGO.transform.localScale = Vector3.one / scaleFactor; //Shrink graphic size again
                rectTransform.rotation = Quaternion.identity; //Reset rotation
                graphicImage.sprite = openBiteSprite;
                isBiting = false;
            });
    }

    public override void Rotate()
    {
        //Should not be able to rotate
    }

    public override bool ActivateEffects()
    {
        //Has no effect
        return false;
    }

    public override bool DeactivateEffects()
    {
        //Has no effect
        return false;
    }

    //Method for starting tooltip hovering
    public override void OnPointerEnter(PointerEventData eventData)
    {
        //no tooltip
        Shake(); //do little shake effect
    }

    //Method for stopping tooltip hovering
    public override void OnPointerExit(PointerEventData eventData)
    {
        //no tooltip
    }

    //METHODS UNIQUE TO BITE

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
