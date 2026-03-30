using UnityEngine;

public class FoodItem : MonoBehaviour
{
    public event System.Action onDestroyed;

    private void OnDestroy()
    {
        onDestroyed?.Invoke();
    }
}
