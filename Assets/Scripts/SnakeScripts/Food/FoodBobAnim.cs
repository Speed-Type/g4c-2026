using UnityEngine;

public class FoodBobAnim : MonoBehaviour
{
    public float sizeMultiplier = 1.1f;
    public float animLength = 3f;
    public float initialGrowthLength;

    void Start()
    {
        transform.localScale = Vector3.zero;
        
        LeanTween.scale(this.gameObject, Vector3.one, initialGrowthLength)
        .setOnComplete(() =>
        {
            LeanTween.scale(this.gameObject, Vector3.one * sizeMultiplier, animLength) //Scale undulate
            .setEaseInOutSine()
            .setLoopPingPong();
        });
    }
}
