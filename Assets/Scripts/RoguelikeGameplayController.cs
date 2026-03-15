using UnityEngine;
using System.Collections.Generic;

public class RoguelikeGameplayController : MonoBehaviour
{
    public DialogueBoxController dialogueBoxController;

    //Variables in Inspector
    public GameState gameState;

    [Header("System References")]
    [SerializeField] private DialogueBoxController dialogueSystemController;
    [SerializeField] private GameObject inventorySystemGO;
    [SerializeField] private GameObject nodeMapGO;
    
    [Header("Player Option Buttons")]
    [SerializeField] private GameObject showMapButton;
    [SerializeField] private GameObject hideMapButton;
    [SerializeField] private GameObject showInventorySystemButton;
    [SerializeField] private float playerOptionButtonShrunkenSize;
    [SerializeField] private float playerOptionButtonShrinkSpeed;
    private Vector2 playerOptionButtonDefaultDimensions;

    [Header("Camera Stuff")]
    [SerializeField] private RoguelikeCameraController cameraController;

    [Header("Player Data")]
    public int playerHealth;
    
    //Hidden variables
    private List<PlayerOptionButtons> openButtons = new List<PlayerOptionButtons>();
    private bool playerCanMakeChoices;

    //Method to update the buttons that should be open; called primarily by nodes
    public void SetOpenButtons(List<PlayerOptionButtons> newList)
    {
        openButtons = newList;

        //Shrink buttons
        SetPlayerOptionButtonHeight(playerOptionButtonShrunkenSize);

        //Immediately show player's options
        HideMap(true);
    }

    //Method to convert enum PlayerOptionButtons to actual GameObjects
    private GameObject playerOptionButtonsEnumToGameObject(PlayerOptionButtons button)
    {
        switch(button)
        {
            case PlayerOptionButtons.Map:
                return showMapButton;
            case PlayerOptionButtons.Inventory:
                return showInventorySystemButton;
            default:
                return null;
        }
    }

    //Method to show map (and by extension, hide open buttons)
    public void ShowMap()
    {
        //Make sure player can actually open the map
        if(!playerCanMakeChoices)
        {
            return;
        }

        //Update game state
        gameState = GameState.Map;

        //Hide other open buttons
        ToggleOpenButtons(false);

        //Hide dialogue box
        dialogueSystemController.HideDialogueBox();

        //The following code runs after general delay is met
        LeanTween.delayedCall(RoguelikeCameraController.generalDelayBetweenTransitions, () =>
        {
            //Camera transition
            cameraController.StopFreeCam();

            //The following code runs after free cam has transitioned to map state
            LeanTween.delayedCall(RoguelikeCameraController.freeCamTransitionTiming, () =>
            {
                //Show "hide map" button
                hideMapButton.SetActive(true);
                
                //Show map
                nodeMapGO.SetActive(true);
            });
        });
    }

    //Method to hide map (and by extension, show open buttons)
    public void HideMap(bool isNewNode = false)
    {
        //Hide "hide map" button
        hideMapButton.SetActive(false);

        float waitTime = RoguelikeCameraController.generalDelayBetweenTransitions;
        if(isNewNode)
        {
            waitTime += NodeManager.playerTravelAnimTime;

            //Also disable player choices
            playerCanMakeChoices = false;
        }
        else
        {
            nodeMapGO.SetActive(false);
        }

        LeanTween.delayedCall(waitTime, () =>
        {
            //Hide map
            if(isNewNode)
            {
                nodeMapGO.SetActive(false);
            }

            //Update game state
            gameState = GameState.Idle;

            //Update camera
            cameraController.StartFreeCam();

            //The following code runs after camera has transitioned back to free cam and paused the general delay amount
            LeanTween.delayedCall(RoguelikeCameraController.freeCamTransitionTiming + RoguelikeCameraController.generalDelayBetweenTransitions, () =>
            {
                //Show other open buttons
                ToggleOpenButtons(true);
                
                //Show dialogue box
                dialogueSystemController.ShowDialogueBox();
            });
        });
    }

    private void ToggleOpenButtons(bool value)
    {
        foreach(PlayerOptionButtons button in openButtons)
        {
            playerOptionButtonsEnumToGameObject(button).SetActive(value);
        }
    }

    public void EnablePlayerChoices()
    {
        playerCanMakeChoices = true;

        //Open them up
        foreach(PlayerOptionButtons button in openButtons)
        {
            LeanTween.value(this.gameObject, SetPlayerOptionButtonHeight, playerOptionButtonShrunkenSize, playerOptionButtonDefaultDimensions.y, playerOptionButtonShrinkSpeed).setEaseOutElastic();
        }
    }

    private void SetPlayerOptionButtonHeight(float newHeight)
    {
        foreach(PlayerOptionButtons button in openButtons)
        {
            playerOptionButtonsEnumToGameObject(button).GetComponent<RectTransform>().sizeDelta = new Vector2(playerOptionButtonDefaultDimensions.x, newHeight);
        }
    }

    public void ShowInventory()
    {
        //Make sure the player can actually open the map
        if(!playerCanMakeChoices)
        {
            return;
        }

        ToggleOpenButtons(false);
        inventorySystemGO.SetActive(true);
        dialogueSystemController.HideDialogueBox();
    }

    public void HideInventory()
    {
        ToggleOpenButtons(true);
        inventorySystemGO.SetActive(false);
        dialogueSystemController.ShowDialogueBox();
    }

    void Awake()
    {
        //Enable/disable necessary buttons
        nodeMapGO.SetActive(true);
        hideMapButton.SetActive(true);
        showMapButton.SetActive(false);
        inventorySystemGO.SetActive(false);
        showInventorySystemButton.SetActive(false);

        //Initialize default player option button dimensions
        playerOptionButtonDefaultDimensions = showMapButton.GetComponent<RectTransform>().sizeDelta;
    }
}

public enum PlayerOptionButtons
{
    Map,
    Inventory
}

public enum GameState
{
    Map,
    Idle
}