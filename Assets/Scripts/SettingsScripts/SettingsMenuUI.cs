using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;


public class SettingsMenuUI : MonoBehaviour
{
    //------------------------------------------------------------------------------------------

    [Header("Audio Controls")]
    public Slider masterVolumeSlider;
    public TMP_Text masterVolumeLabel;

    public Slider musicVolumeSlider;
    public TMP_Text musicVolumeLabel;

    public Slider sfxVolumeSlider;
    public TMP_Text sfxVolumeLabel;

    //------------------------------------------------------------------------------------------

    [Header("Input Controls")]
    public Slider sensitivitySlider;
    public TMP_Text sensitivityLabel;

    [Tooltip("Min/max values shown on the sensitivity slider")]
    public float sensitivityMin = 0.1f;
    public float sensitivityMax = 10f;

    //------------------------------------------------------------------------------------------

    [Header("Gameplay Controls")]
    public Toggle screenShakeToggle;
    public Toggle particlesToggle;

    //------------------------------------------------------------------------------------------

    [Header("Footer")]
    public Button resetButton;
    public Button closeButton;

    //------------------------------------------------------------------------------------------

    [Header("Panel Behaviour")]
    public InputActionAsset inputActions;
    private InputAction toggleSettingsAction;

    [Tooltip("Fade speed when showing / hiding the panel")]
    public float fadeSpeed = 8f;

    [Tooltip("Pause the game (Time.timeScale = 0) while settings are open")]
    public bool pauseWhileOpen = true;

    //------------------------------------------------------------------------------------------

    private CanvasGroup group;
    private bool isOpen = false;
    private bool isListening = false; //prevents feedback loops

    //------------------------------------------------------------------------------------------

    private void Awake()
    {
        //Start hidden
        SetGroupState(false, instant: true);

        //Setup input
        var map = inputActions.FindActionMap("UI");

        toggleSettingsAction = map.FindAction("OpenSettings");

        toggleSettingsAction.performed += _ => Toggle();
    }

    private void OnEnable()
    {
        SettingsManager.OnSettingsChanged += RefreshAllControls;
        toggleSettingsAction.Enable();
    }

    private void OnDisable()
    {
        SettingsManager.OnSettingsChanged -= RefreshAllControls;
        toggleSettingsAction.Disable();
    }

    private void Start()
    {
        ConfigureSliders();
        WireListeners();
        RefreshAllControls(); //Set control positions from saved values
    }

    private void Update()
    {
        //Smooth fade
        float target = isOpen ? 1f : 0f;
        group.alpha = Mathf.MoveTowards(group.alpha, target, fadeSpeed * Time.unscaledDeltaTime);
    }

    //------------------------------------------------------------------------------------------

    public void Open()
    {
        if(isOpen) return;
        isOpen = true;
        SetGroupState(true, instant: false);
        RefreshAllControls();
        if(pauseWhileOpen) Time.timeScale = 0f;
    }

    public void Close()
    {
        if(!isOpen) return;
        isOpen = false;
        SetGroupState(false, instant: false);
        if(pauseWhileOpen) Time.timeScale = 1f;
    }

    public void Toggle()
    {
        if(isOpen) Close(); else Open();
    }

    //------------------------------------------------------------------------------------------

    private void ConfigureSliders()
    {
        masterVolumeSlider.minValue = 0f;
        masterVolumeSlider.maxValue = 1f;
        musicVolumeSlider.minValue = 0f;
        musicVolumeSlider.maxValue = 1f;
        sfxVolumeSlider.minValue = 0f;
        sfxVolumeSlider.maxValue = 1f;

        sensitivitySlider.minValue = sensitivityMin;
        sensitivitySlider.maxValue = sensitivityMax;
    }

    private void WireListeners()
    {
        //Sliders
        masterVolumeSlider.onValueChanged.AddListener(v => {if(!isListening) SettingsManager.instance.MasterVolume = v;});

        musicVolumeSlider.onValueChanged.AddListener(v => {if(!isListening) SettingsManager.instance.MusicVolume = v;});

        sfxVolumeSlider.onValueChanged.AddListener(v => {if(!isListening) SettingsManager.instance.SFXVolume = v;});

        sensitivitySlider.onValueChanged.AddListener(v => {if(!isListening) SettingsManager.instance.MouseSensitivity = v;});

        //Toggles
        screenShakeToggle.onValueChanged.AddListener(v => {if(!isListening) SettingsManager.instance.ScreenShakeEnabled = v;});

        particlesToggle.onValueChanged.AddListener(v => {if(!isListening) SettingsManager.instance.ParticlesEnabled = v;});

        //Buttons
        resetButton.onClick.AddListener(OnResetClicked);
        closeButton.onClick.AddListener(Close);
    }

    //------------------------------------------------------------------------------------------

    private void RefreshAllControls()
    {
        if(SettingsManager.instance == null) return;

        //Prevent slider callbacks from writing back while we set them
        isListening = true;

        var s = SettingsManager.instance;

        SetSlider(masterVolumeSlider, s.MasterVolume);
        SetSlider(musicVolumeSlider, s.MusicVolume);
        SetSlider(sfxVolumeSlider, s.SFXVolume);
        SetSlider(sensitivitySlider, s.MouseSensitivity);

        SetToggle(screenShakeToggle, s.ScreenShakeEnabled);
        SetToggle(particlesToggle, s.ParticlesEnabled);

        isListening = false;

        //Update labels after values are set
        UpdateLabels();
    }

    private void UpdateLabels()
    {
        if(SettingsManager.instance == null) return;

        SetLabel(masterVolumeLabel, "Master Volume", SettingsManager.instance.MasterVolume, format: "percent");
        SetLabel(musicVolumeLabel, "Music Volume", SettingsManager.instance.MusicVolume, format: "percent");
        SetLabel(sfxVolumeLabel, "SFX Volume", SettingsManager.instance.SFXVolume, format: "percent");
        SetLabel(sensitivityLabel, "Sensitivity", SettingsManager.instance.MouseSensitivity, format: "multiplier");
    }

    //------------------------------------------------------------------------------------------

    private void OnResetClicked()
    {
        SettingsManager.instance?.ResetToDefaults();

        //RefreshAllControls is called automatically via OnSettingsChanged so no need to call it
    }

    //------------------------------------------------------------------------------------------

    //Helper methods

    private void SetGroupState(bool on, bool instant)
    {
        if(instant) group.alpha = on ? 1f : 0f;
        group.interactable   = on;
        group.blocksRaycasts = on;
    }

    private static void SetSlider(Slider s, float value)
    {
        if(s != null) s.SetValueWithoutNotify(value);
    }

    private static void SetToggle(Toggle t, bool value)
    {
        if(t != null) t.SetIsOnWithoutNotify(value);
    }

    private static void SetLabel(TMP_Text label, string name, float value, string format)
    {
        if(label == null) return;
        
        if(format == "percent")
        {
            label.text = $"{name}   {Mathf.RoundToInt(value * 100f)}%";
        }
        else
        {
            label.text = $"{name}   {value:0.0}x";
        }
    }
}
