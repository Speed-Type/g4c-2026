using UnityEngine;

//Add to any GameObject with a ParticleSystem to have it automatically managed by the particles toggle in Settings

[RequireComponent(typeof(ParticleSystem))]
public class ManagedParticle : MonoBehaviour
{
    private ParticleSystem ps;

    private void Awake() => ps = GetComponent<ParticleSystem>();
    private void OnEnable()
    {
        ParticleManager.instance?.Register(ps);
        if (SettingsManager.instance != null)
            SetEmission(SettingsManager.instance.ParticlesEnabled);
    }

    private void OnDisable() => ParticleManager.instance?.Unregister(ps);

    private void SetEmission(bool enabled)
    {
        var emission = ps.emission;
        emission.enabled = enabled;
        if(!enabled) ps.Clear();
    }
}