using UnityEngine;
using TMPro;
using System;
using UnityEngine.UI;

public class FileRow : MonoBehaviour
{
    [SerializeField] private TMP_Text label;
    [SerializeField] private Image icon;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Button button;
    [SerializeField] private Sprite folderSprite;
    [SerializeField] private Sprite fileSprite;
    [SerializeField] private Color shade1;
    [SerializeField] private Color shade2;

    public void SetLabel(string pText)
    {
        label.text = pText;
    }

    public void SetIcon(bool isFolder)
    {
        icon.sprite = isFolder ? folderSprite : fileSprite;
    }

    public void SetShade(int parity)
    {
        backgroundImage.color = (parity % 2 == 0) ? shade1 : shade2;
    }

    public void AddButtonListener(Action action)
    {
        button.onClick.AddListener(() => {action();});
    }
}
