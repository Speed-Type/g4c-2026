using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class AppWindow : MonoBehaviour
{
    [SerializeField] private TMP_Text label;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button titleBarButton;
    
    public void SetLabel(string pText)
    {
        label.text = pText;
    }

    public void AddCloseButtonListener(Action action)
    {
        closeButton.onClick.AddListener(() => {action();});
    }

    public void AddTitleBarButtonListener(Action action)
    {
        titleBarButton.onClick.AddListener(() => {action();});
    }
}
