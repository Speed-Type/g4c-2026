using UnityEngine;
using System.Collections.Generic;

public class RoguelikeGameplayController : MonoBehaviour
{
    public DialogueBoxController dialogueBoxController;

    //Variables in Inspector

    [Header("Player Data")]
    public int playerHealth;
    public int playerFlesh;
    public int maxBites;
    private int remainingBites;

    [Header("System References")]
    [SerializeField] private DialogueBoxController dialogueSystemController;
    [SerializeField] private GameObject inventorySystemGO;
    [SerializeField] private GameObject lootSystemGO;
    [SerializeField] private GameObject nodeMapGO;
    
    [Header("Player Option Buttons")]
    [SerializeField] private GameObject showMapButton;
    [SerializeField] private GameObject hideMapButton;
    [SerializeField] private GameObject showInventorySystemButton;
    [SerializeField] private GameObject hideInventorySystemButton;
    [SerializeField] private GameObject showLootSystemButton;
    [SerializeField] private float playerOptionButtonShrunkenSize;
    [SerializeField] private float playerOptionButtonShrinkSpeed;
    private Vector2 playerOptionButtonDefaultDimensions;

    [Header("References for Bite System")]
    [SerializeField] private GameObject[] biteList;
    [SerializeField] private int currentBite;
    [SerializeField] private RectTransform biteHomePosition;

    [Header("References for Placement of Inventory and Loot Systems")]
    [SerializeField] private Transform hiddenPosition;
    [SerializeField] private Transform shownPosition;

    [Header("Camera Stuff")]
    [SerializeField] private RoguelikeCameraController cameraController;
    
    //Hidden variables
    private List<PlayerOptionButtons> openButtons = new List<PlayerOptionButtons>();
    private List<PlayerOptionButtons> allButtons = new List<PlayerOptionButtons>();
    private GeneralInventoryStructure inventoryManager;
    private bool playerCanMakeChoices;

    //Static variable for current game state
    public static GameState gameState;

    //Static variable for holding the one instance of this variable
    private static RoguelikeGameplayController instance;

    //Method to update the buttons that should be open; called primarily by nodes
    public void SetOpenButtons(List<PlayerOptionButtons> newList)
    {
        openButtons = newList;

        //Shrink buttons
        ResetPlayerOptionButtonHeight();

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
            case PlayerOptionButtons.Loot:
                return showLootSystemButton;
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
        TogglePlayerOptionButtons(false);

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
                TogglePlayerOptionButtons(true);
                
                //Show dialogue box
                dialogueSystemController.ShowDialogueBox();
            });
        });
    }

    private void TogglePlayerOptionButtons(bool value)
    {
        foreach(PlayerOptionButtons button in allButtons)
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
            LeanTween.value(this.gameObject, UpdatePlayerOptionButtonHeight, playerOptionButtonShrunkenSize, playerOptionButtonDefaultDimensions.y, playerOptionButtonShrinkSpeed).setEaseOutElastic();
        }
    }

    private void ResetPlayerOptionButtonHeight()
    {
        foreach(PlayerOptionButtons button in allButtons)
        {
            playerOptionButtonsEnumToGameObject(button).GetComponent<RectTransform>().sizeDelta = new Vector2(playerOptionButtonDefaultDimensions.x, playerOptionButtonShrunkenSize);
        }
    }

    private void UpdatePlayerOptionButtonHeight(float newHeight)
    {
        foreach(PlayerOptionButtons button in openButtons)
        {
            playerOptionButtonsEnumToGameObject(button).GetComponent<RectTransform>().sizeDelta = new Vector2(playerOptionButtonDefaultDimensions.x, newHeight);
        }
    }

    public void ShowInventory()
    {
        //Make sure the player can actually make choices
        if(!playerCanMakeChoices)
        {
            return;
        }
        
        //Make sure this option is actually one of the open ones
        if(!openButtons.Contains(PlayerOptionButtons.Inventory))
        {
            return;
        }

        //Update graphics
        TogglePlayerOptionButtons(false);
        //inventorySystemGO.SetActive(true);
        inventorySystemGO.GetComponent<RectTransform>().position = shownPosition.position;
        dialogueSystemController.HideDialogueBox();

        //Update game state
        gameState = GameState.InMenu;
    }

    public void HideInventory()
    {
        //Update graphics
        TogglePlayerOptionButtons(true);
        //inventorySystemGO.SetActive(false);
        inventorySystemGO.GetComponent<RectTransform>().position = hiddenPosition.position;
        dialogueSystemController.ShowDialogueBox();

        //Update game state
        gameState = GameState.Idle;
    }

    public void ShowLoot()
    {
        //Make sure the player can actually make choices
        if(!playerCanMakeChoices)
        {
            return;
        }

        //Make sure this option is actually one of the open ones
        if(!openButtons.Contains(PlayerOptionButtons.Loot))
        {
            return;
        }

        //Update graphics
        TogglePlayerOptionButtons(false);
        //lootSystemGO.SetActive(true);
        lootSystemGO.GetComponent<RectTransform>().position = shownPosition.position;
        //inventorySystemGO.SetActive(true);
        inventorySystemGO.GetComponent<RectTransform>().position = shownPosition.position;
        hideInventorySystemButton.SetActive(false);
        dialogueSystemController.HideDialogueBox();
        ReloadBite();

        //Update game state
        gameState = GameState.InMenu;
    }

    public void HideLoot()
    {
        //Update graphics
        TogglePlayerOptionButtons(true);
        //lootSystemGO.SetActive(false);
        lootSystemGO.GetComponent<RectTransform>().position = hiddenPosition.position;
        //inventorySystemGO.SetActive(false);
        inventorySystemGO.GetComponent<RectTransform>().position = hiddenPosition.position;
        hideInventorySystemButton.SetActive(true);
        dialogueSystemController.ShowDialogueBox();
        biteList[currentBite].SetActive(false);

        //Update game state
        gameState = GameState.Idle;
    }

    void Awake()
    {
        //Set the instance
        instance = this;

        //Enable/disable necessary systems
        nodeMapGO.SetActive(true);
        //inventorySystemGO.SetActive(false);
        inventorySystemGO.GetComponent<RectTransform>().position = hiddenPosition.position;
        //lootSystemGO.SetActive(false);
        lootSystemGO.GetComponent<RectTransform>().position = hiddenPosition.position;

        //Disable all buttons
        TogglePlayerOptionButtons(false);

        //Initialize default player option button dimensions
        playerOptionButtonDefaultDimensions = showMapButton.GetComponent<RectTransform>().sizeDelta;

        //Initialize the list of all buttons
        allButtons = new List<PlayerOptionButtons> {PlayerOptionButtons.Map, PlayerOptionButtons.Inventory, PlayerOptionButtons.Loot};
    
        //Initialize inventory manager (is attached to the same object)
        inventoryManager = GetComponent<InventoryManager>();

        //Reset Fragment class's static variable
        Fragment.ResetFragmentList();

        //Set number of bites
        ResetBites();
        biteList[currentBite].SetActive(false);
    }

    //Static method to add flesh
    public static void AddFlesh(int amount)
    {
        instance.playerFlesh += amount;
    }

    //Static method to do a bite and decrease number of bites remaining
    public static void ReduceBites()
    {
        instance.remainingBites--;
    }

    public static void ReloadBite_Static()
    {
        instance.ReloadBite();
    }

    //Moves bite back to proper position, shows if there are any bites remaining
    private void ReloadBite()
    {
        //POTENTIALLY: Reset size of bite here
        if(remainingBites > 0)
        {
            biteList[currentBite].GetComponent<RectTransform>().position = biteHomePosition.position + new Vector3(-inventoryManager.inventorySlotSize / 2f, inventoryManager.inventorySlotSize / 2f, 0); //Reset position
            biteList[currentBite].SetActive(true);
        }
        else
        {
            biteList[currentBite].SetActive(false);
        }
    }

    //Sets current bites to max bites for the next looting
    public static void ResetBites()
    {
        instance.remainingBites = instance.maxBites;
    }
}

public enum PlayerOptionButtons
{
    Map,
    Inventory,
    Loot
}

public enum GameState
{
    Map,
    Idle,
    InMenu
}