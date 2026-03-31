using UnityEngine;
using System.Collections.Generic;
using TMPro;
using System;
using System.Collections;

public class RoguelikeGameplayController : MonoBehaviour
{
    [Header("Important Controllers and Parents")]
    public DialogueBoxController dialogueBoxController;
    public SnakeGameplayController snakeGameplayController;
    public SceneTransitionHandler sceneTransitionHandler;

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
    public CanvasGroup inventorySystemCanvasGroup;
    public CanvasGroup lootSystemCanvasGroup;
    public GameObject statSystemGO;
    [SerializeField] private GameObject nodeMapGO;
    [SerializeField] private GameObject combatUI;
    
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
    [SerializeField] private TMP_Text canSwapBitesIndicator;

    [Header("Camera Stuff")]
    public RoguelikeCameraController cameraController;
    
    //Hidden variables
    private List<PlayerOptionButtons> openButtons = new List<PlayerOptionButtons>();
    private List<PlayerOptionButtons> allButtons = new List<PlayerOptionButtons>();
    public GeneralInventoryStructure inventoryManager;
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
        equippedBites = new List<int> {0};
    }

    void Start()
    {
        //Enable/disable necessary systems
        nodeMapGO.SetActive(true);
        SetGroup(inventorySystemCanvasGroup, false);
        SetGroup(lootSystemCanvasGroup, false);
        statSystemGO.SetActive(false);
        HideBites();
        combatUI.SetActive(false);
        SetPlayerHasStartedLooting(false);

        //Disable all buttons
        TogglePlayerOptionButtons(false);

        //Load in saved stuffs
        StartCoroutine(LoadAfterLayout());
    }

    private IEnumerator LoadAfterLayout()
    {
        //Wait one frame so LayoutRebuilder has processed
        yield return new WaitForEndOfFrame();

        SaveSystem.Load(inventoryManager);
        
        //Set number of bites
        ResetBites();
        HideBites();
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
        SetGroup(inventorySystemCanvasGroup, true);
        dialogueSystemController.HideDialogueBox();
        statSystemGO.SetActive(true);
        StatDisplay.UpdateAllFleshFillBars_Static();
        StatDisplay.UpdateStats_Static();
        NodeManager.instance.HideEnemyBug();

        //Update camera
        cameraController.ShiftFreecamFocusToCaterpillar();

        //Update game state
        gameState = GameState.Inventory;

        //Tutorial for inventory
        if(PlayerPrefs.GetInt("Tutorial_Inventory", 0) == 0)
        {
            PlayerPrefs.SetInt("Tutorial_Inventory", 1);
            TutorialPopup.Show("Inventory", "Your case is your best chance at survival. By weaving the fragments of enemies "
            + "into your silk, you can gain various abilities and boosts that will be more than necessary to survive.\n\n"
            + "Fact Check: The stat-boosting abilities are perhaps less true in real life, but it's still a sick way to camouflage.");
        }
    }

    public void HideInventory()
    {
        //Update graphics
        TogglePlayerOptionButtons(true);
        SetGroup(inventorySystemCanvasGroup, false);
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
        SetGroup(lootSystemCanvasGroup, true);
        SetGroup(inventorySystemCanvasGroup, true);
        dialogueSystemController.HideDialogueBox();
        hideInventorySystemButton.SetActive(false);
        StatDisplay.UpdateAllFleshFillBars_Static();

        //Prepare bite system
        LeanTween.cancel(biteBackgroundPopupTween); //cancel current tween to avoid overlap
        biteBackgroundRectTransform.localPosition = biteBackgroundOriginalPos - new Vector2(0, biteBackgroundRectTransform.sizeDelta.y); //TODO: reset bg position to a lower position
        ReloadBite();

        //Update game state
        gameState = GameState.Loot;

        if(PlayerPrefs.GetInt("Tutorial_Looting", 0) == 0)
        {
            PlayerPrefs.SetInt("Tutorial_Looting", 1);
            TutorialPopup.Show("Looting", "Looting bodies is your whole thing. Most of the ones you find lying around will "
            + "be half-eaten by the Spider, but you can still get the flesh you need from them. More importantly though, "
            + "these bodies have valuable fragments.\n\nTo take things from a body, you need to carefully bite off what you want. "
            + "To remove a fragment, you need to get rid of all the surrounding flesh. Flesh is soft and goes down in one bite though. "
            + "Start by dragging and dropping your mandibles. Be strategic!");
        }
    }

    public void HideLoot()
    {
        //Update graphics
        TogglePlayerOptionButtons(true);
        SetGroup(lootSystemCanvasGroup, false);
        SetGroup(inventorySystemCanvasGroup, false);
        hideInventorySystemButton.SetActive(true);
        dialogueSystemController.ShowDialogueBox();
        HideBites();

        LeanTween.cancel(biteBackgroundPopupTween); //cancel current tween to avoid overlap

        //Update game state
        gameState = GameState.Idle;
    }

    private bool doubleCheckCombat = false;
    public void StartCombat()
    {
        //Make sure the player can actually make choices
        if(!playerCanMakeChoices) return;

        doubleCheckInstar = false;

        //Make sure this option is actually one of the open ones
        if(!openButtons.Contains(PlayerOptionButtons.Combat)) return;

        playerCanMakeChoices = false;

        //Make sure this option is actually one of the open ones
        if(!openButtons.Contains(PlayerOptionButtons.Combat)) return;

        if(doubleCheckCombat)
        {
            gameState = GameState.Combat;

            //Set enemy bug
            snakeGameplayController.enemyBugEnum = NodeManager.instance.playerNode.GetComponent<Node_Combat>().spawnedBug;

            //Switch camera
            cameraController.SwitchToCombatCam(() => {snakeGameplayController.PlayGame(); combatUI.SetActive(true);});

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

    private List<string> outputText;
    public void EndCombat(bool playerWon)
    {
        //Set results text as returned from Node_Combat method
        outputText = NodeManager.instance.playerNode.gameObject.GetComponent<Node_Combat>().GetResultsText(playerWon);

        if(playerWon) //Case 1: Player wins
        {
            //Change node to scavenge node
            NodeManager.instance.ReplaceNode(NodeManager.instance.playerNode.gameObject, NodeTypes.Scavenge);

            //Upgrade to rare loot odds for this scavenge node
            NodeManager.instance.playerNode.UpgradeLoot();
        }
        else //Case 2: Player loses
        {
            //Change node to empty node
            NodeManager.instance.ReplaceNode(NodeManager.instance.playerNode.gameObject, NodeTypes.Empty);
        }

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

            //Hide combat UI
            combatUI.SetActive(false);

            //Show idle environment
            cameraController.battleEnvironmentParent.SetActive(false);
            cameraController.idleEnvironmentParent.SetActive(true);

            //Destroy enemy bug
            Destroy(snakeGameplayController.snakeEnemy.gameObject);

            //Take all the damage from the combat (Must call it here so that the screenshake doesn't just go crazy)
            int damagedFragments = 0;
            int shatteredFragments = 0;
            int fleshLost = 0;
            for(int i = 0; i < snakeGameplayController.totalDamage; i++)
            {
                Vector3Int result = inventoryManager.TakeBattleDamage();
                damagedFragments += result.x;
                shatteredFragments += result.y;
                fleshLost += result.z;
            }
            
            //Add results for hits
            if(snakeGameplayController.totalDamage > 0)
            {
                string temp = "You took " + snakeGameplayController.totalDamage + " point" + ((snakeGameplayController.totalDamage == 1) ? "" : "s") + " of damage.\n";
                if(damagedFragments != 0) temp += damagedFragments + " fragment" + ((damagedFragments == 1) ? " was" : "s were") + " damaged.\n";
                if(shatteredFragments != 0) temp += shatteredFragments + " fragment" + ((shatteredFragments == 1) ? " was" : "s were") + " broken.\n";
                if(fleshLost != 0) temp += fleshLost + " flesh was lost.";

                outputText.Add(temp);
            }
            else
            {
                outputText.Add("You finished that encounter without taking any hits!");
            }

            if(!playerWon)
            {
                //Increase spider alertness
                currentSpiderAlertness += 10;

                //Add to results text
                outputText.Add("The Spider gained 10 alertness.");
            }

            outputText.Add("...");
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

    public static bool CanInstar()
    {
        if(instance.currentFlesh >= instance.goalFlesh)
        {
            //Tutorial for instar
            if(PlayerPrefs.GetInt("Tutorial_Instar", 0) == 0)
            {
                PlayerPrefs.SetInt("Tutorial_Instar", 1);
                TutorialPopup.Show("Instar", "It looks like you've got all the flesh you need! So long as this stays true, "
                + "you can Instar and move on to the next phase of your life.\n\nOr... you could risk it for more fragments. "
                + "You do get to keep them to the next stage.");
            }

            return true;
        }
        return false;
    }

    private bool doubleCheckInstar = false;
    public void Instar()
    {
        //Make sure the player can actually make choices
        if(!playerCanMakeChoices) return;

        doubleCheckCombat = false;

        //Make sure this option is actually one of the open ones
        if(!openButtons.Contains(PlayerOptionButtons.Instar)) return;

        if(doubleCheckInstar)
        {
            //Save
            SaveSystem.Save(inventoryManager);

            switch(PlayerPrefs.GetInt("CurrentStage", 0))
            {
                case 0:
                    //Increment current stage
                    PlayerPrefs.SetInt("CurrentStage", 1);
                    
                    TutorialPopup.Show("Act I Complete", "Congratulations! You've successfully made it through the first "
                    + "stage of bone collector life. But before you continue, you'll need a few upgrades...");
                    
                    sceneTransitionHandler.SceneTransitionToScene("NextStage");
                    break;
                case 2:
                    PlayerPrefs.SetInt("CurrentStage", 3);
                    
                    TutorialPopup.Show("Act III Complete", "Few bugs make it this far, and even fewer are carnivorous, "
                    + "cannibalistic corpse-looters. Soon, you'll be on your way to blooming into a moth. Just one final "
                    + "bit of human assistance remains.");
                    
                    sceneTransitionHandler.SceneTransitionToScene("NextStage");
                    break;
                case 4:
                    //Delete player save and move them back to the main menu
                    PlayerPrefs.SetInt("HasStartedGame", 0);
                    SaveSystem.DeleteSave();

                    TutorialPopup.Show("Act V Complete", "Truly, thank you. For carrying this little corpse-looter to "
                    + "magnificence, and for taking the time to at least, if nothing else, simulate the efforts that are "
                    + "required to protect the rainforests and reefs that creatures as strange and brutal as this call home."
                    + "\n\nTo be fair, I did mean to add an animation for the actual metamorphosis, but I ran out of time."
                    + "\nThank you for sharing in my labor of love. Let us give back tenfold to the world that gives so much to us.");

                    sceneTransitionHandler.SceneTransitionToScene("MainMenu");
                    break;
            }
        }
        else
        {
            dialogueBoxController.SetText(new List<string> {"Press again to confirm stage end."});
            doubleCheckInstar = true;
        }
    }

    //Static method to add flesh
    public static void AddFlesh(int amount)
    {
        instance.currentFlesh += amount;
        instance.currentFlesh = Math.Max(instance.currentFlesh, 0); //Keep it nonneg

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

        SetPlayerHasStartedLooting(true);
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
    
        //Tutorial for switching bites
        if(PlayerPrefs.GetInt("Tutorial_SwitchBites", 0) == 0)
        {
            PlayerPrefs.SetInt("Tutorial_SwitchBites", 1);
            TutorialPopup.Show("Switching Bites", "Certain fragments allow you to switch your bite's shape and damage "
            + "to meet all your corpse-looting needs. Once you take your first bite into a body, you can't switch "
            + "that bite until you find the next meal, so be strategic.\nThe \"Bite unlocked\" indicator should help you keep "
            + "track of whether or not you can switch bites.");
        }
    }

    public static void SetPlayerHasStartedLooting(bool val)
    {
        playerHasStartedLooting = val;
        instance.canSwapBitesIndicator.text = val ? "[Bite locked]" : "[Bite unlocked]";
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

    private void SetGroup(CanvasGroup group, bool interactable)
    {
        group.alpha = interactable ? 1 : 0;
        group.interactable = interactable;
        group.blocksRaycasts = interactable;
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