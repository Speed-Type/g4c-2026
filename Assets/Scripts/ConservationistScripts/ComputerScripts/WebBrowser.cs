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
        if(optionList == null) return;

        foreach(Transform child in optionList) Destroy(child.gameObject);
        rows.Clear();

        for (int i = 0; i < options.Count; i++)
        {
            int idx = i;   // capture
            GameObject row = optionRowPrefab != null
                ? Instantiate(optionRowPrefab, optionList)
                : BuildFallbackRow(optionList);

            rows.Add(row);

            // Wire the button
            var btn = row.GetComponent<Button>();
            if (btn == null) btn = row.AddComponent<Button>();

            btn.onClick.AddListener(() =>
            {
                if (!options[idx].disabled)
                    ApplySelection(idx);
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

        //Background colour
        var bg = row.GetComponent<Image>();
        if(bg != null)
        {
            bg.color = opt.disabled  ? colourDisabled
                     : selected      ? colourSelected
                     :                 colourNormal;
        }

        //Labels — find by name first, fall back to component order
        var allTexts = row.GetComponentsInChildren<TMP_Text>();

        TMP_Text labelText  = FindChildText(row, "OptionLabel")  ?? (allTexts.Length > 0 ? allTexts[0] : null);
        TMP_Text detailText = FindChildText(row, "OptionDetail") ?? (allTexts.Length > 1 ? allTexts[1] : null);

        Color textCol = opt.disabled ? colourTextDisabled : colourTextNormal;

        if (labelText != null)
        {
            labelText.text      = opt.label;
            labelText.color     = textCol;
            labelText.fontStyle = selected ? FontStyles.Bold : FontStyles.Normal;
        }

        if (detailText != null)
        {
            detailText.text    = opt.detail;
            detailText.color   = textCol * new Color(1, 1, 1, 0.7f);
            detailText.gameObject.SetActive(!string.IsNullOrEmpty(opt.detail));
        }

        // RadioIndicator — swap colour to show selected state
        var radio = FindChildImage(row, "RadioIndicator");
        if (radio != null)
            radio.color = selected && !opt.disabled ? colourSelected * 1.4f : colourNormal * 1.4f;

        // Button interactability
        var btn = row.GetComponent<Button>();
        if (btn != null)
        {
            var colors         = btn.colors;
            colors.disabledColor = colourTextDisabled;
            btn.colors         = colors;
            btn.interactable   = !opt.disabled;
        }
    }

    private void UpdateSelectionInfo()
    {
        if (selectionInfoLabel == null) return;

        selectionInfoLabel.text = selectedIndex >= 0
            ? $"Selected: {options[selectedIndex].label}"
            : "No option selected.";
    }

    private void SetConfirmButtonState(bool ready)
    {
        if (confirmButton == null) return;

        var lbl = confirmButton.GetComponentInChildren<TMP_Text>();
        if (lbl != null)
            lbl.text = ready ? confirmLabelReady : confirmLabelIdle;

        confirmButton.interactable = ready;
    }

    //------------------------------------------------------------------------------------------

    private GameObject BuildFallbackRow(Transform parent)
    {
        // Root
        var row = new GameObject("OptionRow", typeof(RectTransform));
        row.transform.SetParent(parent, false);

        var le = row.AddComponent<LayoutElement>();
        le.minHeight = 48f;

        var bg = row.AddComponent<Image>();
        bg.color = colourNormal;

        // Horizontal layout for radio + text column
        var hLayout = row.AddComponent<HorizontalLayoutGroup>();
        hLayout.padding       = new RectOffset(12, 12, 8, 8);
        hLayout.spacing       = 10f;
        hLayout.childAlignment           = TextAnchor.MiddleLeft;
        hLayout.childControlWidth        = false;
        hLayout.childControlHeight       = true;
        hLayout.childForceExpandWidth    = false;
        hLayout.childForceExpandHeight   = true;

        // Radio dot
        var radioGO  = new GameObject("RadioIndicator", typeof(RectTransform));
        radioGO.transform.SetParent(row.transform, false);
        var radioImg = radioGO.AddComponent<Image>();
        radioImg.color = colourNormal * 1.4f;
        var radioLE = radioGO.AddComponent<LayoutElement>();
        radioLE.minWidth  = 16f;
        radioLE.minHeight = 16f;
        radioLE.preferredWidth  = 16f;
        radioLE.preferredHeight = 16f;

        // Text column
        var textCol = new GameObject("TextColumn", typeof(RectTransform));
        textCol.transform.SetParent(row.transform, false);
        var vLayout = textCol.AddComponent<VerticalLayoutGroup>();
        vLayout.childControlHeight     = true;
        vLayout.childForceExpandHeight = false;
        vLayout.childAlignment         = TextAnchor.MiddleLeft;
        var textColLE = textCol.AddComponent<LayoutElement>();
        textColLE.flexibleWidth = 1f;

        // Label
        var labelGO  = new GameObject("OptionLabel", typeof(RectTransform));
        labelGO.transform.SetParent(textCol.transform, false);
        var label    = labelGO.AddComponent<TextMeshProUGUI>();
        label.fontSize = 14f;
        label.color    = colourTextNormal;

        // Detail
        var detailGO  = new GameObject("OptionDetail", typeof(RectTransform));
        detailGO.transform.SetParent(textCol.transform, false);
        var detail    = detailGO.AddComponent<TextMeshProUGUI>();
        detail.fontSize = 11f;
        detail.color    = colourTextNormal * new Color(1, 1, 1, 0.6f);

        return row;
    }

    //------------------------------------------------------------------------------------------

    //Helper methods

    private TMP_Text FindChildText(GameObject root, string childName)
    {
        var t = root.transform.Find(childName);
        return t != null ? t.GetComponent<TMP_Text>() : null;
    }

    private Image FindChildImage(GameObject root, string childName)
    {
        var t = root.transform.Find(childName);
        return t != null ? t.GetComponent<Image>() : null;
    }
}
