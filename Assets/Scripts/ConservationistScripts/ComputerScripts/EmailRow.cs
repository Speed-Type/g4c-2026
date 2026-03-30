using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EmailRow : MonoBehaviour
{
    [SerializeField] private TMP_Text subjectText;
    [SerializeField] private TMP_Text senderText;
    [SerializeField] private Image profileImage;
    [SerializeField] private Button button;

    public void SetSubject(string pText)
    {
        subjectText.text = pText;
    }

    public void SetSender(string pText)
    {
        senderText.text = pText;
    }
    
    public void SetProfile(Sprite pSprite)
    {
        profileImage.sprite = pSprite;
    }

    public void AddButtonListener(Action action)
    {
        button.onClick.AddListener(() => {action();});
    }

    public void SetRead()
    {
        subjectText.color = Color.black;
        subjectText.fontStyle = FontStyles.Normal;
        senderText.fontStyle = FontStyles.Normal;
    }

    public void SetUnread()
    {
        subjectText.color = new Color(0.09229262f, 0.233911f, 0.3207547f);
        subjectText.fontStyle = FontStyles.Bold;
        senderText.fontStyle = FontStyles.Bold;
    }
}
