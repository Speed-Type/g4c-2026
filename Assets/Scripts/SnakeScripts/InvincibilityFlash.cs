using UnityEngine;

public class InvincibilityFlash : MonoBehaviour
{
    public GameObject target;

    private float invincibilityDuration = 2f;
    private float startInterval = 0.25f;
    private float endInterval = 0.05f;

    private float elapsed;

    public void StartInvincibilityFlash(float invincibilityDuration, float startInterval, float endInterval)
    {
        //Set variables
        elapsed = 0f;
        this.invincibilityDuration = invincibilityDuration;
        this.startInterval = startInterval;
        this.endInterval = endInterval;

        //Call first function call
        FlashStep();
    }

    void FlashStep()
    {
        if(elapsed >= invincibilityDuration)
        {
            target.SetActive(true);
            return;
        }

        //Progress through invincibility
        float t = elapsed / invincibilityDuration;

        //Interpolate interval (slow to fast)
        float interval = Mathf.Lerp(startInterval, endInterval, t * t);

        target.SetActive(!target.activeSelf);

        LeanTween.delayedCall(interval, () =>
        {
            elapsed += interval;
            FlashStep();
        });
    }
}