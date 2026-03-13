using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class InventoryManager : MonoBehaviour
{
    [Header("Inventory Graphics")]
    public GameObject inventoryLayoutGroupGO;
    public GameObject inventorySlotGO;

    [Header("Inventory Settings")]
    public int inventoryNumCols;
    public int inventoryNumRows;
    public float inventorySlotSize;

    //Inventory logic
    private InventorySlot[,] inventorySlotArray;

    void Awake()
    {
        //Initialize inventory array
        inventorySlotArray = new InventorySlot[inventoryNumRows, inventoryNumCols];

        //Generate inventory game objects and assign inventory array with the corresponding scripts of those game objects
        for(int row = 0; row < inventoryNumRows; row++)
        {
            for(int col = 0; col < inventoryNumCols; col++)
            {
                GameObject newInventorySlotGO = Instantiate(inventorySlotGO, inventoryLayoutGroupGO.transform);
                newInventorySlotGO.name = "Inventory slot at row " + row + ", col " + col;
                inventorySlotArray[row, col] = newInventorySlotGO.GetComponent<InventorySlot>();

                //Set edges
                if(row == 0 || col == 0 || row == inventoryNumRows - 1 || col == inventoryNumCols - 1)
                {
                    inventorySlotArray[row, col].isOnEdge = true;
                }
            }
        }
    }

    //Method to check if the fragment can be placed at these coordinates
    private bool CanPlaceFragment(Fragment fragment, int originRow, int originCol)
    {
        //Check that x and y are within bounds
        if(originRow < 0 || originCol < 0)
        {
            return false;
        }

        for(int row = 0; row < fragment.shape.GetLength(0); row++)
        {
            for(int col = 0; col < fragment.shape.GetLength(1); col++)
            {
                //Check that this part of the fragment's shape does actually need space
                if(fragment.shape[row,col] == true)
                {
                    //Calculate whether this specific spot at (x+i), (y+j) is (1) in bounds and (2) empty
                    int actualRow = originRow + row;
                    int actualCol = originCol + col;

                    if(actualRow >= inventoryNumRows || actualCol >= inventoryNumCols)
                    {
                        return false;
                    }

                    if(inventorySlotArray[actualRow, actualCol].GetFragment() != null)
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
        return new Vector2Int(y, x);
    }

    //Method to put all of the snapping together
    //Returns true if successful; false if not
    public bool SnapToGrid(Fragment fragment)
    {
        Vector2Int gridPosition = GetGridPosition(fragment);
        if(CanPlaceFragment(fragment, gridPosition.x, gridPosition.y))
        {
            PlaceFragment(fragment, gridPosition.x, gridPosition.y);
            return true;
        }

        return false;
    }
    
    //Method to remove (more like "unstick") a fragment
    public void RemoveFragment(Fragment fragment)
    {
        //Tracker to keep account of whether there was anything to actually remove
        bool hasRemovedAnything = false;

        //Reset positional states
        fragment.isOnEdge = false;

        //Remove all instances of this fragment from the grid
        for(int row = 0; row < inventoryNumRows; row++)
        {
            for(int col = 0; col < inventoryNumCols; col++)
            {
                if(inventorySlotArray[row, col].GetFragment() == fragment)
                {
                    inventorySlotArray[row, col].SetFragment(null);
                    hasRemovedAnything = true;
                }
            }
        }

        if(hasRemovedAnything)
        {
            //Deactivate fragment's effects
            fragment.DeactivateEffects();
        }
    }
}
