using UnityEngine;
using System.Collections.Generic;

public class Node_Combat : Node
{
    //Combat nodes provides an enemy for the player to fight

    [Header("Odds Corresponding to BugLoot Enum")]
    public int[] bugSpawningOdds;
    private Bugs spawnedBug;
    private LootManager lootManager;

    private List<string> flavorText = new List<string> //USE [BugName] (CASE-SENSITIVE) AS PLACEHOLDER FOR THE BUG'S NAME
    {
        "The [BugName] is still alive. Will you engage?",
        "You find the [BugName] caught in the spider's web but otherwise untouched. What to do is up to you.",
        "This [BugName] could be dangerous, but it also has the flesh you need.",
        "The [BugName] is fresh, untouched by the Spider. It's yours to take, if you're willing to put in the work.",
        "Taking on this [BugName] is a risk, but surely one with great rewards."
    };

    protected override void Awake()
    {
        base.Awake();

        nodeType = NodeTypes.Combat;

        lootManager = FindFirstObjectByType<LootManager>();

        //Set the spawned bug

        //First, get the total odds
        int sum = 0;
        foreach(int odd in bugSpawningOdds)
        {
            sum += odd;
        }

        //Generate random number from [0, sum]
        float rand = Random.Range(0f, sum);
        float current = 0;

        for(int j = 0; j < bugSpawningOdds.Length; j++)
        {
            current += bugSpawningOdds[j];
            if(current >= rand)
            {
                //This is the correct index
                spawnedBug = (Bugs)j;
            }
        }
    }

    public override void VisitNode()
    {
        //Send request to NodeManager; if this node is not accessible, the rest of the function won't run
        if(!nodeManager.TryVisitNode(this))
        {
            return;
        }
        
        RoguelikeGameplayController.instance.SetOpenButtons(new List<PlayerOptionButtons> {PlayerOptionButtons.Map, PlayerOptionButtons.Inventory, PlayerOptionButtons.Combat});
        
        //Generate loot
        lootManager.GenerateLoot(spawnedBug);
    }

    public override void DoNodeEffects()
    {
        LeanTween.delayedCall(RoguelikeCameraController.freeCamTransitionTiming + 2 * RoguelikeCameraController.generalDelayBetweenTransitions, () =>
        {
            //The following code is run AFTER camera has returned to free cam view and paused the general delay time

            //Update node bg color
            SetNodeBGColorVisited();
            
            //Display random flavor text
            string encounterText = flavorText[Random.Range(0, flavorText.Count)];
            encounterText = encounterText.Replace("[BugName]", LootManager.BugEnumToBugName(spawnedBug));

            RoguelikeGameplayController.instance.dialogueBoxController.SetText(new List<string> {encounterText, "..."});
        });
    }

    public override GameObject GetBug()
    {
        //RETURN BUG MODEL CORRESPONDING TO THIS BUG LOOT OBJECT
        return lootManager.BugEnumToCombatModel(spawnedBug);
    }
}
