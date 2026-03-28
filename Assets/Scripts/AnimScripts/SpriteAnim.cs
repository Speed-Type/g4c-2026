using UnityEngine;
using UnityEngine.UI;

public class SpriteAnim : MonoBehaviour
{
    public Sprite[] spriteList;
    public float timeBetweenFrames;
    public bool destroyAfterComplete;

    private Image image;

    void Start()
    {
        image = GetComponent<Image>();
        DoFrame(0);
    }

    private void DoFrame(int frameNum)
    {
        image.sprite = spriteList[frameNum];

        LeanTween.delayedCall(timeBetweenFrames, () =>
        {
            if(frameNum + 1 < spriteList.Length)
            {
                DoFrame(frameNum + 1);
            }
            else
            {
                if(destroyAfterComplete) Destroy(this.gameObject);
            }
        });
    }
}
