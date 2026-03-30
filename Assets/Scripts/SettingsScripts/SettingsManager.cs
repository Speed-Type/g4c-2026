using UnityEngine;
using UnityEngine.Audio;

//Ripped straight from other games; thanks Kevin

public class SettingsManager : MonoBehaviour
{
    //Static instance
    public static SettingsManager instance {get; private set;}

    //------------------------------------------------------------------------------------------
    
    public static event System.Action OnSettingsChanged;

    //------------------------------------------------------------------------------------------

    [Header("Audio")]
    [Tooltip("Should be the master AudioMixer")]
    public AudioMixer audioMixer;

    [Tooltip("Exposed parameter name on the mixer for master volume")]
    public string mixerMasterParam = "MasterVolume";

    [Tooltip("Exposed parameter name on the mixer for music volume")]
    public string mixerMusicParam = "MusicVolume";

    [Tooltip("Exposed parameter name on the mixer for SFX volume")]
    public string mixerSFXParam = "SFXVolume";

    //------------------------------------------------------------------------------------------

    [Header("Defaults (used on first launch)")]
    [Range(0f, 1f)] public float defaultMasterVolume = 1f;
    [Range(0f, 1f)] public float defaultMusicVolume  = 0.8f;
    [Range(0f, 1f)] public float defaultSFXVolume    = 1f;
    [Range(0.1f, 10f)] public float defaultMouseSensitivity = 1f;
    public bool defaultScreenShake = true;
    public bool defaultParticles = true;

    //------------------------------------------------------------------------------------------

    private const string K_MASTER  = "s_MasterVol";
    private const string K_MUSIC = "s_MusicVol";
    private const string K_SFX = "s_SFXVol";
    private const string K_SENS = "s_MouseSens";
    private const string K_SHAKE = "s_ScreenShake";
    private const string K_PARTS = "s_Particles";

    //------------------------------------------------------------------------------------------

    private float masterVolume;
    private float musicVolume;
    private float sfxVolume;
    private float mouseSensitivity;
    private bool  screenShakeEnabled;
    private bool  particlesEnabled;

    //------------------------------------------------------------------------------------------

    public float MasterVolume
    {
        get => masterVolume;
        set {masterVolume = Mathf.Clamp01(value); ApplyAudio(); Save(); Broadcast();}
    }

    public float MusicVolume
    {
        get => musicVolume;
        set {musicVolume = Mathf.Clamp01(value); ApplyAudio(); Save(); Broadcast();}
    }

    public float SFXVolume
    {
        get => sfxVolume;
        set {sfxVolume = Mathf.Clamp01(value); ApplyAudio(); Save(); Broadcast();}
    }

    public float MouseSensitivity
    {
        get => mouseSensitivity;
        set {mouseSensitivity = Mathf.Clamp(value, 0.1f, 10f); Save(); Broadcast();}
    }

    public bool ScreenShakeEnabled
    {
        get => screenShakeEnabled;
        set {screenShakeEnabled = value; Save(); Broadcast();}
    }

    public bool ParticlesEnabled
    {
        get => particlesEnabled;
        set {particlesEnabled = value; ApplyParticles(); Save(); Broadcast();}
    }

    //------------------------------------------------------------------------------------------

    private void Awake()
    {
        if(instance != null && instance != this) {Destroy(gameObject); return;}
        instance = this;
        DontDestroyOnLoad(gameObject);

        Load();
        ApplyAudio();
        ApplyParticles();
    }

    //------------------------------------------------------------------------------------------

    private void Load()
    {
        masterVolume = PlayerPrefs.GetFloat(K_MASTER, defaultMasterVolume);
        musicVolume = PlayerPrefs.GetFloat(K_MUSIC, defaultMusicVolume);
        sfxVolume = PlayerPrefs.GetFloat(K_SFX, defaultSFXVolume);
        mouseSensitivity = PlayerPrefs.GetFloat(K_SENS, defaultMouseSensitivity);
        screenShakeEnabled = PlayerPrefs.GetInt(K_SHAKE, defaultScreenShake ? 1 : 0) == 1;
        particlesEnabled = PlayerPrefs.GetInt(K_PARTS, defaultParticles  ? 1 : 0) == 1;
    }

    private void Save()
    {
        PlayerPrefs.SetFloat(K_MASTER, masterVolume);
        PlayerPrefs.SetFloat(K_MUSIC, musicVolume);
        PlayerPrefs.SetFloat(K_SFX, sfxVolume);
        PlayerPrefs.SetFloat(K_SENS, mouseSensitivity);
        PlayerPrefs.SetInt(K_SHAKE, screenShakeEnabled ? 1 : 0);
        PlayerPrefs.SetInt(K_PARTS, particlesEnabled  ? 1 : 0);
        PlayerPrefs.Save();
    }

    //------------------------------------------------------------------------------------------

    private void ApplyAudio()
    {
        if(audioMixer == null) return;

        //AudioMixer volumes are in dB, so logarithmic crap
        //Silence at -80 dB, full at 0 dB
        audioMixer.SetFloat(mixerMasterParam, LinearToDecibel(masterVolume));
        audioMixer.SetFloat(mixerMusicParam, LinearToDecibel(musicVolume));
        audioMixer.SetFloat(mixerSFXParam, LinearToDecibel(sfxVolume));
    }

    private void ApplyParticles()
    {
        //Calls particle manager to find all ParticleSystems in the scene and enable/disable emission
        
        if(ParticleManager.instance != null)
        {
            ParticleManager.instance.SetParticlesEnabled(particlesEnabled);
        }
    }

    private void Broadcast() => OnSettingsChanged?.Invoke();

    //------------------------------------------------------------------------------------------

    public void ResetToDefaults()
    {
        masterVolume = defaultMasterVolume;
        musicVolume = defaultMusicVolume;
        sfxVolume = defaultSFXVolume;
        mouseSensitivity = defaultMouseSensitivity;
        screenShakeEnabled = defaultScreenShake;
        particlesEnabled = defaultParticles;

        ApplyAudio();
        ApplyParticles();
        Save();
        Broadcast();
    }

    //No I just love math no really I do
    private static float LinearToDecibel(float linear)
    {
        return linear > 0.0001f ? Mathf.Log10(linear) * 20f : -80f;
    }
}
