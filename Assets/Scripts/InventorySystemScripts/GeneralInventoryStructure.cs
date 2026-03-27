using System;
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
    [SerializeField] private float gridBorderHorizontalPadding;
    [SerializeField] private float gridBorderVerticalPadding;

    protected int gridNumCols;
    protected int gridNumRows;
    public float inventorySlotSize; //Note that this variable needs to be public for Fragment class access

    //Inventory logic
    protected InventorySlot[,] inventorySlotArray;

    protected virtual void Awake()
    {
        
    }

    public void GenerateInventory(int pNumRows, int pNumCols)
    {
        //Initialize dimensions
        gridNumCols = pNumCols;
        gridNumRows = pNumRows;

        //Initialize size of background graphic
        backgroundGraphicGO.sizeDelta = new Vector2(gridBorderHorizontalPadding + pNumCols * inventorySlotSize, gridBorderVerticalPadding + pNumRows * inventorySlotSize);

        //Delete any previous inventory slot objects (NOTE: This is different than fragments; those are destroyed in Fragment.FilterFragments() static method)
        if(inventorySlotArray != null)
        {
            for(int i = 0; i < inventorySlotArray.GetLength(0); i++)
            {
                for(int j = 0; j < inventorySlotArray.GetLength(1); j++)
                {
                    inventorySlotArray[i,j].transform.SetParent(transform);
                    Destroy(inventorySlotArray[i,j].gameObject);
                }
            }
        }

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

    //Method to check for adjacent flesh for a fragment
    public bool IsAdjacentFlesh(Fragment fragmentToCheck)
    {
        //Get position of fragment
        Vector2Int gridPosition = GetGridPosition(fragmentToCheck); //x corresponds to row number, y corresponds to col number
        
        int rowOrigin = gridPosition.x;
        int colOrigin = gridPosition.y;

        //Loop through this fragment's occupied positions
        for(int row = 0; row < fragmentToCheck.shape.GetLength(0); row++)
        {
            for(int col = 0; col < fragmentToCheck.shape.GetLength(1); col++)
            {
                //Check that this part of the fragment's shape is actually occupied
                if(fragmentToCheck.shape[row,col] == true)
                {
                    //Check adjacent squares for flesh
                    int rowToCheck = row + rowOrigin;
                    int colToCheck = col + colOrigin;
                    if(rowToCheck - 1 >= 0 && inventorySlotArray[rowToCheck-1,colToCheck].GetFragment() != null && inventorySlotArray[rowToCheck-1,colToCheck].GetFragment().IsFlesh()) return true;
                    if(colToCheck - 1 >= 0 && inventorySlotArray[rowToCheck,colToCheck-1].GetFragment() != null && inventorySlotArray[rowToCheck,colToCheck-1].GetFragment().IsFlesh()) return true;
                    if(rowToCheck + 1 < inventorySlotArray.GetLength(0) && inventorySlotArray[rowToCheck+1,colToCheck].GetFragment() != null && inventorySlotArray[rowToCheck+1,colToCheck].GetFragment().IsFlesh()) return true;
                    if(colToCheck + 1 < inventorySlotArray.GetLength(1) && inventorySlotArray[rowToCheck,colToCheck+1].GetFragment() != null && inventorySlotArray[rowToCheck,colToCheck+1].GetFragment().IsFlesh()) return true;
                }
            }
        }

        //If no flesh found, return false
        return false;
    }

    //Method to check for adjacent fragments for one single inventory slot (used by flesh for drawing connectors)
    public Vector4 GetAdjacentFragmentsFromSingleSlot(int row, int col)
    {
        Vector4 result = new Vector4();
        if(row - 1 >= 0 && inventorySlotArray[row-1,col].GetFragment() != null && !inventorySlotArray[row-1,col].GetFragment().IsFlesh()) result.x = 1;
        if(col - 1 >= 0 && inventorySlotArray[row,col-1].GetFragment() != null && !inventorySlotArray[row,col-1].GetFragment().IsFlesh()) result.y = 1;
        if(row + 1 < inventorySlotArray.GetLength(0) && inventorySlotArray[row+1,col].GetFragment() != null && !inventorySlotArray[row+1,col].GetFragment().IsFlesh()) result.z = 1;
        if(col + 1 < inventorySlotArray.GetLength(1) && inventorySlotArray[row,col+1].GetFragment() != null && !inventorySlotArray[row,col+1].GetFragment().IsFlesh()) result.w = 1;
    
        return result;
    }

    //Method to check if the fragment can be placed at these coordinates
    protected bool CanPlaceFragment(Fragment fragment, int rowOrigin, int colOrigin)
    {
        //Check that x and y are within bounds
        if(rowOrigin < 0 || colOrigin < 0)
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
                    int actualRow = rowOrigin + row;
                    int actualCol = colOrigin + col;

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
    public Vector2Int GetGridPosition(Fragment fragment)
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

    //Method to put all of the biting together
    //Returns true if successful; false if not
    public bool TryBite(Fragment fragment, int damage)
    {
        Vector2Int targetPosition = GetGridPosition(fragment);
        int rowNum = targetPosition.x;
        int colNum = targetPosition.y;
        
        //(return false when invalid bound found)
        if(rowNum < 0) return false;
        if(colNum < 0) return false;
        if(rowNum + fragment.shape.GetLength(0) > inventorySlotArray.GetLength(0)) return false;
        if(colNum + fragment.shape.GetLength(1) > inventorySlotArray.GetLength(1)) return false;

        //Deal damage to each necessary spot
        //Functionally like PlaceFragment, except it deals damage
        //Loop through necessary spots, relative to x and y, and deal damage to fragments
        for(int i = 0; i < fragment.shape.GetLength(0); i++)
        {
            for(int j = 0; j < fragment.shape.GetLength(1); j++)
            {
                if(fragment.shape[i,j] == true && inventorySlotArray[rowNum + i, colNum + j].GetFragment() != null)
                {
                    inventorySlotArray[rowNum + i, colNum + j].GetFragment().TakeDamage(damage);
                }
            }
        }

        //TODO: Make pretty graphics here

        return true;
    }

    //Functionally like highlight, but a different color, and also shows would-be damage dealt to fragments
    public void BiteHighlight(Fragment fragment, int damage)
    {
        Vector2Int gridPosition = GetGridPosition(fragment);
        int rowOrigin = gridPosition.x;
        int colOrigin = gridPosition.y;

        bool canPlaceFragment = true;

        //Calculate if we can actually place the fragment
        if(rowOrigin < 0 || colOrigin < 0 || rowOrigin + fragment.shape.GetLength(0) > inventorySlotArray.GetLength(0)  || colOrigin + fragment.shape.GetLength(1) > inventorySlotArray.GetLength(1))
        {
            canPlaceFragment = false;
        }

        //De-highlight all positions
        ClearBiteHighlight();

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

                    //Show would-be damage
                    Fragment currentFragment = inventorySlotArray[actualRow, actualCol].GetFragment();
                    if(currentFragment != null)
                    {
                        currentFragment.ShowWouldBeDamage(damage);
                    }
                }
            }
        }
    }

    //Clears bite highlight color and would-be health drain visuals
    public void ClearBiteHighlight()
    {
        ClearHighlight(); //Clears the color

        //Un-show the values of would-be health drain
        Fragment.HideWouldBeDamage();
    }

    //Method to take damage from battle
    public void TakeBattleDamage()
    {
        //Pick a random inventory slot
        int randomRow = UnityEngine.Random.Range(0, gridNumRows);
        int randomCol = UnityEngine.Random.Range(0, gridNumCols);

        Fragment fragment = inventorySlotArray[randomRow, randomCol].GetFragment();
        if(fragment != null && fragment.currentHealth > 0)
        {
            //Damage fragment
            fragment.TakeDamage(1);
        }
        else
        {
            //Reduce flesh if there is no fragment protecting this spot
            RoguelikeGameplayController.AddFlesh(-1);
        }
    }
}
