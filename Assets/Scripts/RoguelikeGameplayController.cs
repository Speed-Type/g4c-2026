using UnityEngine;
using System.Collections.Generic;

public class RoguelikeGameplayController : MonoBehaviour
{
    //Variables in Inspector
    [Header("Player Option GameObjects")]
    public GameObject continueButton;
    public GameObject showMapButton;
    public GameObject nodeMap;

    //Hidden variables
    private List<PlayerOptionButtons> openButtons = new List<PlayerOptionButtons>();

    //Method to update the buttons that should be open; called primarily by nodes
    public void AddOpenButtons(List<PlayerOptionButtons> newList)
    {
        openButtons = newList;
    }

    //Method to show map (and by extension, hide open buttons)
    public void ShowMap()
    {
        
    }

    //Method to hide map (and by extension, show open buttons)
    public void HideMap()
    {
        
    }

    void Start()
    {
        
    }

    void Update()
    {
        
    }
}
public enum PlayerOptionButtons
{
    Map
}