using UnityEngine;
using TMPro;

public class ScrollingTextAnim : MonoBehaviour
{
    public RectTransform textA;
    public RectTransform textB;
    public float speed = 100f;

    private float width;

    void Start()
    {
        TextMeshProUGUI tmp = textA.GetComponent<TextMeshProUGUI>();
        tmp.ForceMeshUpdate();

        width = textA.rect.width;

        textB.anchoredPosition = new Vector2(width, 0);
    }

    void Update()
    {
        float move = speed * Time.deltaTime;

        textA.anchoredPosition += Vector2.left * move;
        textB.anchoredPosition += Vector2.left * move;

        if (textA.anchoredPosition.x <= -width)
            textA.anchoredPosition += new Vector2(width * 2, 0);

        if (textB.anchoredPosition.x <= -width)
            textB.anchoredPosition += new Vector2(width * 2, 0);
    }
}
