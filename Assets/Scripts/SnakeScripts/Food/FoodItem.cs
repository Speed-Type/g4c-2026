using UnityEngine;

public class FoodItem : MonoBehaviour
{
    public event System.Action onDestroyed;
    public GameObject particleEffect;

    private void OnDestroy()
    {
        onDestroyed?.Invoke();
        Instantiate(particleEffect, transform.position, transform.rotation);
    }
}
