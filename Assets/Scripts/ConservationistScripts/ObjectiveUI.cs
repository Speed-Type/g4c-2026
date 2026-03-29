using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Manages the on-screen objective list panel.
///
/// Hierarchy expected:
///   ObjectivePanel (this script)
///     ├── Header (TMP_Text)           — optional title
///     ├── MandatorySection
///     │     ├── SectionLabel (TMP_Text)
///     │     └── MandatoryContainer (VerticalLayoutGroup)
///     └── OptionalSection
///           ├── SectionLabel (TMP_Text)
///           └── OptionalContainer (VerticalLayoutGroup)
///
/// Assign ObjectiveRowPrefab — a prefab with:
///   ├── CheckIcon  (Image)     — shown when complete
///   ├── BulletIcon (Image)     — shown when incomplete
///   └── Label      (TMP_Text)  — objective description
/// </summary>
public class ObjectiveUI : MonoBehaviour
{
    //Static instance
    public static ObjectiveUI instance {get; private set;}

    //------------------------------------------------------------------------------------------

    [Header("Containers")]
    public Transform mandatoryContainer;
    public Transform optionalContainer;

    [Header("Prefab")]
    public GameObject objectiveRowPrefab;

    [Header("Styling")]
    public Color colorIncomplete = new Color(0.9f, 0.9f, 0.9f);
    public Color colorComplete = new Color(0.4f, 0.9f, 0.4f);
    public Color colorOptional = new Color(0.7f, 0.7f, 1.0f);

    [Header("Animation")]
    [Tooltip("How fast the panel slides in/out (CanvasGroup alpha fade)")]
    public float fadeSpeed = 4f;

    //State
    private CanvasGroup canvasGroup;
    private bool thisVisible = true;

    //------------------------------------------------------------------------------------------

    void Awake()
    {
        instance = this;

        canvasGroup = GetComponent<CanvasGroup>();
    }

    private void Update()
    {
        //Smooth fade
        float targetAlpha = thisVisible ? 1f : 0f;
        canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, targetAlpha, fadeSpeed * Time.deltaTime);
    }

    //------------------------------------------------------------------------------------------

    //Rebuilds the entire objective list from the current data
    public void Refresh(List<ObjectiveManager.Objective> objectives)
    {
        ClearContainer(mandatoryContainer);
        ClearContainer(optionalContainer);

        foreach(var obj in objectives)
        {
            Transform container = obj.isMandatory ? mandatoryContainer : optionalContainer;
            if(container == null) continue;

            CreateRow(obj, container);
        }

        //Hide optional section header if no optional objectives exist
        if(optionalContainer != null)
        {
            bool hasOptional = objectives.Exists(o => !o.isMandatory);
            var section = optionalContainer.parent?.gameObject;
            if(section != null) section.SetActive(hasOptional);
        }
    }

    public void ToggleVisibility()
    {
        thisVisible = !thisVisible;
        canvasGroup.interactable = thisVisible;
        canvasGroup.blocksRaycasts = thisVisible;
    }

    public void Show() {thisVisible = true; canvasGroup.interactable = canvasGroup.blocksRaycasts = true;}
    public void Hide() {thisVisible = false; canvasGroup.interactable = canvasGroup.blocksRaycasts = false;}

    //------------------------------------------------------------------------------------------

    //Helper methods

    private void ClearContainer(Transform container)
    {
        if(container == null) return;
        foreach(Transform child in container)
            Destroy(child.gameObject);
    }

    private void CreateRow(ObjectiveManager.Objective obj, Transform container)
    {
        GameObject row = Instantiate(objectiveRowPrefab, container);
        var label = row.GetComponentInChildren<TMP_Text>();
        var checkIcon = row.transform.Find("CheckIcon")?.GetComponent<Image>();
        var bulletIcon = row.transform.Find("BulletIcon")?.GetComponent<Image>();

        if(label != null)
        {
            label.text = obj.description;
            label.color = obj.isComplete ? colorComplete : (obj.isMandatory ? colorIncomplete : colorOptional);

            if(!obj.isMandatory) label.fontStyle |= FontStyles.Italic;

            if(obj.isComplete)
                label.fontStyle |= FontStyles.Strikethrough;
            else
                label.fontStyle &= ~FontStyles.Underline;
        }

        checkIcon.gameObject.SetActive(obj.isComplete);
        bulletIcon.gameObject.SetActive(!obj.isComplete);
    }
}