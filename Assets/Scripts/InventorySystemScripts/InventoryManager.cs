using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class InventoryManager : MonoBehaviour
{
    [Header("Inventory Graphics")]
    public GameObject inventoryLayoutGroupGO;
    public GameObject inventorySlotGO;

    [Header("Inventory Settings")]
    public int inventoryWidth;
    public int inventoryHeight;
    public float inventorySlotSize;

    //Inventory logic
    private InventorySlot[,] inventorySlotArray;

    void Awake()
    {
        //Initialize inventory array
        inventorySlotArray = new InventorySlot[inventoryWidth, inventoryHeight];

        //Generate inventory game objects and assign inventory array with the corresponding scripts of those game objects
        for(int y = 0; y < inventoryHeight; y++)
        {
            for(int x = 0; x < inventoryWidth; x++)
            {
                GameObject newInventorySlotGO = Instantiate(inventorySlotGO, inventoryLayoutGroupGO.transform);
                newInventorySlotGO.name = "Inventory slot " + x + ", " + y;
                inventorySlotArray[x, y] = newInventorySlotGO.GetComponent<InventorySlot>();

                //Set edges
                if(y == 0 || x == 0 || y == inventoryHeight - 1 || x == inventoryWidth - 1)
                {
                    inventorySlotArray[x, y].isOnEdge = true;
                }
            }
        }
    }

    //Method to check if the fragment can be placed at these coordinates
    private bool CanPlaceFragment(Fragment fragment, int x, int y)
    {
        //Check that x and y are within bounds
        if(x < 0 || y < 0)
        {
            return false;
        }

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
        //Loop through necessary spots, relative to x and y, and assign fragments
        for(int i = 0; i < fragment.shape.GetLength(0); i++)
        {
            for(int j = 0; j < fragment.shape.GetLength(1); j++)
            {
                if(fragment.shape[i,j] == true)
                {
                    inventorySlotArray[x + i, y + j].SetFragment(fragment);

                    //Set edge-ness
                    if(inventorySlotArray[x + i, y + j].isOnEdge == true)
                    {
                        fragment.isOnEdge = true;
                    }
                }
            }
        }

        //Activate fragment's effects
        fragment.ActivateEffects();

        //Determine placement of actual graphic for the fragment
        Vector2 placePosition = inventorySlotArray[x, y].gameObject.GetComponent<RectTransform>().position;
        placePosition += new Vector2(-inventorySlotSize / 2f, inventorySlotSize / 2f);
        fragment.gameObject.GetComponent<RectTransform>().position = placePosition;
    }

    //Method to convert mouse position into grid position, to help with snapping item to array
    Vector2Int GetGridPosition(Fragment fragment)
    {
        //Calculate relative position
        Vector2 fragmentPosition = fragment.gameObject.transform.position;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            inventoryLayoutGroupGO.GetComponent<RectTransform>(),
            fragmentPosition,
            null,
            out Vector2 relativeFragmentPosition
        );
        relativeFragmentPosition.y *= -1f;

        //Adjust to top left corner
        relativeFragmentPosition.x -= inventoryLayoutGroupGO.GetComponent<RectTransform>().rect.xMin;

        //Calculate decimal grid position, then adjust for ease of snapping
        relativeFragmentPosition.x = relativeFragmentPosition.x / inventorySlotSize;
        relativeFragmentPosition.y = relativeFragmentPosition.y / inventorySlotSize;

        relativeFragmentPosition.x += 0.5f;
        relativeFragmentPosition.y += 0.5f;

        //Snap to a grid spot (floor to int)
        int x = Mathf.FloorToInt(relativeFragmentPosition.x);
        int y = Mathf.FloorToInt(relativeFragmentPosition.y);
        return new Vector2Int(x, y);
    }

    //Method to put all of the snapping together
    public void SnapToGrid(Fragment fragment)
    {
        Vector2Int gridPosition = GetGridPosition(fragment);
        if(CanPlaceFragment(fragment, gridPosition.x, gridPosition.y))
        {
            PlaceFragment(fragment, gridPosition.x, gridPosition.y);
        }
    }
    
    //Method to remove (more like "unstick") a fragment
    public void RemoveFragment(Fragment fragment)
    {
        //Reset positional states
        fragment.isOnEdge = false;

        //Deactivate fragment's effects
        fragment.DeactivateEffects();

        //Remove all instances of this fragment from the grid
        for(int i = 0; i < inventoryWidth; i++)
        {
            for(int j = 0; j < inventoryHeight; j++)
            {
                if(inventorySlotArray[i,j].GetFragment() == fragment)
                {
                    inventorySlotArray[i,j].SetFragment(null);
                }
            }
        }
    }
}
