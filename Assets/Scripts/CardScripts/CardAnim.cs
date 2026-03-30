using UnityEngine;
using System.Collections;

public class CardAnim : MonoBehaviour
{
    RectTransform rect;

    [Header("Idle Float")]
    public float floatRotationAmount = 6f;
    public float floatSpeed = 1.5f;
    public float floatPositionAmount = 10f;

    Vector3 startPos;

    bool spinning = false;

    void Awake()
    {
        rect = GetComponent<RectTransform>();
        startPos = rect.anchoredPosition;
    }

    void Update()
    {
        if(spinning) return;

        float t = Time.time * floatSpeed;

        //Gentle rotation
        float rot = Mathf.Sin(t) * floatRotationAmount;

        //Slight vertical movement
        float yOffset = Mathf.Sin(t * 0.7f) * floatPositionAmount;

        rect.localRotation = Quaternion.Euler(0, 0, rot);
        rect.anchoredPosition = startPos + new Vector3(0, yOffset, 0);
    }

    public void Spin(float duration = 0.5f, int spins = 2)
    {
        StartCoroutine(SpinRoutine(duration, spins));
    }

    IEnumerator SpinRoutine(float duration, int spins)
    {
        spinning = true;

        Quaternion startRot = rect.localRotation;
        Quaternion endRot = startRot * Quaternion.Euler(0, 360f * spins, 0);

        float t = 0f;

        while(t < duration)
        {
            t += Time.deltaTime;
            float p = t / duration;

            rect.localRotation = Quaternion.Slerp(startRot, endRot, p);
            yield return null;
        }

        rect.localRotation = endRot;

        spinning = false;
    }
}
