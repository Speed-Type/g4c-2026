using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;

// ══════════════════════════════════════════════════════════════════════════
//  WebBrowser — AppBase subclass
//
//  A single-page "website" rendered inside an app window.
//  The page contains a scrollable list of options; exactly one can be
//  selected at a time (radio-button style).  Selecting an option fires
//  that option's UnityEvent and optionally completes an objective.
//  A "Submit / Confirm" button fires a second event on the currently
//  selected option, useful for two-step confirm flows.
//
//  ── ContentArea hierarchy ──────────────────────────────────────────────
//
//    ContentArea  (WebBrowser.cs here)
//      ├── PageHeader
//      │     ├── SiteTitle     (TMP_Text)   ← set from Inspector
//      │     └── SiteSubtitle  (TMP_Text)   ← set from Inspector (optional)
//      ├── OptionScrollView    (ScrollRect)
//      │     └── Viewport
//      │           └── OptionList  (VerticalLayoutGroup)  ← rows spawn here
//      ├── SelectionInfo       (TMP_Text)   ← shows current selection label
//      └── ConfirmButton       (Button)     ← optional two-step confirm
//
//  ── OptionRow prefab ───────────────────────────────────────────────────
//
//    OptionRow  (Button + CanvasGroup)
//      ├── RadioIndicator  (Image)   ← swap sprite or colour to show selected
//      ├── OptionLabel     (TMP_Text)
//      └── OptionDetail    (TMP_Text, optional sub-label)
//
// ══════════════════════════════════════════════════════════════════════════

public class WebBrowser : AppBase
{
    //Data struct for site options

    [System.Serializable]
    public class SiteOption
    {
        [Tooltip("Short label shown in the list row")]
        public string label = "Option";

        [Tooltip("Optional secondary detail line (shown smaller beneath the label)")]
        public string detail = "";

        [Tooltip("Fired immediately when this option is selected (single-click).")]
        public UnityEvent onSelect = new UnityEvent();

        [Tooltip("Fired when this option is selected AND the Confirm button is pressed.")]
        public UnityEvent onConfirm = new UnityEvent();

        [Tooltip("Objective ID completed on confirm. Leave blank to skip.")]
        public string completesObjectiveOnConfirm = "";

        [Tooltip("If true this option appears greyed-out and cannot be selected.")]
        public bool disabled = false;
    }

    //------------------------------------------------------------------------------------------

    [Header("Site Content")]
    [Tooltip("The list of selectable options on the page")]
    public List<SiteOption> options = new();

    [Header("UI References")]
    public Transform optionList; //VerticalLayoutGroup parent
    public TMP_Text selectionInfoLabel;
    public Button confirmButton;

    [Header("Option Row Prefab")]
    [Tooltip("Leave blank to use a built-in fallback row (fine for prototyping)")]
    public GameObject optionRowPrefab;

    [Header("Colours")]
    public Color colourNormal = new Color(0.15f, 0.18f, 0.24f);
    public Color colourSelected = new Color(0.15f, 0.45f, 0.90f);
    public Color colourDisabled = new Color(0.25f, 0.25f, 0.28f);
    public Color colourTextNormal = Color.white;
    public Color colourTextDisabled = new Color(0.5f, 0.5f, 0.5f);

    [Header("Confirm Button")]
    [Tooltip("Text on the confirm button when nothing is selected")]
    public string confirmLabelIdle = "Select an option";
    [Tooltip("Text on the confirm button when an option is selected")]
    public string confirmLabelReady = "Confirm Selection";

    //------------------------------------------------------------------------------------------

    private int selectedIndex = -1; //-1 = nothing selected
    private List<GameObject> rows = new();

    //------------------------------------------------------------------------------------------

    public override void OnAppOpen(ComputerOS os)
    {
        base.OnAppOpen(os);

        //Confirm button
        if(confirmButton != null)
        {
            confirmButton.onClick.AddListener(OnConfirmClicked);
            SetConfirmButtonState(false);
        }

        BuildOptionList();
    }

    public override void OnAppClose()
    {
        if(confirmButton != null)
            confirmButton.onClick.RemoveListener(OnConfirmClicked);

        selectedIndex = -1;
    }

    //------------------------------------------------------------------------------------------

    //Select an option by index from code (in case we need that from another event)
    public void SelectOption(int index)
    {
        if(index < 0 || index >= options.Count) return;
        if(options[index].disabled) return;
        ApplySelection(index);
    }

    //Deselect whatever is currently selected
    public void ClearSelection()
    {
        selectedIndex = -1;
        RefreshRowVisuals();
        UpdateSelectionInfo();
        SetConfirmButtonState(false);
    }

    //Enable or disable a specific option at runtime
    public void SetOptionDisabled(int index, bool disabled)
    {
        if(index < 0 || index >= options.Count) return;
        options[index].disabled = disabled;

        if(disabled && selectedIndex == index)
            ClearSelection();

        RefreshRowVisuals();
    }

    //------------------------------------------------------------------------------------------

    private void BuildOptionList()
    {
        foreach(Transform child in optionList) Destroy(child.gameObject);
        rows.Clear();

        for (int i = 0; i < options.Count; i++)
        {
            GameObject row = Instantiate(optionRowPrefab, optionList);
            VolunteerOptionRow volunteerOptionRow = row.GetComponent<VolunteerOptionRow>();

            rows.Add(row);

            //Wire the button
            volunteerOptionRow.AddButtonListener(() =>
            {
                if (!options[i].disabled)
                    ApplySelection(i);
            });

            ApplyRowVisual(row, i, selected: false);
        }
    }

    //------------------------------------------------------------------------------------------

    private void ApplySelection(int index)
    {
        selectedIndex = index;

        RefreshRowVisuals();
        UpdateSelectionInfo();
        SetConfirmButtonState(true);

        //Fire onSelect event + objective
        var opt = options[index];
        opt.onSelect?.Invoke();
    }

    private void OnConfirmClicked()
    {
        if(selectedIndex < 0 || selectedIndex >= options.Count) return;

        var opt = options[selectedIndex];
        if (opt.disabled) return;

        opt.onConfirm?.Invoke();

        if(!string.IsNullOrEmpty(opt.completesObjectiveOnConfirm))
            ObjectiveManager.instance.CompleteObjective(opt.completesObjectiveOnConfirm);
    }

    //------------------------------------------------------------------------------------------

    private void RefreshRowVisuals()
    {
        for(int i = 0; i < rows.Count; i++)
            ApplyRowVisual(rows[i], i, selected: i == selectedIndex);
    }

    private void ApplyRowVisual(GameObject row, int index, bool selected)
    {
        var opt = options[index];
        VolunteerOptionRow volunteerOptionRow = row.GetComponent<VolunteerOptionRow>();

        //Background colour
        volunteerOptionRow.SetBackgroundImageColor(opt.disabled ? colourDisabled : selected ? colourSelected : colourNormal);

        //Label
        volunteerOptionRow.SetLabelText(opt.label, opt.disabled ? colourTextDisabled : colourTextNormal, selected ? FontStyles.Bold : FontStyles.Normal);

        //Detail
        volunteerOptionRow.SetDetailText(opt.detail, opt.disabled ? colourTextDisabled : colourTextNormal * new Color(1, 1, 1, 0.7f));

        //RadioIndicator
        volunteerOptionRow.SetRadioButtonColor(selected && !opt.disabled ? colourSelected * 1.4f : colourNormal * 1.4f);

        //Button interactability
        var colors = volunteerOptionRow.button.colors;
        colors.disabledColor = colourTextDisabled;
        volunteerOptionRow.button.colors = colors;
        volunteerOptionRow.button.interactable = !opt.disabled;
    }

    private void UpdateSelectionInfo()
    {
        selectionInfoLabel.text = selectedIndex >= 0
            ? $"Selected: {options[selectedIndex].label}"
            : "No option selected.";
    }

    private void SetConfirmButtonState(bool ready)
    {
        var lbl = confirmButton.GetComponentInChildren<TMP_Text>();
        if(lbl != null)
            lbl.text = ready ? confirmLabelReady : confirmLabelIdle;

        confirmButton.interactable = ready;
    }
}
