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
    [SerializeField] private Image chinGraphic;
    [SerializeField] private GameObject raycastTargetGO;
    [SerializeField] private float typewriterEffectSpeed;
    [SerializeField] private float dialogueBoxCloseSpeed;

    //internal variables
    [SerializeField] private float shrunkBoxHeight;
    private float defaultBoxHeight;
    private List<string> textBuffer;
    private Coroutine typewriterCoroutine;
    private RectTransform textBoxRectTransform;

    void Awake()
    {
        //Initialize variables
        textBuffer = new List<string>();
        textBoxRectTransform = textBoxGO.GetComponent<RectTransform>();
        defaultBoxHeight = textBoxRectTransform.sizeDelta.y;

        ContinueText();

        HideDialogueBox();
    }

    //Method to continue to the next line of text in buffer
    public void ContinueText()
    {
        if(textGraphic.maxVisibleCharacters != textGraphic.text.Length)
        {
            //Finish current text typewriter effect
            textGraphic.maxVisibleCharacters = textGraphic.text.Length;
        }
        else if(textBuffer.Count >= 0)
        {
            //Load new text
            textGraphic.text = textBuffer[0];
            textBuffer.RemoveAt(0);
            textGraphic.maxVisibleCharacters = 0;
            typewriterCoroutine = StartCoroutine(TypewriterCoroutine());

            if(textBuffer.Count == 0)
            {
                //Hide text box
                LeanTween.value(this.gameObject, SetDialogueBoxHeight, defaultBoxHeight, shrunkBoxHeight, dialogueBoxCloseSpeed).setEaseOutElastic();
                
                raycastTargetGO.SetActive(false); //make it so it can't be interacted with anymore

                //HERE, CALL THE METHOD TO ENABLE PLAYER CHOICES AGAIN
                rgController.EnablePlayerChoices();
            }
        }
    }

    private void SetDialogueBoxHeight(float newHeight)
    {
        textBoxRectTransform.sizeDelta = new Vector2(textBoxRectTransform.sizeDelta.x, newHeight);
    }

    public void HideDialogueBox()
    {
        textBoxGO.GetComponent<Image>().enabled = false;
        textGraphic.enabled = false;
        chinGraphic.enabled = false;
        raycastTargetGO.SetActive(false);
    }

    public void ShowDialogueBox()
    {
        textBoxGO.GetComponent<Image>().enabled = true;
        textGraphic.enabled = true;
        chinGraphic.enabled = true;
        raycastTargetGO.SetActive(true);
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
        SetDialogueBoxHeight(defaultBoxHeight); //reset sizing
        AddText(newText);
        ContinueText();
    }
}
