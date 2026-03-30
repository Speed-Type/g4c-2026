using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransitionHandler : MonoBehaviour
{
    [Header("Screen Transition")]
    public CanvasRenderer sceneTransitionImage;
    public float screenTransitionLength;
    public float delayBeforeEnteringScene;

    [Header("Settings Menu")]
    public GameObject settingsMenu;

    void Start()
    {
        SceneTransitionIn();
    }

    public void SceneTransitionIn()
    {
        //Set the settings menu un-openable
        if(settingsMenu != null) settingsMenu?.SetActive(false);

        //Scene transition in
        sceneTransitionImage.gameObject.SetActive(true);
        LeanTween.delayedCall(delayBeforeEnteringScene, () =>
        {
            LeanTween.value(1, 0, screenTransitionLength)
            .setOnUpdate((float val) =>
            {
                sceneTransitionImage.SetAlpha(val);
            })
            .setOnComplete(() =>
            {
                sceneTransitionImage.gameObject.SetActive(false);
                if(settingsMenu != null) settingsMenu?.SetActive(true);
            });
        });
    }

    public void SceneTransitionToScene(string targetScene)
    {
        //Set the settings menu un-openable
        if(settingsMenu != null) settingsMenu.SetActive(false);

        //Scene transition out
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
