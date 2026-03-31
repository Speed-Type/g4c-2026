using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class EmailClient : AppBase
{
    //Email datatypes

    [System.Serializable]
    public class Email
    {
        public Sprite profilePic;
        public string from = "unknown@example.com";
        public string humanName = "";
        public string subject = "(no subject)";
        [TextArea(3, 8)]
        public string body = "";
        public string answerChoice1 = "";
        public string answerChoice2 = "";
        public string answerChoice3 = "";
        public string answerChoice4 = "";
        public AnswerChoice correctAnswer;
        [HideInInspector] public AnswerChoice selectedAnswer;
        [HideInInspector]
        public bool isAnswered = false;
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

    [Header("Answer Choice Labels")]
    public TMP_Text answerLabel1;
    public TMP_Text answerLabel2;
    public TMP_Text answerLabel3;
    public TMP_Text answerLabel4;

    [Header("Colors")]
    public Color defaultColor;
    public Color correctColor;
    public Color incorrectColor;

    private Email currentEmail;

    //------------------------------------------------------------------------------------------

    public override void OnAppOpen(ComputerOS os)
    {
        base.OnAppOpen(os);

        //Load in today's volunteer opportunities from ConservationistUpgradeController
        emails = ConservationistUpgradeController.instance.dailyEmails;

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

    //Builds the inbox list (of row objects)
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

            //Bold if unanswered
            if(mail.isAnswered)
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

        //Reset any previous selected answer choice's font
        if(currentEmail != null && currentEmail.isAnswered)
        {
            AnswerChoiceEnumToTMP(currentEmail.selectedAnswer).fontStyle = FontStyles.Normal;
            AnswerChoiceEnumToTMP(currentEmail.selectedAnswer).color = defaultColor;
            AnswerChoiceEnumToTMP(currentEmail.correctAnswer).color = defaultColor;
        }

        currentEmail = emails[index];

        subjectLabel.text = currentEmail.subject;
        fromLabel.text = currentEmail.humanName;
        bodyText.text = currentEmail.body;
        profilePic.sprite = currentEmail.profilePic;

        //Set question buttons' text
        answerLabel1.text = currentEmail.answerChoice1;
        answerLabel2.text = currentEmail.answerChoice2;
        answerLabel3.text = currentEmail.answerChoice3;
        answerLabel4.text = currentEmail.answerChoice4;

        //See if this question was right or not
        if(currentEmail.isAnswered)
        {
            //Bold the selected answer choice
            AnswerChoiceEnumToTMP(currentEmail.selectedAnswer).fontStyle = FontStyles.Bold;
            
            //Mark correct answer green
            AnswerChoiceEnumToTMP(currentEmail.correctAnswer).color = correctColor;

            //If the player chose something different, mark it red
            if(currentEmail.correctAnswer != currentEmail.selectedAnswer) AnswerChoiceEnumToTMP(currentEmail.selectedAnswer).color = incorrectColor;
        }
    }

    private void SetGroup(CanvasGroup g, bool on)
    {
        if(g == null) return;
        g.alpha = on ? 1f : 0f;
        g.interactable = on;
        g.blocksRaycasts = on;
    }

    public void AnswerQuestion(string answer)
    {
        AnswerChoice answerChoice;
        Enum.TryParse(answer, out answerChoice);

        if(currentEmail.isAnswered) return; //Do nothing if already answered

        currentEmail.isAnswered = true; //Set it answered
        currentEmail.selectedAnswer = answerChoice;

        //Check if correct
        if(answerChoice == currentEmail.correctAnswer)
        {
            //Correct
            ConservationistUpgradeController.instance.totalCorrectEmails++;
        }
        else
        {
            //Incorrect
        }

        BuildInbox(); //Refresh inbox
        OpenEmail(emails.IndexOf(currentEmail)); //Refresh email reader

        //Notify objective handler if all emails are answered
        foreach(Email email in emails)
        {
            if(!email.isAnswered) return;
        }

        ObjectiveManager.instance.CompleteObjective("checkEmails");
    }

    private TMP_Text AnswerChoiceEnumToTMP(AnswerChoice answerChoice)
    {
        switch(answerChoice)
        {
            case AnswerChoice.Answer1:
                return answerLabel1;
            case AnswerChoice.Answer2:
                return answerLabel2;
            case AnswerChoice.Answer3:
                return answerLabel3;
            case AnswerChoice.Answer4:
                return answerLabel4;
        }

        return null;
    }
}

public enum AnswerChoice
{
    Answer1,
    Answer2,
    Answer3,
    Answer4
}