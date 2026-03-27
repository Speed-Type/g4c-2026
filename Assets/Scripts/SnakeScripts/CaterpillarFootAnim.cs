using System.Collections;
using UnityEngine;

public class CaterpillarFootAnim : MonoBehaviour
{
    public Transform foot1;
    public Transform foot2;

    public float rotationAngle = 25f;
    public float speed = 6f;

    //Phase offset between leg pairs
    public float phaseOffset;

    Quaternion foot1BaseRot;
    Quaternion foot2BaseRot;

    void Start()
    {
        foot1BaseRot = foot1.localRotation;
        foot2BaseRot = foot2.localRotation;
    }

    void Update()
    {
        float t = Time.time * speed;

        float foot1Wave = Mathf.Sin(t + phaseOffset);
        float foot2Wave = Mathf.Sin(t + phaseOffset + Mathf.PI); //opposite phase

        foot1.localRotation = foot1BaseRot * Quaternion.Euler(0, foot1Wave * rotationAngle, 0);
        foot2.localRotation = foot2BaseRot * Quaternion.Euler(0, -foot2Wave * rotationAngle, 0);
    }
}
