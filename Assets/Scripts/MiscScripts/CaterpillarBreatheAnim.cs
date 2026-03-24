using UnityEngine;

public class CaterpillarBreatheAnim : MonoBehaviour
{
    public float sizeMultiplier = 1.1f;
    public float animLength = 3f;

    void Start()
    {
        LeanTween.scale(this.gameObject, Vector3.one * sizeMultiplier, animLength) //Scale undulate
            .setEaseInOutSine()
            .setLoopPingPong();
    }
}
