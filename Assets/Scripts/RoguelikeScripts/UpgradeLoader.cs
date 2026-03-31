using UnityEngine;
using System.Collections;

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
                break;
            case "netpatrol":
                break;
            case "botanical":
                break;
            case "hilo":
                break;
            case "reef":
                break;
            case "diamond":
                break;
            case "her":
                break;
            case "point":
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
