using UnityEngine;
using UnityEngine.EventSystems;

public class StatDisplay : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("GameObject References")]
    [SerializeField] private RectTransform statWindow;
    [SerializeField] private RectTransform fleshFillBarInventory;
    [SerializeField] private RectTransform fleshFillBarLooting;

    [Header("Stat Display Popup Anim Params")]
    [SerializeField] private float peekHeight;
    [SerializeField] private float statWindowAnimLength;

    //Internal variables
    private DisplayState displayState;
    private float openHeight;
    private int statWindowTween;
    private static StatDisplay instance;

    void Awake()
    {
        //Set the static instance to this one instance
        instance = this;

        //Initialize variables
        openHeight = statWindow.sizeDelta.y;
        displayState = DisplayState.Closed;
    }

    void OnEnable()
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
            .setEaseInOutQuart()
            .setOnUpdate((float val) =>
            {
                statWindow.sizeDelta = new Vector2(statWindow.sizeDelta.x, val);
            })
            .id;
    }

    private void OpenDisplay()
    {
        //First, update all texts to display the right words n stuff

        //Then, do the tween anim
        TweenStatWindow(openHeight);
    }

    private void UpdateFleshFillBar(RectTransform fillBar)
    {
        //TODO: Method to update a single flesh fill bar
    }

    private void UpdateAllFleshFillBars()
    {
        UpdateFleshFillBar(fleshFillBarInventory);
        UpdateFleshFillBar(fleshFillBarLooting);
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