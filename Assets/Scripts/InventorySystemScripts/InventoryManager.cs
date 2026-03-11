using UnityEngine;
using UnityEngine.UI;

public class InventoryManager : MonoBehaviour
{
    [Header("Inventory Graphics")]
    public GameObject inventoryLayoutGroupGO;
    public GameObject inventorySlotGO;

    [Header("Inventory Settings")]
    public int inventoryWidth;
    public int inventoryHeight;

    //Inventory logic
    private InventorySlot[,] inventorySlotArray;

    void Start()
    {
        //Initialize inventory array
        inventorySlotArray = new InventorySlot[inventoryWidth, inventoryHeight];

        //Generate inventory game objects and assign inventory array with the corresponding scripts of those game objects
        for(int i = 0; i < inventoryWidth; i++)
        {
            for(int j = 0; j < inventoryHeight; j++)
            {
                GameObject newInventorySlotGO = Instantiate(inventorySlotGO, inventoryLayoutGroupGO.transform);
                inventorySlotArray[i,j] = newInventorySlotGO.GetComponent<InventorySlot>();

                //Set edges
                if(i == 0 || j == 0 || i == inventoryWidth - 1 || j == inventoryHeight - 1)
                {
                    inventorySlotArray[i,j].isOnEdge = true;
                }
            }
        }
    }

    //Method to check if the fragment can be placed at these coordinates
    private bool CanPlaceFragment(Fragment fragment, int x, int y)
    {
        for(int i = 0; i < fragment.shape.GetLength(0); i++)
        {
            for(int j = 0; j < fragment.shape.GetLength(1); j++)
            {
                //Check that this part of the fragment's shape does actually need space
                if(fragment.shape[i,j] == true)
                {
                    //Calculate whether this specific spot at (x+i), (y+j) is (1) in bounds and (2) empty
                    int posX = x + i;
                    int posY = y + j;

                    if(posX >= inventoryWidth || posY >= inventoryHeight)
                    {
                        return false;
                    }

                    if(inventorySlotArray[posX, posY].GetFragment() != null)
                    {
                        return false;
                    }
                }
            }
        }

        return true;
    }

    //Method to update the array with a new fragment
    public void PlaceFragment(Fragment fragment, int x, int y)
    {
        for (int i = 0; i < fragment.shape.GetLength(0); i++)
        {
            for (int j = 0; j < fragment.shape.GetLength(1); j++)
            {
                if(fragment.shape[i,j] == true)
                {
                    inventorySlotArray[x + i, y + j].SetFragment(fragment);
                }
            }
        }
    }
    
    void Update()
    {
        
    }
}
