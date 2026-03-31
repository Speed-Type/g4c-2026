using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

//Simple tutorial popup system
//Pauses time while open, resumes on close
///
/// ── Hierarchy ─────────────────────────────────────────────────────────
///
///   TutorialPopup  (CanvasGroup + TutorialPopup.cs)
///     ├── Panel        (Image — background)
///     │     ├── TitleText   (TMP_Text)
///     │     ├── BodyText    (TMP_Text)
///     │     └── CloseButton (Button)
///     └── Overlay      (optional full-screen dark Image behind the panel)
///
/// ── Usage from any script ─────────────────────────────────────────────
///
///   TutorialPopup.Show("Title", "Body text here.");
///   TutorialPopup.Hide();

public class TutorialPopup : MonoBehaviour
{
    public static TutorialPopup instance {get; private set;}

    [Header("References")]
    public TMP_Text titleText;
    public TMP_Text bodyText;
    public Button closeButton;

    [Header("Settings")]
    public float fadeSpeed = 8f;
    public bool pauseTime = true;

    [Header("SettingsMenuUI to Kill")]
    public GameObject settingsMenuUI;

    private CanvasGroup group;
    private bool isOpen = false;

    //------------------------------------------------------------------------------------------

    private void Awake()
    {
        if(instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;

        group = GetComponent<CanvasGroup>();
        if(group == null) group = gameObject.AddComponent<CanvasGroup>();

        SetGroupState(false, instant: true);

        if(closeButton != null)
            closeButton.onClick.AddListener(Hide);
    }

    private void Update()
    {
        float target = isOpen ? 1f : 0f;
        group.alpha = Mathf.MoveTowards(group.alpha, target, fadeSpeed * Time.unscaledDeltaTime);
    }

    //------------------------------------------------------------------------------------------

    public static void Show(string title, string body)
        => instance?.ShowPopup(title, body);

    public static void Hide()
        => instance?.HidePopup();

    //------------------------------------------------------------------------------------------

    public void ShowPopup(string title, string body)
    {
        if(titleText != null) titleText.text = title;
        if(bodyText  != null) bodyText.text  = body;

        settingsMenuUI.SetActive(false);

        SetGroupState(true, instant: false);
        isOpen = true;

        if(pauseTime) Time.timeScale = 0f;
    }

    public void HidePopup()
    {
        SetGroupState(false, instant: false);
        isOpen = false;

        settingsMenuUI.SetActive(true);

        if(pauseTime) Time.timeScale = 1f;
    }

    //------------------------------------------------------------------------------------------

    private void SetGroupState(bool on, bool instant)
    {
        if(instant) group.alpha = on ? 1f : 0f;
        group.interactable   = on;
        group.blocksRaycasts = on;
    }
}