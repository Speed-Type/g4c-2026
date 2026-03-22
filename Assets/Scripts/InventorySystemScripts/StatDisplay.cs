using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

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
    [SerializeField] private float smoothGrowthAnimLength;

    //Internal variables
    private float inventoryBarMaxWidth;
    private float lootBarMaxWidth;
    private DisplayState displayState;
    private float openHeight;
    private int statWindowTween;
    private Color fleshBarOriginalColor;
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

        //Close stat window
        statWindow.sizeDelta = new Vector2(statWindow.sizeDelta.x, 0);
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
        //TODO: First, update all texts to display the right words n stuff

        //Then, do the tween anim
        TweenStatWindow(openHeight);
    }

    //Method to update a single flesh fill bar
    private void UpdateFleshFillBar(RectTransform fillBar, float maxWidth)
    {
        //Update the fill bar size
        float desiredPercentage = Math.Min(1, (float)RoguelikeGameplayController.instance.currentFlesh / RoguelikeGameplayController.instance.goalFlesh);
        fillBar.sizeDelta = new Vector2(desiredPercentage * maxWidth, fillBar.sizeDelta.y);

        //Flash the bar
        fillBar.gameObject.GetComponent<Image>().color = fleshBarFlashColor;
        LeanTween.delayedCall(flashAnimLength, () =>
        {
            fillBar.gameObject.GetComponent<Image>().color = fleshBarOriginalColor;
        });
    }

    private void UpdateAllFleshFillBars()
    {
        UpdateFleshFillBar(fleshFillBarInventory, inventoryBarMaxWidth);
        UpdateFleshFillBar(fleshFillBarLooting, lootBarMaxWidth);
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