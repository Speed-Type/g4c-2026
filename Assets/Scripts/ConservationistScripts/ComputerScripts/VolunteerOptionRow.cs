using UnityEngine;
using TMPro;
using System;
using UnityEngine.UI;

public class VolunteerOptionRow : MonoBehaviour
{
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image radioIndicator;
    [SerializeField] private Sprite selectedRadioSprite;
    [SerializeField] private Sprite deselectedRadioSprite;
    [SerializeField] private TMP_Text labelText;
    [SerializeField] private TMP_Text detailText;
    public Button button;

    public void SetBackgroundImageColor(Color color)
    {
        backgroundImage.color = color;
    }

    public void SetRadioButton(bool selected)
    {
        radioIndicator.sprite = selected ? selectedRadioSprite : deselectedRadioSprite;
    }

    public void SetLabelText(string pText, FontStyles pFontStyle)
    {
        labelText.text = pText;
        labelText.fontStyle = pFontStyle;
    }

    public void SetDetailText(string pText)
    {
        detailText.text = pText;
    }

    public void AddButtonListener(Action action)
    {
        button.onClick.AddListener(() => {action();});
    }
}
