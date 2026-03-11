using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System.Collections;

public class DialogueBoxController : MonoBehaviour
{
    public TMP_Text textGraphic;
    public GameObject textBoxGO;
    public float typewriterEffectSpeed;
    private List<string> textBuffer;
    private Coroutine typewriterCoroutine;

    void Awake()
    {
        textBuffer = new List<string>();
        ContinueText();

        textBoxGO.SetActive(false);
    }

    //Method to continue to the next line of text in buffer
    public void ContinueText()
    {
        if(textGraphic.maxVisibleCharacters != textGraphic.text.Length)
        {
            //Finish current text typewriter effect
            textGraphic.maxVisibleCharacters = textGraphic.text.Length;
        }
        else if(textBuffer.Count > 0)
        {
            //Load new text
            textGraphic.text = textBuffer[0];
            textBuffer.RemoveAt(0);
            textGraphic.maxVisibleCharacters = 0;
            typewriterCoroutine = StartCoroutine(TypewriterCoroutine());
        }
        else if(textBuffer.Count == 0)
        {
            //Hide text box
            textBoxGO.SetActive(false);
        }
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
        textBoxGO.SetActive(true);
        AddText(newText);
        ContinueText();
    }
}
