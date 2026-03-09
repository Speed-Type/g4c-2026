using UnityEngine;
using System.Collections.Generic;

public class RoguelikeGameplayController : MonoBehaviour
{
    //Variables in Inspector
    [Header("Player Option GameObjects")]
    public GameObject showMapButton;
    public GameObject hideMapButton;
    public GameObject nodeMap;

    //Hidden variables
    private List<PlayerOptionButtons> openButtons = new List<PlayerOptionButtons>();

    //Method to update the buttons that should be open; called primarily by nodes
    public void SetOpenButtons(List<PlayerOptionButtons> newList)
    {
        openButtons = newList;

        //Immediately show player's options
        HideMap();
    }

    //Method to convert enum PlayerOptionButtons to actual GameObjects
    private GameObject playerOptionButtonsEnumToGameObject(PlayerOptionButtons button)
    {
        switch(button)
        {
            case PlayerOptionButtons.Map:
                return showMapButton;
            default:
                return null;
        }
    }

    //Method to show map (and by extension, hide open buttons)
    public void ShowMap()
    {
        //Show map
        nodeMap.SetActive(true);

        //Show "hide map" button
        hideMapButton.SetActive(true);

        //Hide other open buttons
        foreach(PlayerOptionButtons button in openButtons)
        {
            playerOptionButtonsEnumToGameObject(button).SetActive(false);
        }
    }

    //Method to hide map (and by extension, show open buttons)
    public void HideMap()
    {
        //Hide map
        nodeMap.SetActive(false);

        //Hide "hide map" button
        hideMapButton.SetActive(false);

        //Show other open buttons
        foreach(PlayerOptionButtons button in openButtons)
        {
            playerOptionButtonsEnumToGameObject(button).SetActive(true);
        }
    }

    void Awake()
    {
        //Enable/disable necessary buttons
        nodeMap.SetActive(true);
        hideMapButton.SetActive(true);
        showMapButton.SetActive(false);
    }
}
public enum PlayerOptionButtons
{
    Map
}