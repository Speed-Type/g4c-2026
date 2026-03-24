using UnityEngine;
using System.Collections.Generic;

public class Node_Scavenge : Node
{
    //Scavenge node contains loot for the player
    [Header("Odds Corresponding to BugLoot Enum")]
    public int[] bugLootOdds;
    private Bugs spawnedBug;
    private LootManager lootManager;

    private List<string> flavorText = new List<string> //USE [BugName] (CASE-SENSITIVE) AS PLACEHOLDER FOR THE BUG'S NAME
    {
        "The [BugName] is trapped. Easy prey.",
        "You find the [BugName] wrapped in the spider's webbing. Surely there's something left of it to take.",
        "The [BugName] is long chewed-through, but maybe there's some flesh left for you.",
        "This [BugName]'s remains would go to waste anyway. Might as well take what you can.",
        "Well, it's your namesake after all. Go collect the bones of that [BugName].",
        "That [BugName]'s limbs would make excellent decoration for your silk casing.",
        "This [BugName] still has some goodies left on it. You'll have to be strategic with extracting it though."
    };

    protected override void Awake()
    {
        base.Awake();

        nodeType = NodeTypes.Scavenge;

        lootManager = FindFirstObjectByType<LootManager>();

        //Set the spawned bug

        //First, get the total odds
        int sum = 0;
        foreach(int odd in bugLootOdds)
        {
            sum += odd;
        }

        //Generate random number from [0, sum]
        float rand = Random.Range(0f, sum);
        float current = 0;

        for(int j = 0; j < bugLootOdds.Length; j++)
        {
            current += bugLootOdds[j];
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
        
        RoguelikeGameplayController.instance.SetOpenButtons(new List<PlayerOptionButtons> {PlayerOptionButtons.Map, PlayerOptionButtons.Inventory, PlayerOptionButtons.Loot});
        
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
        return lootManager.BugEnumToScavengeModel(spawnedBug);
    }
}
