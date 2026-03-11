using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    [Header("Inventory UI")]
    public GameObject inventoryGridSpaceGO;

    //Inventory logic
    private InventorySlot[,] inventoryArray = new InventorySlot[10,10]; //LEFT OFF HERE
    private int inventoryWidth;
    private int inventoryHeight;

    void Awake()
    {
        //Generate inventory game objects and assign inventory array with the corresponding scripts of those game objects
        
    }

    
    void Update()
    {
        
    }
}
