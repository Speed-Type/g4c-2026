using UnityEngine;

public class HorizontalRotationAnim : MonoBehaviour
{
    [Header("Degrees/sec")]
    public float turnSpeed;

    void Update()
    {
        transform.Rotate(0f, turnSpeed * Time.deltaTime, 0f);
    }
}
