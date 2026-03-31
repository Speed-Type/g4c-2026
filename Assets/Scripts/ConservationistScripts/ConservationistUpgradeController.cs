using UnityEngine;
using System.Collections.Generic;

public class ConservationistUpgradeController : MonoBehaviour
{
    public static ConservationistUpgradeController instance;

    [Header("References")]
    public InteractableObject door;

    [Header("Scene Transition")]
    public SceneTransitionHandler sceneTransitionHandler;

    [Header("All Volunteer Options")]
    public int totalDailyVolunteerOptions;
    public List<WebBrowser.SiteOption> allVolunteerOptions = new();

    [Header("All Emails")]
    public int totalDailyEmails;
    public List<EmailClient.Email> allEmails = new();

    //Internal variables
    public string selectedVolunteeringUpgradeID = null;
    [HideInInspector] public List<WebBrowser.SiteOption> dailyVolunteerOptions;
    
    public int totalCorrectEmails;
    [HideInInspector] public List<EmailClient.Email> dailyEmails;

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

        //Set today's daily emails
        dailyEmails = new List<EmailClient.Email>();
        for(int i = 0; i < totalDailyVolunteerOptions; i++)
        {
            EmailClient.Email email = allEmails[Random.Range(0, allEmails.Count - 1)];
            allEmails.Remove(email);
            dailyEmails.Add(email);
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
        //Figure out prefix
        string prefix;
        if(PlayerPrefs.GetInt("CurrentStage", 0) == 1)
        {
            prefix = "Round1_";
        }
        else
        {
            prefix = "Round2_";
        }

        //ROUND-SPECIFIC UPGRADES

        //Set selected upgrade data
        PlayerPrefs.SetString(prefix + "VolunteerUpgrade", selectedVolunteeringUpgradeID);

        //ROUND-AGNOSTIC UPGRADES (as in, only lasts for the next bone collector round)
        PlayerPrefs.SetInt("EmailBonus", totalCorrectEmails);

        //Increase level
        PlayerPrefs.SetInt("CurrentStage", PlayerPrefs.GetInt("CurrentStage", 0) + 1);

        //Call scene transition
        sceneTransitionHandler.SceneTransitionToScene("NextStage");
    }
}
