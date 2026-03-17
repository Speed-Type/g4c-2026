using UnityEngine;
using System.Collections.Generic;

public class Node_Scavenge : Node
{
    //Scavenge node contains loot for the player

    // private List<string> flavorText = new List<string>
    // {
    //     "Nothing here for a very hungry caterpillar.",
    //     "An empty node.",
    //     "You're hungry. You're always hungry.",
    //     "Nothing of interest here.",
    //     "You should keep moving.",
    //     "Fun fact: Scientists formally described the bone collector in a 2010 paper led by entomologist Daniel Rubinoff.",
    //     "Fun fact: Bone collector caterpillars are cannibalistic. You sick freak.",
    //     "The only chance you have of making a living in this situation is to decorate or die.",
    //     "Fun fact: Bone collectors belong to the genus Hyposmocoma, which usually have fancy cases.",
    //     "Nothing here.",
    //     "Nothing.",
    //     "A cool breeze passes over the web. Nothing of interest here.",
    //     "There's nothing here. You should keep searching.",
    //     "Fun fact: This game was made in just three weeks.",
    //     "You feel tired, but you press on.",
    //     "You watch for the spider, but it's not here.",
    //     "No enemies in sight.",
    //     "There's no food here for you.",
    //     "You hear the rustling of some mammal. Otherwise, there's nothing here.",
    //     "The atmosphere is calm. At least for now, you're safe.",
    //     "The air is quiet. There's nothing else alive here but you. Nothing worth eating anyway.",
    //     "You don't feel quite fat enough yet. You won't, until you're ready to metamorphose.",
    //     "You take care to crawl quietly, so as not to alert the spider with vibrations.",
    //     "There aren't enough bones here to appease a bone collector.",
    //     "You could go for a snack.",
    //     "No meals to be found here.",
    //     "Your silk case could always use an upgrade. Best keep searching.",
    //     "You wonder what you'll look like as a moth. But you need to keep moving to get to find out.",
    //     "A twig snaps in the underbrush. Whatever it is, it shouldn't reach you here.",
    //     "You're so hungry you could eat a horsefly.",
    //     "You feel old. In fact, bone collector caterpillars may have existed for over 6 million years. I hope that'll be the case far into the future.",
    //     "You lick your mandibles hungrily, but there's no snacks for you here.",
    //     "Fun fact: Bone collector caterpillars have their silk-producing spinnerets in the mouth region. Maybe you and the spider are only a little different.",
    //     "You rub your true legs together (The ones up close to your head). You're itching for a meal.",
    //     "...",
    //     "...",
    //     "...",
    //     "...",
    //     "...",
    //     "You can't wait to become a pupa and grow your wings. You just have to find more food.",
    //     "You wonder what the world will be like, even just a generation into the future.",
    //     "The Hawaiian weather is just wonderful. It almost distracts you from the fact that you're stealing food from under a spider's nose."
    // };

    void Awake()
    {
        //flavorText.Add("There is a 1 in " + (flavorText.Count + 1) + " chance of getting this flavor text.");
    }

    public override void VisitNode()
    {
        //Send request to NodeManager; if this node is not accessible, the rest of the function won't run
        if(!nodeManager.TryVisitNode(this))
        {
            return;
        }
        
        nodeManager.rgController.SetOpenButtons(new List<PlayerOptionButtons> {PlayerOptionButtons.Map, PlayerOptionButtons.Inventory, PlayerOptionButtons.Loot});
        
        //Generate loot
        FindFirstObjectByType<LootManager>().GenerateLoot(LootTables.AfricanBigHeadedAnt); //CHANGE LATER

        LeanTween.delayedCall(RoguelikeCameraController.freeCamTransitionTiming + 2 * RoguelikeCameraController.generalDelayBetweenTransitions + NodeManager.playerTravelAnimTime, () =>
        {
            //The following code is run AFTER camera has returned to free cam view and paused the general delay time

            //Display random flavor text
            //nodeManager.rgController.dialogueBoxController.SetText(new List<string> {flavorText[Random.Range(0, flavorText.Count)], "..."});
            nodeManager.rgController.dialogueBoxController.SetText(new List<string> {"Temp text for found bug", "..."});
        });
    }
}
