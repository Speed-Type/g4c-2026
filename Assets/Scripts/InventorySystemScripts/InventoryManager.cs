using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    [Header("Inventory Graphics")]
    public GameObject inventoryLayoutGroupGO;
    public GameObject inventorySlotGO;

    [Header("Inventory Settings")]
    public int inventoryWidth;
    public int inventoryHeight;

    //Inventory logic
    private InventorySlot[,] inventorySlotArray = new InventorySlot[10,10]; //LEFT OFF HERE

    void Start()
    {
        //Generate inventory game objects and assign inventory array with the corresponding scripts of those game objects
        for(int i = 0; i < inventoryWidth; i++)
        {
            for(int j = 0; j < inventoryHeight; j++)
            {
                GameObject newInventorySlotGO = Instantiate(inventorySlotGO, inventoryLayoutGroupGO.transform);
                inventorySlotArray[i,j] = newInventorySlotGO.GetComponent<InventorySlot>();
            }
        }
    }

    
    void Update()
    {
        
    }
}
