using UnityEngine;
using System.Collections;
using System;

public class UpgradeLoader : MonoBehaviour
{
    void Start()
    {
        StartCoroutine(SlightPause());
    }

    IEnumerator SlightPause()
    {
        yield return new WaitForEndOfFrame();

        //Load in upgrades

        //Check which upgrades this stage should have
        switch(PlayerPrefs.GetInt("CurrentStage", 0))
        {
            case 0:
                //No upgrades
                break;
            case 2:
                //Should have upgrades from conservation round 1
                LoadUpgrades("Round1_");
                break;
            case 4:
                //Should have upgrades from conservation round 1 AND 2
                LoadUpgrades("Round1_");
                LoadUpgrades("Round2_");
                break;
        }
    }

    private void LoadUpgrades(string prefix)
    {
        //Load Malama Aina
        switch(PlayerPrefs.GetString(prefix + "VolunteerUpgrade", "null"))
        {
            case "kiawe":
                //Decrease enemy encounters
                NodeManager.instance.nodeWeights[3] = Math.Clamp(NodeManager.instance.nodeWeights[3] - 1, 0, 10000);
                break;
            case "netpatrol":
                //Increase bites
                RoguelikeGameplayController.instance.maxBites += 2;
                break;
            case "botanical":
                //Decrease required flesh
                RoguelikeGameplayController.instance.goalFlesh -= 15;
                break;
            case "hilo":
                //Increase spider alertness threshold
                RoguelikeGameplayController.instance.maxSpiderAlertness += 20;
                break;
            case "reef":
                //Upgrade three nodes
                for(int i = 0; i < 3; i++)
                {
                    if(NodeManager.instance.UpgradeRandomNode() == false)
                    {
                        i = 99999;
                    }
                }
                break;
            case "diamond":
                //Increase camo chance
                RoguelikeGameplayController.instance.stealthChance += 0.1f;
                break;
            case "her":
                //Increase card charge rate
                SnakeGameplayController.instance.chargeRate += 0.2f;
                break;
            case "point":
                //Increase battle length
                SnakeGameplayController.instance.bonusBattleTime += 10;
                break;
        }

        //Load email bonus (day-based, so don't need to add prefix)
        for(int i = 0; i < PlayerPrefs.GetInt("EmailBonus", 0); i++)
        {
            if(NodeManager.instance.UpgradeRandomNode() == false)
            {
                i = 99999;
            }
        }


    }
}
