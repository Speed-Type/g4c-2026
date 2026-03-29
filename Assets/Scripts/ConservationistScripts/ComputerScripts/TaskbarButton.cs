using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TaskbarButton : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text label;
    [SerializeField] private Button button;

    public void SetIcon(Sprite pSprite)
    {
        icon.sprite = pSprite;
    }
    
    public void SetLabel(string pText)
    {
        label.text = pText;
    }

    public void AddButtonListener(Action action)
    {
        button.onClick.AddListener(() => {action();});
    }
}
