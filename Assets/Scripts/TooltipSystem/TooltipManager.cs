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
    [SerializeField] private TextMeshProUGUI tooltipDescription;
    [SerializeField] private RectTransform tooltipBackground; //Background of the tooltip
    [SerializeField] private RectTransform tooltipParentRectTransform;
    [SerializeField] private CanvasGroup tooltipCanvasGroup;

    [Header("Other Settings")]
    [SerializeField] private float mouseOffset = 14f;
    [SerializeField] private float tooltipShowDelay = 0.4f;
    [SerializeField] private float tooltipHideDelay = 0.1f;
    [SerializeField] private float fadeDuration = 0.15f;

    private Coroutine showTooltipCoroutine;
    private Coroutine hideTooltipCoroutine;

    //Internal variable for the single instance of the tooltip manager
    private static TooltipManager instance;

    void Awake()
    {
        instance = this;
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
        screenPoint.y = Mathf.Min(screenPoint.y, Screen.height - height);

        tooltipParentRectTransform.position = screenPoint;
    }

    //Coroutine to wait before showing the tooltip
    IEnumerator ShowTooltipDelayed(float start)
    {
        yield return new WaitForSeconds(tooltipShowDelay);

        tooltipParentRectTransform.gameObject.SetActive(true);

        yield return FadeTooltip(start, 1f);
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

    //Shows tooltip with set text for title, subtitle, and description
    public void ShowTooltip(string title, string subtitle1, string subtitle2, string description)
    {
        //Necessary for grace period between shows/hides
        if(hideTooltipCoroutine != null)
        {
            StopCoroutine(hideTooltipCoroutine);
            hideTooltipCoroutine = null;

            tooltipCanvasGroup.alpha = 1;
        }

        //Set texts
        tooltipTitle.gameObject.SetActive(true);
        tooltipSubtitle1.gameObject.SetActive(true);
        tooltipSubtitle2.gameObject.SetActive(true);
        tooltipTitle.text = title;
        tooltipSubtitle1.text = subtitle1;
        tooltipSubtitle2.text = subtitle2;
        tooltipDescription.text = description;

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
    public void ShowTooltip(string simpleText)
    {
        //Necessary for grace period between shows/hides
        if(hideTooltipCoroutine != null)
        {
            StopCoroutine(hideTooltipCoroutine);
            hideTooltipCoroutine = null;

            tooltipCanvasGroup.alpha = 1;
        }

        //Set texts
        tooltipTitle.gameObject.SetActive(false);
        tooltipSubtitle1.gameObject.SetActive(false);
        tooltipSubtitle2.gameObject.SetActive(false);
        tooltipDescription.text = simpleText;

        //If we're going from object to object, don't have to delay or fade in/out
        if(tooltipParentRectTransform.gameObject.activeSelf)
        {
            return;
        }

        if(showTooltipCoroutine != null)
        {
            StopCoroutine(showTooltipCoroutine);
        }

        showTooltipCoroutine = StartCoroutine(ShowTooltipDelayed(0));
    }

    //Hides tooltip
    public void HideTooltip()
    {
        //Stop the tooltip delay, like a dash cancel
        if(showTooltipCoroutine != null)
        {
            StopCoroutine(showTooltipCoroutine);
        }

        hideTooltipCoroutine = StartCoroutine(HideTooltipDelayed());
    }

    //Coroutine to give a bit of grace period before hiding
    IEnumerator HideTooltipDelayed()
    {
        yield return new WaitForSeconds(tooltipHideDelay);

        yield return FadeTooltip(1f, 0f);

        tooltipParentRectTransform.gameObject.SetActive(false);
    }

    public static void ShowTooltip_Static(string title, string subtitle1, string subtitle2, string description)
    {
        instance.ShowTooltip(title, subtitle1, subtitle2, description);
    }

    public static void ShowTooltip_Static(string simpleText)
    {
        instance.ShowTooltip(simpleText);
    }

    public static void HideTooltip_Static()
    {
        instance.HideTooltip();
    }
}
