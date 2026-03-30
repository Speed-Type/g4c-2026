using UnityEngine;
using TMPro;
using System;
using UnityEngine.UI;

public class VolunteerOptionRow : MonoBehaviour
{
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image radioIndicator;
    [SerializeField] private TMP_Text labelText;
    [SerializeField] private TMP_Text detailText;
    public Button button;

    public void SetBackgroundImageColor(Color color)
    {
        backgroundImage.color = color;
    }

    public void SetRadioButtonColor(Color color)
    {
        
    }

    public void SetLabelText(string pText, Color pColor, FontStyles pFontStyle)
    {
        labelText.text = pText;
        labelText.color = pColor;
        labelText.fontStyle = pFontStyle;
    }

    public void SetDetailText(string pText, Color pColor)
    {
        detailText.text = pText;
        detailText.color = pColor;
    }

    public void AddButtonListener(Action action)
    {
        button.onClick.AddListener(() => {action();});
    }
}
