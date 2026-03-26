using UnityEngine;

public class ScreenShake : MonoBehaviour
{
    //Instead of using LeanTween just make everything run in update
    public static ScreenShake instance;

    [Header("References")]
    public Transform cameraTransform;
    public RectTransform uiRoot;

    [Header("Shake Settings")]
    public float maxTranslation3D = 20f;
    public float maxRotation = 2f;
    public float maxTranslationUI = 10f;
    public float traumaDecay = 1.8f;

    [Header("Noise")]
    public float noiseFrequency = 30f;

    //Internal variables
    float trauma = 0f;
    float noiseSeed;

    Vector3 currentCamOffset;
    float currentRotOffset;
    Vector2 currentUIOffset;

    void Awake()
    {
        //Set static instance
        instance = this;

        noiseSeed = Random.Range(0f, 1000f); //Necessary for perlin noise
    }

    void LateUpdate()
    {
        //Remove previous frame's shake
        cameraTransform.localPosition -= currentCamOffset;
        cameraTransform.localRotation *= Quaternion.Euler(0, 0, -currentRotOffset);

        if(uiRoot != null) uiRoot.anchoredPosition -= currentUIOffset;

        currentCamOffset = Vector3.zero;
        currentRotOffset = 0;
        currentUIOffset = Vector2.zero;

        //If there is shake left to do, do shake
        if(trauma > 0)
        {
            float shake = trauma * trauma; //Nonlinear scaling for game juice

            float time = Time.time * noiseFrequency;

            //Get random perlin noise values
            float x = Mathf.PerlinNoise(noiseSeed, time) * 2 - 1;
            float y = Mathf.PerlinNoise(noiseSeed + 1, time) * 2 - 1;
            float r = Mathf.PerlinNoise(noiseSeed + 2, time) * 2 - 1;

            //Set cam position and rotation offsets
            currentCamOffset = new Vector3(x, y, 0) * maxTranslation3D * shake;
            currentRotOffset = r * maxRotation * shake;

            //Set UI position offset
            currentUIOffset = new Vector2(x, y) * maxTranslationUI * 40f * shake;

            //Apply these offsets
            cameraTransform.localPosition += currentCamOffset;
            cameraTransform.localRotation *= Quaternion.Euler(0, 0, currentRotOffset);

            if(uiRoot != null) uiRoot.anchoredPosition += currentUIOffset;



            //Decay trauma
            trauma -= traumaDecay * Time.deltaTime;
            trauma = Mathf.Clamp01(trauma);
        }
    }

    public void AddTrauma(float amount)
    {
        trauma = Mathf.Clamp01(trauma + amount);
    }

    public void SetTrauma(float amount)
    {
        trauma = Mathf.Clamp01(amount);
    }
}
