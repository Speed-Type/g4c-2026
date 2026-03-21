using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class ButtonClickAnim : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private float maxRotation = 6f;
    [SerializeField] private float shakeTime = 0.05f;
    private int rotateTween;

    public void OnPointerClick(PointerEventData eventData)
    {
        Shake();
    }

    void OnEnable()
    {
        LeanTween.cancel(rotateTween);
        transform.localRotation = Quaternion.Euler(0,0,0); //Reset rotation
    }

    //Method to do shake
    private void Shake()
    {
        //Stop current tween for shake effect
        LeanTween.cancel(rotateTween);
        transform.localRotation = Quaternion.Euler(0,0,0);

        //Do small graphic shake
        rotateTween = LeanTween.rotateZ(gameObject, maxRotation, shakeTime)
            .setEaseShake()
            .setLoopPingPong(2)
            .id;
    }
}
