using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class StatDisplay : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("GameObject References")]
    [SerializeField] private RectTransform statWindow;
    [SerializeField] private RectTransform fleshFillBarInventory;
    [SerializeField] private RectTransform fleshFillBarLooting;

    [Header("Stat Display Popup Anim Params")]
    [SerializeField] private float peekHeight;
    [SerializeField] private float statWindowAnimLength;

    [Header("Flesh Fill Bar Anim Params")]
    [SerializeField] private float flashAnimLength;
    [SerializeField] private Color fleshBarFlashColor;

    [Header("Flesh Text Displays")]
    [SerializeField] private TMP_Text inventoryFleshText;
    [SerializeField] private TMP_Text lootFleshText;

    [Header("Stat Window Content")]
    [SerializeField] private TMP_Text stealthStatsText;
    [SerializeField] private Image biteImage;
    [SerializeField] private float biteImageMaxHeight;
    [SerializeField] private TMP_Text biteStatsText;
    [SerializeField] private TMP_Text sensoryStatsText;

    //Internal variables
    private float inventoryBarMaxWidth;
    private float lootBarMaxWidth;
    private DisplayState displayState;
    private float openHeight;
    private int statWindowTween;
    private Color fleshBarOriginalColor;
    private RectTransform biteRectTransform;
    private static StatDisplay instance;

    void Awake()
    {
        //Set the static instance to this one instance
        instance = this;

        //Initialize variables
        openHeight = statWindow.sizeDelta.y;
        displayState = DisplayState.Closed;
        inventoryBarMaxWidth = fleshFillBarInventory.sizeDelta.x;
        lootBarMaxWidth = fleshFillBarLooting.sizeDelta.x;
        fleshBarOriginalColor = fleshFillBarInventory.gameObject.GetComponent<Image>().color;
        biteRectTransform = biteImage.GetComponent<RectTransform>();

        //Close stat window
        statWindow.sizeDelta = new Vector2(statWindow.sizeDelta.x, 0);
    }

    void Start()
    {
        UpdateAllFleshFillBars();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        //Toggle stat window open/close
        if(displayState == DisplayState.Open)
        {
            //Close the window
            TweenStatWindow(0);
            displayState = DisplayState.Closed;
        }
        else
        {
            //In all other cases, open the windows
            OpenDisplay();
            displayState = DisplayState.Open;
        }
    }

    //Method called on hover
    public virtual void OnPointerEnter(PointerEventData eventData)
    {
        if(displayState == DisplayState.Closed)
        {
            //Peek out a bit
            TweenStatWindow(peekHeight);
            displayState = DisplayState.Peek;
        }
    }

    //Method called on hover stop
    public virtual void OnPointerExit(PointerEventData eventData)
    {
        //Make stat window close ONLY IF it's peeked
        if(displayState == DisplayState.Peek)
        {
            //Close
            TweenStatWindow(0);
            displayState = DisplayState.Closed;
        }
    }

    private void TweenStatWindow(float endVal)
    {
        //Just tween anim stuff
        LeanTween.cancel(statWindowTween);
        statWindowTween = LeanTween.value(statWindow.sizeDelta.y, endVal, statWindowAnimLength)
            .setEaseOutElastic()
            .setOnUpdate((float val) =>
            {
                statWindow.sizeDelta = new Vector2(statWindow.sizeDelta.x, val);
            })
            .id;
    }

    private void OpenDisplay()
    {
        //Update stealth stats
        bool isCritical = RoguelikeGameplayController.instance.currentSpiderAlertness >= RoguelikeGameplayController.instance.maxSpiderAlertness;
        stealthStatsText.text = 
            "Spider Alertness: " + RoguelikeGameplayController.instance.currentSpiderAlertness + (isCritical ? " (!!!)" : "") + "\n" +
            "Critical Spider Alertness: " + RoguelikeGameplayController.instance.maxSpiderAlertness + "\n" +
            "Chance for Spider to pursue: " + RoguelikeGameplayController.ChanceForSpiderToPursue().ToString("P2") + "\n" +
            "Camouflage Chance: " + (isCritical ? "0.00% (Instakill)" : RoguelikeGameplayController.instance.stealthChance.ToString("P2"));

        //Store current bite's fragment script for sake of speed
        GameObject currentBite = RoguelikeGameplayController.GetCurrentBiteGO_Static();
        Fragment_Bite fragmentScript = currentBite.GetComponent<Fragment_Bite>();

        //Update bite stats
        biteStatsText.text = 
            "Damage: " + fragmentScript.damage + "\n" +
            "Uses: " + RoguelikeGameplayController.instance.maxBites + "\n" +
            "Dimensions: " + fragmentScript.shape.GetLength(1) + "x" + fragmentScript.shape.GetLength(0);

        //Update bite image
        float width = biteImageMaxHeight / fragmentScript.shape.GetLength(0) * currentBite.GetComponent<Fragment_Bite>().shape.GetLength(1);
        biteImage.sprite = fragmentScript.openBiteSprite;
        biteRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);

        //Update sensory stats
        sensoryStatsText.text = 
            "Empty Nodes: " + RoguelikeGameplayController.instance.emptyNodesToReveal + "\n" +
            "Scavenge Nodes: " + RoguelikeGameplayController.instance.scavengerNodesToReveal + "\n";

        //Then, do the tween anim
        TweenStatWindow(openHeight);
    }

    //Method to update a single flesh fill bar
    private void UpdateFleshFillBar(RectTransform fillBar, float maxWidth)
    {
        //Update the fill bar size
        float desiredPercentage = Math.Min(1, (float)RoguelikeGameplayController.instance.currentFlesh / RoguelikeGameplayController.instance.goalFlesh);
        fillBar.gameObject.GetComponent<Image>().color = fleshBarFlashColor; //set color to flashed
        LeanTween.value(fillBar.sizeDelta.x, desiredPercentage * maxWidth, flashAnimLength)
            .setEaseOutElastic()
            .setOnUpdate((float val) =>
            {
                fillBar.sizeDelta = new Vector2(val, fillBar.sizeDelta.y);
            })
            .setOnComplete(() =>
            {
                if((float)RoguelikeGameplayController.instance.currentFlesh / RoguelikeGameplayController.instance.goalFlesh < 1)
                {
                    fillBar.gameObject.GetComponent<Image>().color = fleshBarOriginalColor; //reset color when done, unless of course we've hit the goal
                }
            });
    }

    private void UpdateAllFleshFillBars()
    {
        //Update the fill bars
        UpdateFleshFillBar(fleshFillBarInventory, inventoryBarMaxWidth);
        UpdateFleshFillBar(fleshFillBarLooting, lootBarMaxWidth);

        //Update the texts
        string str = RoguelikeGameplayController.instance.currentFlesh + "/" + RoguelikeGameplayController.instance.goalFlesh;
        inventoryFleshText.text = "Flesh: " + str;
        lootFleshText.text = str;
    }

    public static void UpdateAllFleshFillBars_Static()
    {
        instance.UpdateAllFleshFillBars();
    }
}

enum DisplayState
{
    Open,
    Peek,
    Closed
}