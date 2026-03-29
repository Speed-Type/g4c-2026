using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

//App 1: EmailClient
//Read-only, has trivia questions for the player

/// Email client application.
///
/// ── ContentArea hierarchy ──────────────────────────────────────────────
///
///   ContentArea  (EmailClient.cs here)
///     ├── InboxPanel
///     │     └── EmailList  (VerticalLayoutGroup)  ← email rows spawn here
///     └── ReaderPanel
///           ├── SubjectLabel  (TMP_Text)
///           ├── FromLabel     (TMP_Text)
///           ├── BodyText      (TMP_Text, ScrollRect)
///           └── BackButton    (Button)
///

public class EmailClient : AppBase
{
    //Email datatypes

    [System.Serializable]
    public class Email
    {
        public Sprite profilePic;
        public string from = "unknown@example.com";
        public string subject = "(no subject)";
        [TextArea(3, 8)]
        public string body = "";
        [Tooltip("Objective ID to complete when this email is opened; Leave blank to skip")]
        public string completesObjective = "";
        [HideInInspector]
        public bool isRead = false;
    }

    //------------------------------------------------------------------------------------------

    [Header("Emails")]
    public List<Email> emails = new();

    [Header("Default State")]
    public CanvasGroup readerGroup;
    public GameObject hintText;

    [Header("Inbox")]
    public Transform emailList;
    public GameObject emailRowPrefab; //Button + TMP_Text (subject) + TMP_Text (from)

    [Header("Reader")]
    public TMP_Text subjectLabel;
    public TMP_Text fromLabel;
    public TMP_Text bodyText;
    public Image profilePic;

    //------------------------------------------------------------------------------------------

    public override void OnAppOpen(ComputerOS os)
    {
        base.OnAppOpen(os);
        BuildInbox();

        SetGroup(readerGroup, false);
        hintText.SetActive(true);
    }

    public override void OnAppClose()
    {
        
    }

    //------------------------------------------------------------------------------------------

    //Inbox logic
    //Also pretty much ripped straight from The FACTory

    private void BuildInbox()
    {
        if(emailList == null) return;

        foreach(Transform child in emailList) Destroy(child.gameObject);

        for(int i = 0; i < emails.Count; i++)
        {
            int idx = i; 
            Email mail = emails[i];

            GameObject row = Instantiate(emailRowPrefab, emailList);
            EmailRow emailRow = row.GetComponent<EmailRow>();

            //Profile pic
            if(mail.profilePic != null) emailRow.SetProfile(mail.profilePic);

            //Subject and from labels
            emailRow.SetSender(mail.from);
            emailRow.SetSubject(mail.subject);

            //Bold if unread
            if(mail.isRead)
            {
                emailRow.SetRead();
            }
            else
            {
                emailRow.SetUnread();
            }

            emailRow.AddButtonListener(() => OpenEmail(idx));
        }
    }

    private void OpenEmail(int index)
    {
        if(index < 0 || index >= emails.Count) return;

        //Show the right things
        SetGroup(readerGroup, true);
        hintText.SetActive(false);

        Email mail = emails[index];
        mail.isRead = true;

        subjectLabel.text = mail.subject;
        fromLabel.text = mail.from;
        bodyText.text = mail.body;
        profilePic.sprite = mail.profilePic;

        //Complete linked objective
        if(!string.IsNullOrEmpty(mail.completesObjective))
            ObjectiveManager.instance?.CompleteObjective(mail.completesObjective);

        BuildInbox(); //Refresh bold state
    }

    private void SetGroup(CanvasGroup g, bool on)
    {
        if(g == null) return;
        g.alpha = on ? 1f : 0f;
        g.interactable = on;
        g.blocksRaycasts = on;
    }
}