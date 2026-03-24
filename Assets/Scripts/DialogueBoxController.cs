using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.UI;

public class DialogueBoxController : MonoBehaviour
{
    //Public variables
    [SerializeField] private RoguelikeGameplayController rgController;
    [SerializeField] private TMP_Text textGraphic;
    [SerializeField] private GameObject textBoxGO;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Image chinGraphic;
    [SerializeField] private GameObject continueRaycastTargetGO;
    [SerializeField] private GameObject alertnessBarRaycastTargetGO;
    [SerializeField] private float typewriterEffectSpeed;
    [SerializeField] private float dialogueBoxCloseSpeed;
    [SerializeField] private float shrunkBoxHeight;

    [Header("Spider Alertness Bar")]
    [SerializeField] private RectTransform alertnessFillBar;
    [SerializeField] private Color alertnessBarFlashColor;
    [SerializeField] private Image spiderImage;
    [SerializeField] private Sprite[] spiderSprites;

    //Internal Variables
    private float defaultBoxHeight;
    private List<string> textBuffer;
    private Coroutine typewriterCoroutine;
    private RectTransform textBoxRectTransform;
    private float alertnessBarMaxWidth;
    private Color alertnessBarOriginalColor;

    void Awake()
    {
        //Initialize variables
        textBuffer = new List<string>();
        textBoxRectTransform = textBoxGO.GetComponent<RectTransform>();
        defaultBoxHeight = textBoxRectTransform.sizeDelta.y;
        alertnessBarMaxWidth = alertnessFillBar.sizeDelta.x;
        alertnessBarOriginalColor = alertnessFillBar.gameObject.GetComponent<Image>().color;

        ContinueText();

        HideDialogueBox();
    }

    //Method to continue to the next line of text in buffer
    public void ContinueText()
    {
        if(textGraphic.maxVisibleCharacters != textGraphic.text.Length) //CASE 1: Not yet done typing text
        {
            //Finish current text typewriter effect
            textGraphic.maxVisibleCharacters = textGraphic.text.Length;
        }
        else if(textBuffer.Count == 1) //CASE 2: Last text to show
        {
            //Load new text
            textGraphic.text = textBuffer[0];
            textBuffer.RemoveAt(0);
            textGraphic.maxVisibleCharacters = 0;
            typewriterCoroutine = StartCoroutine(TypewriterCoroutine());

            if(!rgController.playerCanMakeChoices)
            {
                //Hide text box
                LeanTween.value(this.gameObject, SetDialogueBoxHeight, defaultBoxHeight, shrunkBoxHeight, dialogueBoxCloseSpeed).setEaseOutElastic();
                
                //HERE, CALL THE METHOD TO ENABLE PLAYER CHOICES AGAIN
                rgController.EnablePlayerChoices();
            }
        }
        else if(textBuffer.Count > 0) //CASE 3: Not the last text to show
        {
            //Load new text
            textGraphic.text = textBuffer[0];
            textBuffer.RemoveAt(0);
            textGraphic.maxVisibleCharacters = 0;
            typewriterCoroutine = StartCoroutine(TypewriterCoroutine());
        }
    }

    private void SetDialogueBoxHeight(float newHeight)
    {
        textBoxRectTransform.sizeDelta = new Vector2(textBoxRectTransform.sizeDelta.x, newHeight);
    }

    public void HideDialogueBox()
    {
        textBoxGO.GetComponent<Image>().enabled = false;
        canvasGroup.alpha = 0;
        chinGraphic.enabled = false;
        continueRaycastTargetGO.SetActive(false);
        alertnessBarRaycastTargetGO.SetActive(false);
    }

    public void ShowDialogueBox()
    {
        textBoxGO.GetComponent<Image>().enabled = true;
        canvasGroup.alpha = 1;
        chinGraphic.enabled = true;
        continueRaycastTargetGO.SetActive(true);
        alertnessBarRaycastTargetGO.SetActive(true);

        //Also update spider alertness bar
        float percentToFill = RoguelikeGameplayController.ChanceForSpiderToPursue();
        StatDisplay.UpdateFillBar(alertnessFillBar, percentToFill, alertnessBarMaxWidth, alertnessBarFlashColor, alertnessBarOriginalColor);
    
        //Also update spider image
        spiderImage.sprite = spiderSprites[(int)(percentToFill * (spiderSprites.Length - 1))];
    }

    IEnumerator TypewriterCoroutine()
    {
        if(textGraphic.maxVisibleCharacters != textGraphic.text.Length)
        {
            textGraphic.maxVisibleCharacters++;
            yield return new WaitForSeconds(typewriterEffectSpeed);
            typewriterCoroutine = StartCoroutine(TypewriterCoroutine());
        }
    }

    //Method to add strings to text buffer
    public void AddText(List<string> newText)
    {
        foreach(string str in newText)
        {
            textBuffer.Add(str);
        }
    }

    //Method to set the strings for the text buffer (CLEARS BUFFER)
    public void SetText(List<string> newText)
    {
        textBuffer.Clear();
        if(!rgController.playerCanMakeChoices) SetDialogueBoxHeight(defaultBoxHeight); //reset sizing
        AddText(newText);
        ContinueText();
    }
}
