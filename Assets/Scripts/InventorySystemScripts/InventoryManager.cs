using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class InventoryManager : GeneralInventoryStructure
{
    [Header("Inventory Manager Settings")]
    public int inventoryNumCols;
    public int inventoryNumRows;

    void Awake()
    {
        GenerateInventory(inventoryNumRows, inventoryNumCols);
    }
    
    //Overridden fragment placement
    public override void PlaceFragment(Fragment fragment, int x, int y)
    {
        base.PlaceFragment(fragment, x, y);
        //Activate fragment's effects
        fragment.ActivateEffects();
    }

    //Overridden fragment removal
    public override bool RemoveFragment(Fragment fragment)
    {
        if(base.RemoveFragment(fragment))
        {
            fragment.DeactivateEffects();
        }

        return true; //this does nothing
    }
}
