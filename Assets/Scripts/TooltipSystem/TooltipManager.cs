using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;
using System;

public class TooltipManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TextMeshProUGUI tooltipTitle;
    [SerializeField] private TextMeshProUGUI tooltipSubtitle1;
    [SerializeField] private TextMeshProUGUI tooltipSubtitle2;
    [SerializeField] private TextMeshProUGUI tooltipDescription;
    [SerializeField] private RectTransform tooltipBackground; //Background of the tooltip
    [SerializeField] private RectTransform tooltipParentTransform;

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
        //Make sure it doesn't go off screen (clamp it)
        Vector2 screenPoint = Mouse.current.position.ReadValue();

        float width = tooltipBackground.sizeDelta.x;
        float height = tooltipBackground.sizeDelta.y;

        screenPoint.x = Mathf.Min(screenPoint.x, Screen.width - width);
        screenPoint.y = Mathf.Min(screenPoint.y, Screen.height - height);

        tooltipParentTransform.position = screenPoint;
    }

    //Shows tooltip with set text for title, subtitle, and description
    public void ShowTooltip(string title, string subtitle1, string subtitle2, string description)
    {
        //Set texts
        tooltipTitle.gameObject.SetActive(true);
        tooltipSubtitle1.gameObject.SetActive(true);
        tooltipSubtitle2.gameObject.SetActive(true);
        tooltipTitle.text = title;
        tooltipSubtitle1.text = subtitle1;
        tooltipSubtitle2.text = subtitle2;
        tooltipDescription.text = description;

        tooltipParentTransform.gameObject.SetActive(true);
    }

    //Shows tooltip with simple text
    public void ShowTooltip(string simpleText)
    {
        //Set texts
        tooltipTitle.gameObject.SetActive(false);
        tooltipSubtitle1.gameObject.SetActive(false);
        tooltipSubtitle2.gameObject.SetActive(false);
        tooltipDescription.text = simpleText;

        tooltipParentTransform.gameObject.SetActive(true);
    }

    //Hides tooltip
    public void HideTooltip()
    {
        tooltipParentTransform.gameObject.SetActive(false);
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
