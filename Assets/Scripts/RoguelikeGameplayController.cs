using UnityEngine;
using System.Collections.Generic;

public class RoguelikeGameplayController : MonoBehaviour
{
    public DialogueBoxController dialogueBoxController;

    //Variables in Inspector
    public GameState gameState;
    [Header("Player Option GameObjects")]
    public GameObject showMapButton;
    public GameObject hideMapButton;
    public GameObject nodeMap;

    [Header("Camera Stuff")]
    [SerializeField] private RoguelikeCameraController cameraController;

    [Header("Player data")]
    public int playerHealth;
    

    //Hidden variables
    private List<PlayerOptionButtons> openButtons = new List<PlayerOptionButtons>();

    //Method to update the buttons that should be open; called primarily by nodes
    public void SetOpenButtons(List<PlayerOptionButtons> newList)
    {
        openButtons = newList;

        //Immediately show player's options
        HideMap();
    }

    //Method to convert enum PlayerOptionButtons to actual GameObjects
    private GameObject playerOptionButtonsEnumToGameObject(PlayerOptionButtons button)
    {
        switch(button)
        {
            case PlayerOptionButtons.Map:
                return showMapButton;
            default:
                return null;
        }
    }

    //Method to show map (and by extension, hide open buttons)
    public void ShowMap()
    {
        //Update game state
        gameState = GameState.Map;

        //Hide other open buttons
        foreach(PlayerOptionButtons button in openButtons)
        {
            playerOptionButtonsEnumToGameObject(button).SetActive(false);
        }

        //Update camera
        cameraController.StopFreeCam();

        //Show "hide map" button after moved to freecam position
        LeanTween.delayedCall(RoguelikeCameraController.freeCamTransitionTiming, () =>
        {
            hideMapButton.SetActive(true);
            
            //Show map
            nodeMap.SetActive(true);
        });
    }

    //Method to hide map (and by extension, show open buttons)
    public void HideMap()
    {
        //Hide map
        nodeMap.SetActive(false);

        //Hide "hide map" button
        hideMapButton.SetActive(false);

        //Update game state
        gameState = GameState.Idle;

        //Update camera
        cameraController.StartFreeCam();

        //Show other open buttons after moved to freecam position
        LeanTween.delayedCall(RoguelikeCameraController.freeCamTransitionTiming, () =>
        {
            foreach(PlayerOptionButtons button in openButtons)
            {
                playerOptionButtonsEnumToGameObject(button).SetActive(true);
            }
        });
    }

    void Awake()
    {
        //Enable/disable necessary buttons
        nodeMap.SetActive(true);
        hideMapButton.SetActive(true);
        showMapButton.SetActive(false);
    }
}
public enum PlayerOptionButtons
{
    Map
}

public enum GameState
{
    Map,
    Idle
}