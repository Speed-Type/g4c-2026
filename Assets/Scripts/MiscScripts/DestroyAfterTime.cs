using UnityEngine;

public class DestroyAfterTime : MonoBehaviour
{
    public float timeTilDestroy;
    void Start()
    {
        Destroy(this.gameObject, timeTilDestroy);
    }
}
