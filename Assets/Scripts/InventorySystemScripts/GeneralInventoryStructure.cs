using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GeneralInventoryStructure : MonoBehaviour
{
    [Header("Inventory Graphics")]
    public Transform inventorySystemParentTransform;
    [SerializeField] private GameObject inventoryLayoutGroupGO;
    [SerializeField] private GameObject inventorySlotGO;
    [SerializeField] private RectTransform backgroundGraphicGO;
    [SerializeField] private float fragmentSnapAnimLength;

    protected int gridNumCols;
    protected int gridNumRows;
    public float inventorySlotSize; //Note that this variable needs to be public for Fragment class access

    //Inventory logic
    private InventorySlot[,] inventorySlotArray;
    private List<Fragment> ownedFragments;

    public void GenerateInventory(int pNumRows, int pNumCols)
    {
        //Initialize dimensions
        gridNumCols = pNumCols;
        gridNumRows = pNumRows;

        //Initialize size of background graphic
        backgroundGraphicGO.sizeDelta = new Vector2(125 + pNumCols * inventorySlotSize, 120 + pNumRows * inventorySlotSize);

        //Initialize inventory array
        inventorySlotArray = new InventorySlot[gridNumRows, gridNumCols];

        //Generate inventory game objects and assign inventory array with the corresponding scripts of those game objects
        for(int row = 0; row < gridNumRows; row++)
        {
            for(int col = 0; col < gridNumCols; col++)
            {
                //Instantiate
                GameObject newInventorySlotGO = Instantiate(inventorySlotGO, inventoryLayoutGroupGO.transform);

                //Rename
                newInventorySlotGO.name = "Inventory slot at row " + row + ", col " + col;

                //Set in the 2d array data
                inventorySlotArray[row, col] = newInventorySlotGO.GetComponent<InventorySlot>();

                //Set edge-ness
                if(row == 0 || col == 0 || row == gridNumRows - 1 || col == gridNumCols - 1)
                {
                    inventorySlotArray[row, col].isOnEdge = true;
                }
            }
        }
        
        //Force reload layout group
        LayoutRebuilder.ForceRebuildLayoutImmediate(inventoryLayoutGroupGO.GetComponent<RectTransform>());
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

                    if(actualRow >= gridNumRows || actualCol >= gridNumCols)
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
    public virtual void PlaceFragment(Fragment fragment, int x, int y)
    {
        //Add to owned list
        ownedFragments.Add(fragment);

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

        //Determine placement of actual graphic for the fragment
        Vector2 placePosition = inventorySlotArray[x, y].gameObject.GetComponent<RectTransform>().position;
        placePosition += new Vector2(-inventorySlotSize / 2f, inventorySlotSize / 2f);

        //Snappily tween to position
        LeanTween.value(fragment.gameObject, (Vector2)fragment.gameObject.GetComponent<RectTransform>().position, placePosition, fragmentSnapAnimLength).setEaseOutElastic().setOnUpdate((Vector2 val) =>
        {
            fragment.gameObject.GetComponent<RectTransform>().position = val;
        });
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

        //Return backwards, since y position corresponds to row number, and x position corresponds to col number
        return new Vector2Int(y, x);
    }

    //Method to put all of the snapping together
    //Returns true if successful; false if not
    public bool SnapToGrid(Fragment fragment, int rowNum, int colNum)
    {
        if(CanPlaceFragment(fragment, rowNum, colNum))
        {
            PlaceFragment(fragment, rowNum, colNum);
            return true;
        }

        return false;
    }

    //Overloaded method to put all of the snapping together 
    //(does not take in explicit coordinates and instead calculates them itself)
    //Returns true if successful; false if not
    public bool SnapToGrid(Fragment fragment)
    {
        Vector2Int gridPosition = GetGridPosition(fragment); //x corresponds to row number, y corresponds to col number
        
        return SnapToGrid(fragment, gridPosition.x, gridPosition.y);
    }
    
    //Method to remove (more like "unstick") a fragment
    public virtual bool RemoveFragment(Fragment fragment)
    {
        //Tracker to keep account of whether there was anything to actually remove
        bool hasRemovedAnything = false;

        //Reset positional states
        fragment.isOnEdge = false;

        //Remove all instances of this fragment from the grid
        for(int row = 0; row < gridNumRows; row++)
        {
            for(int col = 0; col < gridNumCols; col++)
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
            //Remove this from the owned fragments list
            ownedFragments.Remove(fragment);

            //Will deactivate fragment effects if this is the inventory manager class
            return true;
        }
        return false;
    }

    //Method to highlight valid positions
    public void Highlight(Fragment fragment)
    {
        Vector2Int gridPosition = GetGridPosition(fragment);
        bool canPlaceFragment = CanPlaceFragment(fragment, gridPosition.x, gridPosition.y);

        //De-highlight all positions
        ClearHighlight();

        //Loop through necessary highlighted positions and highlight as needed
        for(int row = 0; row < fragment.shape.GetLength(0); row++)
        {
            for(int col = 0; col < fragment.shape.GetLength(1); col++)
            {
                //Check that this part of the fragment's shape does actually need space and is in bounds
                int actualRow = gridPosition.x + row;
                int actualCol = gridPosition.y + col;

                if(actualRow >= 0 && actualCol >= 0 && actualRow < gridNumRows && actualCol < gridNumCols && fragment.shape[row,col] == true)
                {
                    //Highlight the correct color
                    if(canPlaceFragment)
                    {
                        //Green
                        inventorySlotArray[actualRow, actualCol].HighlightValid();
                    }
                    else
                    {
                        //Red
                        inventorySlotArray[actualRow, actualCol].HighlightInvalid();
                    }
                }
            }
        }
    }

    //Method to de-highlight all inventory slots
    public void ClearHighlight()
    {
        for(int row = 0; row < gridNumRows; row++)
        {
            for(int col = 0; col < gridNumCols; col++)
            {
                inventorySlotArray[row, col].ResetHighlight();
            }
        }
    }
    
}
