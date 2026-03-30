using UnityEngine;

//Add to any GameObject with a ParticleSystem to have it automatically managed by the particles toggle in Settings

[RequireComponent(typeof(ParticleSystem))]
public class ManagedParticle : MonoBehaviour
{
    private ParticleSystem ps;

    private void Awake()  => ps = GetComponent<ParticleSystem>();
    private void OnEnable()  => ParticleManager.instance?.Register(ps);
    private void OnDisable() => ParticleManager.instance?.Unregister(ps);
}