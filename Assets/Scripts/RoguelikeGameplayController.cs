using UnityEngine;
using System.Collections.Generic;
using TMPro;
using System;

public class RoguelikeGameplayController : MonoBehaviour
{
    [Header("Important Controllers and Parents")]
    public DialogueBoxController dialogueBoxController;
    public SnakeGameplayController snakeGameplayController;

    //Variables in Inspector

    [Header("Player Data")]
    public int currentFlesh;
    public int goalFlesh;
    public int maxBites;
    private int remainingBites;
    public float stealthChance;

    //Variables for revealing nodes
    public int emptyNodesToReveal;
    public int scavengerNodesToReveal;
    public int combatNodesToReveal;
    //public int eventNodesToReveal;

    [Header("Spider Behavior")]
    public int currentSpiderAlertness;
    public int maxSpiderAlertness;
    public int spiderMovesPerTurn;

    [Header("System References")]
    [SerializeField] private DialogueBoxController dialogueSystemController;
    public GameObject inventorySystemGO;
    public GameObject lootSystemGO;
    public GameObject statSystemGO;
    [SerializeField] private GameObject nodeMapGO;
    
    [Header("Player Option Buttons")]
    [SerializeField] private GameObject showMapButton;
    [SerializeField] private GameObject hideMapButton;
    [SerializeField] private GameObject showInventorySystemButton;
    [SerializeField] private GameObject hideInventorySystemButton;
    [SerializeField] private GameObject showLootSystemButton;
    [SerializeField] private GameObject combatButton;
    [SerializeField] private GameObject instarButton;
    [SerializeField] private float playerOptionButtonShrunkenSize;
    [SerializeField] private float playerOptionButtonShrinkSpeed;
    private Vector2 playerOptionButtonDefaultDimensions;

    [Header("References for Bite System")]
    [SerializeField] private GameObject[] biteList;
    public List<int> equippedBites;
    [SerializeField] private RectTransform biteHomePosition;
    [SerializeField] private RectTransform biteBackgroundRectTransform;
    [SerializeField] private float biteBackgroundPopupAnimLength = .2f;
    private Vector2 biteBackgroundOriginalPos;
    private int biteBackgroundPopupTween;
    [SerializeField] private TMP_Text bitesRemainingText;

    [Header("References for Placement of Inventory and Loot Systems")]
    [SerializeField] private Transform hiddenPosition;
    [SerializeField] private Transform shownPosition;

    [Header("Camera Stuff")]
    public RoguelikeCameraController cameraController;
    
    //Hidden variables
    private List<PlayerOptionButtons> openButtons = new List<PlayerOptionButtons>();
    private List<PlayerOptionButtons> allButtons = new List<PlayerOptionButtons>();
    private GeneralInventoryStructure inventoryManager;
    [HideInInspector] public bool playerCanMakeChoices;
    [HideInInspector] public static bool playerHasStartedLooting;

    //Static variable for current game state
    public static GameState gameState;

    //Static variable for holding the one instance of this variable
    public static RoguelikeGameplayController instance;

    void Awake()
    {
        //Set the instance
        instance = this;

        //Reset Fragment class's static variable
        Fragment.ResetFragmentList();

        //Initialize variables
        playerOptionButtonDefaultDimensions = showMapButton.GetComponent<RectTransform>().sizeDelta;
        allButtons = new List<PlayerOptionButtons> {PlayerOptionButtons.Map, PlayerOptionButtons.Inventory, PlayerOptionButtons.Loot, PlayerOptionButtons.Combat, PlayerOptionButtons.Instar};
        inventoryManager = GetComponent<InventoryManager>();
        biteBackgroundOriginalPos = biteBackgroundRectTransform.localPosition;
        playerHasStartedLooting = false;
        equippedBites = new List<int> {0};
    }

    void Start()
    {
        //Enable/disable necessary systems
        nodeMapGO.SetActive(true);
        inventorySystemGO.GetComponent<RectTransform>().position = hiddenPosition.position;
        lootSystemGO.GetComponent<RectTransform>().position = hiddenPosition.position;
        statSystemGO.SetActive(false);
        HideBites();

        //Disable all buttons
        TogglePlayerOptionButtons(false);

        //Set number of bites
        ResetBites();
    }

    //Method to update the buttons that should be open; called primarily by nodes
    public void SetOpenButtons(List<PlayerOptionButtons> newList)
    {
        openButtons = new List<PlayerOptionButtons>(newList);

        //Shrink buttons
        ResetPlayerOptionButtonHeight();
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
            case PlayerOptionButtons.Combat:
                return combatButton;
            case PlayerOptionButtons.Instar:
                return instarButton;
            default:
                return null;
        }
    }

    //Method to show map (and by extension, hide open buttons)
    public void ShowMap()
    {
        //Make sure player can actually open the map
        if(!playerCanMakeChoices) return;

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
            cameraController.TransitionToMapCam();

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

    public void HideMapPreloadFromScript()
    {
        //Hide "hide map" button
        hideMapButton.SetActive(false);

        //Also disable player choices
        playerCanMakeChoices = false;
    }

    //General method to hide map (and by extension, show open buttons)
    public void HideMap(bool isNewNode)
    {
        //Hide "hide map" button
        hideMapButton.SetActive(false);

        if(!isNewNode)
        {
            //Hide map itself now
            nodeMapGO.SetActive(false);
        }

        LeanTween.delayedCall(RoguelikeCameraController.generalDelayBetweenTransitions, () =>
        {
            //Hide map itself here if not already
            if(isNewNode)
            {
                nodeMapGO.SetActive(false);
            }

            //Update game state
            gameState = GameState.Idle;

            //Update camera
            cameraController.TransitionToFreeCam();
            cameraController.CloudTransitionIn();

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

        if(playerCanMakeChoices)
        {
            //Only do the animation if this is not being called from dialogue box controller
            //Otherwise, stay shut until the dialogue box controller calls for the options to open up
            OpenButtonAnimation();
        }

        doubleCheckInstar = false;
        doubleCheckCombat = false;
    }

    //Called from DialogueBoxController
    public void EnablePlayerChoices()
    {
        playerCanMakeChoices = true;

        OpenButtonAnimation();
    }

    private void OpenButtonAnimation()
    {
        //If the player has met the flesh requirement, add
        if(CanInstar() && !openButtons.Contains(PlayerOptionButtons.Instar)) openButtons.Add(PlayerOptionButtons.Instar);

        //Open them up
        foreach(PlayerOptionButtons button in openButtons)
        {
            //These tweens are tied to this RGManager gameobject (important to note)
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
        if(!playerCanMakeChoices) return;
        
        //Make sure this option is actually one of the open ones
        if(!openButtons.Contains(PlayerOptionButtons.Inventory)) return;

        //Update graphics
        TogglePlayerOptionButtons(false);
        inventorySystemGO.GetComponent<RectTransform>().position = shownPosition.position;
        dialogueSystemController.HideDialogueBox();
        statSystemGO.SetActive(true);
        StatDisplay.UpdateAllFleshFillBars_Static();
        StatDisplay.UpdateStats_Static();
        NodeManager.instance.HideEnemyBug();

        //Update camera
        cameraController.ShiftFreecamFocusToCaterpillar();

        //Update game state
        gameState = GameState.Inventory;
    }

    public void HideInventory()
    {
        //Update graphics
        TogglePlayerOptionButtons(true);
        inventorySystemGO.GetComponent<RectTransform>().position = hiddenPosition.position;
        dialogueSystemController.ShowDialogueBox();
        statSystemGO.SetActive(false);
        NodeManager.instance.ShowEnemyBug();

        //Update camera
        cameraController.ShiftFreecamFocusToIdle();

        //Update game state
        gameState = GameState.Idle;
    }

    public void ShowLoot()
    {
        //Make sure the player can actually make choices
        if(!playerCanMakeChoices) return;

        //Make sure this option is actually one of the open ones
        if(!openButtons.Contains(PlayerOptionButtons.Loot)) return;

        //Update graphics
        TogglePlayerOptionButtons(false);
        lootSystemGO.GetComponent<RectTransform>().position = shownPosition.position;
        inventorySystemGO.GetComponent<RectTransform>().position = shownPosition.position;
        hideInventorySystemButton.SetActive(false);
        dialogueSystemController.HideDialogueBox();
        StatDisplay.UpdateAllFleshFillBars_Static();

        //Prepare bite system
        LeanTween.cancel(biteBackgroundPopupTween); //cancel current tween to avoid overlap
        biteBackgroundRectTransform.localPosition = biteBackgroundOriginalPos - new Vector2(0, biteBackgroundRectTransform.sizeDelta.y); //TODO: reset bg position to a lower position
        ReloadBite();

        //Update game state
        gameState = GameState.Loot;
    }

    public void HideLoot()
    {
        //Update graphics
        TogglePlayerOptionButtons(true);
        lootSystemGO.GetComponent<RectTransform>().position = hiddenPosition.position;
        inventorySystemGO.GetComponent<RectTransform>().position = hiddenPosition.position;
        hideInventorySystemButton.SetActive(true);
        dialogueSystemController.ShowDialogueBox();
        HideBites();

        LeanTween.cancel(biteBackgroundPopupTween); //cancel current tween to avoid overlap

        //Update game state
        gameState = GameState.Idle;
    }

    public static bool CanInstar()
    {
        return instance.currentFlesh >= instance.goalFlesh;
    }

    private bool doubleCheckCombat = false;
    public void StartCombat()
    {
        //Make sure the player can actually make choices
        if(!playerCanMakeChoices) return;

        //Make sure this option is actually one of the open ones
        if(!openButtons.Contains(PlayerOptionButtons.Combat)) return;

        playerCanMakeChoices = false;

        //Make sure this option is actually one of the open ones
        if(!openButtons.Contains(PlayerOptionButtons.Combat)) return;

        if(doubleCheckCombat)
        {
            Debug.Log("Battle");

            gameState = GameState.Combat;

            //Switch camera
            cameraController.SwitchToCombatCam(snakeGameplayController.PlayGame);

            //Hide all UI
            TogglePlayerOptionButtons(false);
            dialogueSystemController.HideDialogueBox();
        }
        else
        {
            dialogueBoxController.SetText(new List<string> {"Press again to confirm battle start."});
            doubleCheckCombat = true;
        }
    }

    public void EndCombat(bool playerWon)
    {
        //Set results text as returned from Node_Combat method
        List<string> outputText = NodeManager.instance.playerNode.gameObject.GetComponent<Node_Combat>().GetResultsText(playerWon);
        
        //Add results for hits
        if(snakeGameplayController.totalHits > 0)
        {
            outputText.Add("You took " + snakeGameplayController.totalHits + " hits. You may want to check your case.");
        }
        else
        {
            outputText.Add("You finished that encounter without taking any hits!");
        }

        if(playerWon) //Case 1: Player wins
        {
            //Change node to scavenge node
            NodeManager.instance.ReplaceNode(NodeManager.instance.playerNode.gameObject, NodeTypes.Scavenge);

            //Upgrade to rare loot odds for this scavenge node
            NodeManager.instance.playerNode.UpgradeLoot();
        }
        else //Case 2: Player loses
        {
            //Increase spider alertness
            currentSpiderAlertness += 10;

            //Add to results text
            outputText.Add("The Spider gained 10 alertness.");

            //Change node to empty node
            NodeManager.instance.ReplaceNode(NodeManager.instance.playerNode.gameObject, NodeTypes.Empty);
        }

        outputText.Add("...");

        //Visit the current node
        NodeManager.overrideVisitConditions = true; //Override so that we can access this node even as we're on it
        NodeManager.instance.playerNode.VisitNode();

        //Transition out
        cameraController.CloudTransitionOut(() =>
        {
            //NOTE: The following code is run in the middle of the cloud transition
            
            //Switch cam to freecam
            cameraController.TransitionToFreeCam(true);
            cameraController.snakeCameraController.enabled = false;

            //Show idle environment
            cameraController.battleEnvironmentParent.SetActive(false);
            cameraController.idleEnvironmentParent.SetActive(true);
        }, () =>
        {
            //NOTE: The following code is run at the end of the cloud transition

            gameState = GameState.Idle;

            LeanTween.delayedCall(RoguelikeCameraController.generalDelayBetweenTransitions, () =>
            {
                //Show other open buttons
                TogglePlayerOptionButtons(true);
            
                //Show dialogue box
                dialogueSystemController.SetText(outputText);
                dialogueSystemController.ShowDialogueBox();
            });
        });
    }

    private bool doubleCheckInstar = false;
    public void Instar()
    {
        //Make sure the player can actually make choices
        if(!playerCanMakeChoices) return;

        //Make sure this option is actually one of the open ones
        if(!openButtons.Contains(PlayerOptionButtons.Instar)) return;

        if(doubleCheckInstar)
        {
            //TODO: INSTAR HERE
            Debug.Log("Instar");
        }
        else
        {
            dialogueBoxController.SetText(new List<string> {"Press again to confirm level end."});
            doubleCheckInstar = true;
        }
    }

    //Static method to add flesh
    public static void AddFlesh(int amount)
    {
        instance.currentFlesh += amount;

        StatDisplay.UpdateAllFleshFillBars_Static(); //Also update flesh bars
    }

    private void HideBites()
    {
        foreach(GameObject bite in biteList)
        {
            bite.SetActive(false);
        }
    }
    
    //Static method to do a bite and decrease number of bites remaining
    public static void ReduceBites()
    {
        instance.remainingBites--;
        instance.currentSpiderAlertness++;

        playerHasStartedLooting = true;
    }

    public static void ReloadBite_Static()
    {
        instance.ReloadBite();
    }

    //Sets current bites to max bites for the next looting
    public static void ResetBites()
    {
        instance.remainingBites = instance.maxBites;
    }

    //Moves bite back to proper position, shows if there are any bites remaining
    private void ReloadBite()
    {
        //Change text for bites remaining
        bitesRemainingText.text = "" + remainingBites;

        if(remainingBites > 0)
        {
            Vector3 offset = biteList[equippedBites[^1]].GetComponent<Fragment_Bite>().GetBiteOffset();
            biteList[equippedBites[^1]].GetComponent<RectTransform>().position = biteHomePosition.position + offset; //Reset position

            //Move into view
            biteBackgroundPopupTween = LeanTween.move(biteBackgroundRectTransform, biteBackgroundOriginalPos, biteBackgroundPopupAnimLength)
                .setEaseOutElastic()
                .setOnComplete(() =>
                {
                    biteList[equippedBites[^1]].SetActive(true); //Show after the popup is done
                })
                .id;
        }
        else
        {
            HideBites(); //immediately hide the bite

            //Hide background
            biteBackgroundPopupTween = LeanTween.move(biteBackgroundRectTransform, biteBackgroundOriginalPos - new Vector2(0, biteBackgroundRectTransform.sizeDelta.y), biteBackgroundPopupAnimLength)
                .setEaseInElastic()
                .id;
        }
    }

    //Called in fragments that change the current bite
    public void UpdateBite()
    {
        if(remainingBites <= 0) return;

        HideBites();

        biteList[equippedBites[^1]].SetActive(true);
        Vector3 offset = biteList[equippedBites[^1]].GetComponent<Fragment_Bite>().GetBiteOffset();
        biteList[equippedBites[^1]].GetComponent<RectTransform>().position = biteHomePosition.position + offset; //Reset position
    }

    //Method to calculate whether the caterpillar survives
    public bool CheckCaterpillarSurvives()
    {
        if(currentSpiderAlertness >= maxSpiderAlertness) //auto dead if spider is at max alertness
        {
            return false;
        }
        else //Otherwise, depends on player's stealth stat
        {
            if(UnityEngine.Random.Range(0f,1f) < stealthChance)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }

    public static GameObject GetCurrentBiteGO_Static()
    {
        return instance.biteList[instance.equippedBites[^1]];
    }

    public static float ChanceForSpiderToPursue()
    {
        return Math.Min((float)instance.currentSpiderAlertness / instance.maxSpiderAlertness, 1f);
    }
}

public enum PlayerOptionButtons
{
    Map,
    Inventory,
    Loot,
    Combat,
    Instar
}

public enum GameState
{
    Map,
    Idle,
    Inventory,
    Loot,
    Combat
}