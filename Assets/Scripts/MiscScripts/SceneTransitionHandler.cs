using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransitionHandler : MonoBehaviour
{
    [Header("Screen Transition")]
    public CanvasRenderer sceneTransitionImage;
    public float screenTransitionLength;

    void Start()
    {
        SceneTransitionIn();
    }

    public void SceneTransitionIn()
    {
        //Scene transition in
        sceneTransitionImage.gameObject.SetActive(true);
        LeanTween.delayedCall(0.5f, () =>
        {
            LeanTween.value(1, 0, screenTransitionLength)
            .setOnUpdate((float val) =>
            {
                sceneTransitionImage.SetAlpha(val);
            })
            .setOnComplete(() =>
            {
                sceneTransitionImage.gameObject.SetActive(false);
            });
        });
    }

    public void SceneTransitionToScene(string targetScene)
    {
        sceneTransitionImage.gameObject.SetActive(true);
        LeanTween.value(0, 1, screenTransitionLength)
        .setOnUpdate((float val) =>
        {
            sceneTransitionImage.SetAlpha(val);
        })
        .setOnComplete(() =>
        {
            SceneManager.LoadScene(targetScene);
        });
    }
}
