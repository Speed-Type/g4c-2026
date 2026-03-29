using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;
using System.Collections;

public class TooltipManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TextMeshProUGUI tooltipTitle;
    [SerializeField] private TextMeshProUGUI tooltipSubtitle1;
    [SerializeField] private TextMeshProUGUI tooltipSubtitle2;
    [SerializeField] private TextMeshProUGUI tooltipDurabilitySubtitle;
    [SerializeField] private TextMeshProUGUI tooltipDescription;
    [SerializeField] private RectTransform tooltipBackground; //Background of the tooltip
    [SerializeField] private RectTransform tooltipParentRectTransform;
    [SerializeField] private CanvasGroup tooltipCanvasGroup;
    [SerializeField] private GameObject cardHolder;
    [SerializeField] private CardUI cardUI;

    [Header("Other Settings")]
    [SerializeField] private float mouseOffset = 14f;
    [SerializeField] private float tooltipShowDelay = 0.4f;
    [SerializeField] private float tooltipHideDelay = 0.1f;
    [SerializeField] private float fadeDuration = 0.15f;
    [SerializeField] private Color redColor;

    private Coroutine showTooltipCoroutine;
    private Coroutine hideTooltipCoroutine;
    private Color defaultColor;

    //Internal variable for the single instance of the tooltip manager
    private static TooltipManager instance;

    void Awake()
    {
        //Set instance
        instance = this;

        //Initialize default color
        defaultColor = tooltipDurabilitySubtitle.color;

        //Hide tooltip for now
        HideTooltip_Static();
    }

    void Update()
    {
        //Dynamically position and anchor the tooltip based on mouse position
        //Make sure it doesn't go off screen by clamping (flipping pivot didnt' really work)
        Vector2 screenPoint = Mouse.current.position.ReadValue() + new Vector2(mouseOffset, -mouseOffset);

        float width = tooltipBackground.sizeDelta.x;
        float height = tooltipBackground.sizeDelta.y;

        screenPoint.x = Mathf.Min(screenPoint.x, Screen.width - width);
        screenPoint.y = Mathf.Max(screenPoint.y, height);

        tooltipParentRectTransform.position = screenPoint;
    }

    //Coroutine to wait before showing the tooltip
    IEnumerator ShowTooltipDelayed(float start)
    {
        yield return new WaitForSeconds(tooltipShowDelay);

        tooltipParentRectTransform.gameObject.SetActive(true);

        yield return FadeTooltip(start, 0.8f);
    }

    //Coroutine to fade in or out
    IEnumerator FadeTooltip(float start, float end)
    {
        float time = 0f;

        tooltipCanvasGroup.alpha = start;

        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            tooltipCanvasGroup.alpha = Mathf.Lerp(start, end, time / fadeDuration);
            yield return null;
        }

        tooltipCanvasGroup.alpha = end;
    }

    //Helper method to set the text of one chunk of the tooltip
    private void SetSingleTooltipText(TextMeshProUGUI tmp, string text)
    {
        if(text.Equals(""))
        {
            tmp.gameObject.SetActive(false);
        }
        else
        {
            tmp.gameObject.SetActive(true);
            tmp.text = text;
        }
    }

    //Shows tooltip with set text for title, subtitle, and description
    private void ShowTooltip(string title, string subtitle1, string subtitle2, string durability, string description)
    {
        //Keep card UI hidden unless explicitly called to show it
        if(cardHolder != null) cardHolder.gameObject.SetActive(false);

        //Necessary for grace period between shows/hides
        if(hideTooltipCoroutine != null)
        {
            StopCoroutine(hideTooltipCoroutine);
            hideTooltipCoroutine = null;

            tooltipCanvasGroup.alpha = 1;
        }

        //Set texts
        SetSingleTooltipText(tooltipTitle, title);
        SetSingleTooltipText(tooltipSubtitle1, subtitle1);
        SetSingleTooltipText(tooltipSubtitle2, subtitle2);
        SetSingleTooltipText(tooltipDurabilitySubtitle, durability);
        SetSingleTooltipText(tooltipDescription, description);

        //If we're going from object to object, don't have to delay or fade in/out
        if(tooltipParentRectTransform.gameObject.activeSelf)
        {
            return;
        }

        if(showTooltipCoroutine != null)
        {
            StopCoroutine(showTooltipCoroutine);
        }

        showTooltipCoroutine = StartCoroutine(ShowTooltipDelayed(0f));
    }

    //Shows tooltip with simple text
    private void ShowTooltip(string simpleText)
    {
        //Just call multi-parameter function with every other field but description empty
        ShowTooltip("", "", "", "", simpleText);
    }

    //Hides tooltip
    private void HideTooltip()
    {
        //Stop the tooltip delay, like a dash cancel
        if(showTooltipCoroutine != null)
        {
            StopCoroutine(showTooltipCoroutine);
        }

        hideTooltipCoroutine = StartCoroutine(HideTooltipDelayed());
    }

    private void ShowCard(Card card)
    {
        if(cardUI != null) cardUI.UpdateCardDisplay(card);
        if(cardHolder != null) cardHolder.gameObject.SetActive(true);
    }

    //Coroutine to give a bit of grace period before hiding
    IEnumerator HideTooltipDelayed()
    {
        yield return new WaitForSeconds(tooltipHideDelay);

        yield return FadeTooltip(0.8f, 0f);

        tooltipParentRectTransform.gameObject.SetActive(false);
    }

    public static void ShowTooltip_Static(string title, string subtitle1, string subtitle2, string durability, string description)
    {
        instance.ShowTooltip(title, subtitle1, subtitle2, durability, description);
    }

    public static void ShowTooltip_Static(string simpleText)
    {
        instance.ShowTooltip(simpleText);
    }

    public static void ShowCard_Static(Card card)
    {
        instance.ShowCard(card);
    }

    public static void HideTooltip_Static()
    {
        instance.HideTooltip();
    }

    private void ColorRed()
    {
        tooltipDurabilitySubtitle.color = redColor;
    }

    private void ColorDefault()
    {
        tooltipDurabilitySubtitle.color = defaultColor;
    }

    public static void ColorRed_Static()
    {
        instance.ColorRed();
    }

    public static void ColorDefault_Static()
    {
        instance.ColorDefault();
    }
}
