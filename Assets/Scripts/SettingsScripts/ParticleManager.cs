using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Tracks all ParticleSystems in the scene and enables / disables their emission module when SettingsManager.ParticlesEnabled changes

//If you want a particle system to respect this toggle, either:
//1. have it call ParticleManager.Instance.Register(particlesystem) on its Awake
//2. set its GameObject tag to "ManagedParticle" and this manager will auto-discover it on Start

public class ParticleManager : MonoBehaviour
{
    public static ParticleManager instance {get; private set;}

    [Tooltip("Tag used to auto-discover ParticleSystems in the scene on Start")]
    public string managedTag = "ManagedParticle";

    private readonly List<ParticleSystem> systems = new();

    private void Awake()
    {
        if(instance != null && instance != this) {Destroy(gameObject); return;}
        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        //Auto-discover tagged systems
        foreach(var go in GameObject.FindGameObjectsWithTag(managedTag))
        {
            var ps = go.GetComponent<ParticleSystem>();
            if(ps != null) Register(ps);
        }

        //Apply current setting immediately
        if(SettingsManager.instance != null)
            SetParticlesEnabled(SettingsManager.instance.ParticlesEnabled);
    }

    //------------------------------------------------------------------------------------------

    public void Register(ParticleSystem ps)
    {
        if(ps != null && !systems.Contains(ps))
            systems.Add(ps);
    }

    public void Unregister(ParticleSystem ps)
    {
        systems.Remove(ps);
    }

    //Called by SettingsManager when the particles setting changes
    public void SetParticlesEnabled(bool enabled)
    {
        foreach(var ps in systems)
        {
            if(ps == null) continue;

            var emission = ps.emission;
            emission.enabled = enabled;

            //Stop/clear existing particles when disabling
            if(!enabled) ps.Clear();
        }
    }
}