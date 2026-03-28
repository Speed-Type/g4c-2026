using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class MainMenuController : MonoBehaviour
{
    [Header("Camera References")]
    public GameObject cam;
    public Transform defaultCamTransform;
    public Transform extrasCamTransform;
    public Transform creditsCamTransform;
    public float camTransitionLength;

    [Header("Game Start")]
    public GameObject startButton;
    public GameObject continueButton;
    public GameObject newGameButton;

    [Header("Screen Transition")]
    public CanvasRenderer sceneTransitionImage;
    public float screenTransitionLength;

    void Start()
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

        //Show the right start buttons
        if(PlayerPrefs.GetInt("HasStartedGame") == 0)
        {
            //Has not started a game yet
            startButton.SetActive(true);
            continueButton.SetActive(false);
            newGameButton.SetActive(false);
        }
        else
        {
            //Has started a game
            startButton.SetActive(false);
            continueButton.SetActive(true);
            newGameButton.SetActive(true);
        }

        //Set cam to default position
        cam.transform.position = defaultCamTransform.position;
        cam.transform.rotation = defaultCamTransform.rotation;
    }

    //Method called to start a game
    //Called by both startButton and continueButton, since they're functionally the same
    public void StartGame()
    {
        PlayerPrefs.SetInt("HasStartedGame", 1);

        //Start screen transition
        sceneTransitionImage.gameObject.SetActive(true);
        LeanTween.value(0, 1, screenTransitionLength)
        .setOnUpdate((float val) =>
        {
            sceneTransitionImage.SetAlpha(val);
        })
        .setOnComplete(() =>
        {
            SceneManager.LoadScene("RoguelikeGameplayScene");
        });
    }

    public void NewGame()
    {
        //TODO: Reset necessary playerprefs

        StartGame();
    }

    public void Extras()
    {
        PanCamToTransform(extrasCamTransform, camTransitionLength);
    }

    public void MainMenu()
    {
        PanCamToTransform(defaultCamTransform, camTransitionLength);
    }

    public void Credits()
    {
        PanCamToTransform(creditsCamTransform, camTransitionLength);
    }

    private void PanCamToTransform(Transform targetTransform, float timing, Action onCompleteAction = null)
    {
        LeanTween.move(cam, targetTransform.position, timing)
            .setEase(LeanTweenType.easeOutCubic);
        LeanTween.rotate(cam, targetTransform.rotation.eulerAngles, timing)
            .setEase(LeanTweenType.easeOutCubic)
            .setOnComplete(() => {if(onCompleteAction != null) onCompleteAction();});
    }
}
