using UnityEngine;
using UnityEngine.UI;

public class ScrollingImageAnim : MonoBehaviour
{
    public float scrollSpeedX = 0.5f;
    public float scrollSpeedY = 0f;

    private Material mat;
    private Vector2 offset;

    void Start()
    {
        mat = new Material(GetComponent<Image>().material);
        GetComponent<Image>().material = mat;
    }

    void Update()
    {
        offset.x += scrollSpeedX * Time.deltaTime;
        offset.y += scrollSpeedY * Time.deltaTime;

        mat.SetTextureOffset("_MainTex", offset);
    }
}
