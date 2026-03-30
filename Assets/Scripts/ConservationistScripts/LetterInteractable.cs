using UnityEngine;
using UnityEngine.UI;

public class LetterInteractable : MonoBehaviour
{
    public Button backButton;
    public GameObject letter;

    void Awake()
    {
        letter.SetActive(false);
        backButton.gameObject.SetActive(false);
    }

    public void ShowLetter()
    {
        letter.SetActive(true);
        backButton.gameObject.SetActive(true);

        backButton.onClick.AddListener(() => HideLetter());
    }

    public void HideLetter()
    {
        letter.SetActive(false);
        backButton.gameObject.SetActive(false);

        backButton.onClick.RemoveAllListeners();
    }
}
