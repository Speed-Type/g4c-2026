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
    public SceneTransitionHandler sceneTransitionHandler;

    void Start()
    {
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
        sceneTransitionHandler.SceneTransitionToScene("NextStage");
    }

    public void NewGame()
    {
        //TODO: Reset necessary playerprefs

        PlayerPrefs.DeleteKey("CurrentStage"); //Reset to first stage

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
