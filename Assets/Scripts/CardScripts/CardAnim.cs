using UnityEngine;
using System.Collections;
using System;

public class CardAnim : MonoBehaviour
{
    RectTransform rect;

    [Header("Idle Float")]
    public float floatRotationAmount = 6f;
    public float floatSpeed = 1.5f;
    public float floatPositionAmount = 10f;
    public float floatScaleAmount = 0.03f;

    private Vector3 startPos;
    private Vector3 startScale;
    private Coroutine spinCoroutine;

    void Awake()
    {
        rect = GetComponent<RectTransform>();
        startPos = rect.anchoredPosition;
        startScale = rect.localScale;
    }

    void Update()
    {
        float t = Time.time * floatSpeed;

        //Gentle rotation
        float rot = Mathf.Sin(t) * floatRotationAmount;

        //Slight vertical movement
        float yOffset = Mathf.Sin(t * 0.7f) * floatPositionAmount;

        //Breathing scale
        float scaleFactor = 1 + Mathf.Sin(t) * floatScaleAmount;
        rect.localScale = startScale * scaleFactor;

        rect.localRotation = Quaternion.Euler(0, rect.localEulerAngles.y, rot);
        rect.anchoredPosition = startPos + new Vector3(0, yOffset, 0);
    }

    public void Spin(Action halfwayCallback = null, float duration = 0.5f, int spins = 2)
    {
        if(spinCoroutine != null)
            StopCoroutine(spinCoroutine);

        spinCoroutine = StartCoroutine(SpinRoutine(halfwayCallback, duration, spins));
    }

    IEnumerator SpinRoutine(Action halfwayCallback, float duration, int spins)
    {
        float startAngle = rect.localEulerAngles.y;
        float totalRotation = 360f * spins;

        float t = 0f;
        bool triggered = false;

        while (t < duration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / duration);

            //Ease-out quart for smooth deceleration
            float easedP = 1 - Mathf.Pow(1 - p, 4);

            float currentAngle = startAngle + totalRotation * easedP;

            //Breathing scale continues during spin
            float scaleFactor = 1 + Mathf.Sin(Time.time * floatSpeed) * floatScaleAmount;
            rect.localScale = startScale * scaleFactor;

            rect.localRotation = Quaternion.Euler(0, currentAngle, rect.localEulerAngles.z);

            if (!triggered && p >= 0.5f)
            {
                triggered = true;
                halfwayCallback?.Invoke();
            }

            yield return null;
        }

        //Snap to final rotation & reset scale
        rect.localRotation = Quaternion.Euler(0, startAngle + totalRotation, rect.localEulerAngles.z);

        spinCoroutine = null;
    }
}
