using UnityEngine;

public class ConservationistUpgradeController : MonoBehaviour
{
    public InteractableObject door;

    void Start()
    {
        door.canBeInteracted = false;
    }

    // Update is called once per frame
    void Update()
    {
        
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
        
    }
}
