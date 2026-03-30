using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;

public class WebBrowser : AppBase
{
    //Data struct for site options

    [System.Serializable]
    public class SiteOption
    {
        [Tooltip("Unique identifier")]
        public string id = "null";

        [Tooltip("Short label shown in the list row")]
        public string label = "Option";

        [TextArea(3, 8)]
        [Tooltip("Optional secondary detail line (shown smaller beneath the label)")]
        public string detail = "";

        [Tooltip("Fired immediately when this option is selected (single-click)")]
        public UnityEvent onSelect = new UnityEvent();

        [Tooltip("Fired when this option is selected AND the Confirm button is pressed")]
        public UnityEvent onConfirm = new UnityEvent();
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
    public GameObject optionRowPrefab;

    [Header("Colours")]
    public Color colourNormal = new Color(0.15f, 0.18f, 0.24f);
    public Color colourSelected = new Color(0.15f, 0.45f, 0.90f);

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

        //Load in today's volunteer opportunities from ConservationistUpgradeController
        options = ConservationistUpgradeController.instance.dailyVolunteerOptions;

        //Confirm button
        if(confirmButton != null)
        {
            confirmButton.onClick.AddListener(OnConfirmClicked);
            SetConfirmButtonState(false);
        }

        BuildOptionList();

        //Load in any saved selection
        if(ConservationistUpgradeController.instance.selectedVolunteeringUpgradeID != "null")
        {
            SelectOption(ConservationistUpgradeController.instance.selectedVolunteeringUpgradeID);
        }
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
        ApplySelection(index);
    }

    //Overload for select option
    public void SelectOption(string id)
    {
        for(int i = 0; i < options.Count; i++)
        {
            if(options[i].id == id)
            {
                ApplySelection(i, true);
                return;
            }
        }
    }

    //Deselect whatever is currently selected
    public void ClearSelection()
    {
        selectedIndex = -1;
        RefreshRowVisuals();
        UpdateSelectionInfo();
        SetConfirmButtonState(false);
    }

    //------------------------------------------------------------------------------------------

    private void BuildOptionList()
    {
        foreach(Transform child in optionList) Destroy(child.gameObject);
        rows.Clear();

        for(int i = 0; i < options.Count; i++)
        {
            int idx = i;

            GameObject row = Instantiate(optionRowPrefab, optionList);
            VolunteerOptionRow volunteerOptionRow = row.GetComponent<VolunteerOptionRow>();

            rows.Add(row);

            //Wire the button
            volunteerOptionRow.AddButtonListener(() =>
            {
                ApplySelection(idx);
            });

            ApplyRowVisual(row, i, selected: false);
        }
    }

    //------------------------------------------------------------------------------------------

    private void ApplySelection(int index, bool bypass = false)
    {   
        if(bypass == false && ConservationistUpgradeController.instance.selectedVolunteeringUpgradeID != "null") return;

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

        opt.onConfirm?.Invoke();

        ConservationistUpgradeController.instance.ConfirmVolunteering(opt.id);

        SetConfirmButtonState(true);
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
        volunteerOptionRow.SetBackgroundImageColor(selected ? colourSelected : colourNormal);

        //Label
        volunteerOptionRow.SetLabelText(opt.label, selected ? FontStyles.Bold : FontStyles.Normal);

        //Detail
        volunteerOptionRow.SetDetailText(opt.detail);

        //RadioIndicator
        volunteerOptionRow.SetRadioButton(selected);
    }

    private void UpdateSelectionInfo()
    {
        selectionInfoLabel.text = selectedIndex >= 0
            ? $"Selected: {options[selectedIndex].label}"
            : "No option selected.";
    }

    private void SetConfirmButtonState(bool ready)
    {
        if(ConservationistUpgradeController.instance.selectedVolunteeringUpgradeID != "null")
        {
            confirmButton.GetComponentInChildren<TMP_Text>().text = "Confirmed";
            return;
        }

        var lbl = confirmButton.GetComponentInChildren<TMP_Text>();
        if(lbl != null)
            lbl.text = ready ? confirmLabelReady : confirmLabelIdle;

        confirmButton.interactable = ready;
    }
}
