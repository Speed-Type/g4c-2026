using UnityEngine;
using System.Collections.Generic;

public class ConservationistUpgradeController : MonoBehaviour
{
    public static ConservationistUpgradeController instance;

    [Header("References")]
    public InteractableObject door;

    [Header("All Volunteer Options")]
    public int totalDailyVolunteerOptions;
    public List<WebBrowser.SiteOption> allVolunteerOptions = new();

    //Private variables
    public string selectedVolunteeringUpgradeID = null;
    [HideInInspector] public List<WebBrowser.SiteOption> dailyVolunteerOptions;

    void Awake()
    {
        instance = this;

        //Set today's daily volunteer options
        dailyVolunteerOptions = new List<WebBrowser.SiteOption>();
        for(int i = 0; i < totalDailyVolunteerOptions; i++)
        {
            WebBrowser.SiteOption siteOption = allVolunteerOptions[Random.Range(0, allVolunteerOptions.Count - 1)];
            allVolunteerOptions.Remove(siteOption);
            dailyVolunteerOptions.Add(siteOption);
        }
    }

    void Start()
    {
        door.canBeInteracted = false;
    }

    public void ConfirmVolunteering(string id)
    {
        //First, check off the objective
        ObjectiveManager.instance.CompleteObjective("volunteering");

        //Save the id of the confirmed volunteering upgrade
        selectedVolunteeringUpgradeID = id;
    }

    //Method called by objective manager when all mandatory objectives are complete
    public void UnlockDoor()
    {
        door.canBeInteracted = true;
    }

    //Method called by door
    //Pushes all upgrades to player prefs
    public void SaveUpgrades()
    {
        //Set selected upgrade data
        PlayerPrefs.SetString("VolunteerUpgrade", selectedVolunteeringUpgradeID);

        //Increase level
        PlayerPrefs.SetInt("CurrentStage", PlayerPrefs.GetInt("CurrentStage", 0) + 1);
    }
}
