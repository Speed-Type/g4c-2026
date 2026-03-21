using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class ButtonHoverAnim : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Animation Parameters")]
    [SerializeField] private float initialSizeMultiplier = 1.5f;
    [SerializeField] private float undulatingSizeMultiplier = 1.3f;
    [SerializeField] private float initialGrowthSpeed = 0.3f;
    [SerializeField] private float undulatingGrowthSpeed = 0.45f;
    [Header("Optional to Set; If left blank, will pull from the same object as this script")]
    [SerializeField] private RectTransform rectTransform;

    //Private variables
    private int runningTween;
    private bool doubleCheckIsHovered = false;

    void Start()
    {
        if(rectTransform == null) rectTransform = GetComponent<RectTransform>();
    }

    void Update()
    {
        
    }

    public virtual void OnPointerEnter(PointerEventData eventData)
    {
        if(doubleCheckIsHovered) return;
        doubleCheckIsHovered = true;

        //Start tween with initial elastic growth
        runningTween = LeanTween.scale(rectTransform, Vector3.one * initialSizeMultiplier, initialGrowthSpeed)
            .setEaseOutElastic()
            .setOnComplete(() =>
            {
                runningTween = LeanTween.scale(rectTransform, Vector3.one * undulatingSizeMultiplier, undulatingGrowthSpeed) //Scale undulate
                    .setEaseInOutSine()
                    .setLoopPingPong()
                    .id;
            })
            .id;
    }

    public virtual void OnPointerExit(PointerEventData eventData)
    {
        if(!doubleCheckIsHovered) return;
        doubleCheckIsHovered = false;

        LeanTween.cancel(runningTween);
        LeanTween.scale(rectTransform, Vector3.one, initialGrowthSpeed).setEaseOutElastic();
    }

    void OnDisable()
    {
        LeanTween.cancel(runningTween);
        LeanTween.scale(rectTransform, Vector3.one, initialGrowthSpeed).setEaseOutElastic();
    }
}
