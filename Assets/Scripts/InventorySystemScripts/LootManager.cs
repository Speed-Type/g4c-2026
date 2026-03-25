using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LootManager : GeneralInventoryStructure
{
    //Public variables visible in inspector
    [Header("Loot-System-Specific Graphics")]
    public Image lootSystemBackground;

    [Header("Loot Manager Settings")]
    public int maxFragmentPositioningAttempts; //Used in randomly placing fragments
    public GameObject[] fragmentList; //List to hold all the different fragments
    public GameObject[] scavengeBugModelList; //List to hold all the different bug models for scavenge nodes
    public GameObject[] combatBugModelList; //List to hold all the different bug models for combat nodes
    [Tooltip("Should be placed in order of priority to spawn (Largest in grid size to smallest)")]
    public GameObject[] fleshList; //List of fleshes to attempt to spawn

    //Private variables for internal logic
    private InventoryManager inventoryManager;
    public static LootManager instance;

    //Private helper class to hold data for a loot table
    private class BugLoot
    {
        public int numRows;
        public int numCols;
        public int minLoot, maxLoot;
        public float scavengeFleshChance;
        public int[] ratioList;

        private int totalRatio;

        public BugLoot(int ratioListLength)
        {
            ratioList = new int[ratioListLength];
        }

        public void SetChances(Fragments fragmentEnum, int ratio)
        {
            ratioList[(int)fragmentEnum] = ratio;
            totalRatio += ratio;
        }

        public int GetRandomFragmentIndex()
        {
            //Generate random number from [0, totalWeights]
            float rand = Random.Range(0f, totalRatio);
            float current = 0;

            for(int j = 0; j < ratioList.Length; j++)
            {
                current += ratioList[j];
                if(current >= rand)
                {
                    //This is the correct gameobject
                    return j;
                }
            }

            return 0; //This does nothing
        }
    }

    //Method to convert enumerator Bugs to struct of bug loot
    private BugLoot LootEnumToBugLoot(Bugs val)
    {
        BugLoot bugLoot = new BugLoot(fragmentList.Length);
        switch(val)
        {
            case Bugs.AfricanBigHeadedAnt:
                bugLoot.numRows = 3;
                bugLoot.numCols = 5;
                bugLoot.minLoot = 2;
                bugLoot.maxLoot = 4;
                bugLoot.scavengeFleshChance = .3f;
                bugLoot.SetChances(Fragments.AfricanBigHeadedAnt_Abdomen, 4);
                bugLoot.SetChances(Fragments.AfricanBigHeadedAnt_Antenna, 4);
                bugLoot.SetChances(Fragments.AfricanBigHeadedAnt_HeadMajor, 1);
                bugLoot.SetChances(Fragments.AfricanBigHeadedAnt_HeadMinor, 1);
                bugLoot.SetChances(Fragments.AfricanBigHeadedAnt_Leg, 10);
                bugLoot.SetChances(Fragments.AfricanBigHeadedAnt_Thorax, 4);
                break;
            default:
                break;
        }

        return bugLoot;
    }

    //Overridden awake method
    protected override void Awake()
    {
        base.Awake();

        instance = this;

        //Initialize inventory manager
        inventoryManager = FindFirstObjectByType<InventoryManager>();

        //Create individual material for flesh background
        lootSystemBackground.material = new Material(lootSystemBackground.material);
    }

    //Method to generate new loot
    public void GenerateLoot(Bugs lootEnum)
    {
        //Get actual loot info and probabilities
        BugLoot bugLoot = LootEnumToBugLoot(lootEnum);

        //Generate inventory slots
        GenerateInventory(bugLoot.numRows, bugLoot.numCols);

        //Determine how many loots to instantiate
        int numLootToSpawn = Random.Range(bugLoot.minLoot, bugLoot.maxLoot);

        //Instantiate loots
        for(int i = 0; i < numLootToSpawn; i++)
        {
            //Instantiate new fragment
            GameObject newFragmentGO = Instantiate(fragmentList[bugLoot.GetRandomFragmentIndex()], inventorySystemParentTransform);
            Fragment newFragment = newFragmentGO.GetComponent<Fragment>();

            newFragment.belongedInventory = this;

            //Try to place it in the grid
            bool placementSuccessful = false;
            int numTriesSoFar = 0;
            while(!placementSuccessful && numTriesSoFar < maxFragmentPositioningAttempts)
            {                
                //Rotate random number of times
                for(int j = 0; j < Random.Range(0, 3); j++)
                {
                    newFragment.Rotate();
                }

                //Check that the dimensions of the fragment are not outside the total dimensions of the grid
                if(newFragment.shape.GetLength(0) > gridNumRows || newFragment.shape.GetLength(1) > gridNumCols)
                {
                    newFragment.Rotate(); //rotate again
                }
                //Note that GetLength(0) returns number of rows, and GetLength(1) returns number of cols

                //Pick a random position (such that the new fragment will not be out of bounds)
                int potentialRow = Random.Range(0, gridNumRows - newFragment.shape.GetLength(0) + 1);
                int potentialCol = Random.Range(0, gridNumCols - newFragment.shape.GetLength(1) + 1);

                //Check if that position works
                if(SnapToGrid(newFragment, potentialRow, potentialCol))
                {
                    placementSuccessful = true;
                }

                numTriesSoFar++;
            }

            if(placementSuccessful)
            {
                //If placement successful, TBD
                
            }
            else
            {
                //If unsuccessful, destroy that fragment
                
                Destroy(newFragmentGO);
            }
        }

        //Loop through inventory slots
        for(int row = 0; row < gridNumRows; row++)
        {
            for(int col = 0; col < gridNumCols; col++)
            {
                //For every empty inventory slot
                if(inventorySlotArray[row,col].GetFragment() == null)
                {
                    //Random chance to make it a flesh
                    //TODO: Make battle loot better than scavenge loot
                    if(Random.Range(0f,1f) < bugLoot.scavengeFleshChance)
                    {
                        //Loop through possible fleshes to spawn and spawn the first one possible
                        for(int j = 0; j < fleshList.Length; j++)
                        {
                            //Make new flesh
                            Fragment newFlesh = Instantiate(fleshList[j], inventorySystemParentTransform).GetComponent<Fragment>();
                            newFlesh.belongedInventory = this;

                            //Rotate a random number of times
                            for(int k = 0; k < Random.Range(0,3); k++)
                            {
                                newFlesh.Rotate();
                            }

                            //Try to snap to grid
                            if(SnapToGrid(newFlesh, row, col))
                            {
                                //If successful, break out of loop and call for attachments
                                j = fleshList.Length + 67;

                                newFlesh.gameObject.GetComponent<Fragment_Flesh>().GenerateConnectors(row, col);
                            }
                            else
                            {
                                //If not, destroy this attempt at spawn
                                Destroy(newFlesh.gameObject);
                            }
                        }
                    }
                }
            }
        }

        //Graphically, randomize the tiling offset of the background
        lootSystemBackground.material.mainTextureOffset = new Vector2(Random.Range(0f,1f), Random.Range(0f,1f));
    }

    public static string BugEnumToBugName(Bugs bug)
    {
        switch(bug)
        {
            case Bugs.AfricanBigHeadedAnt:
                return "African Big-Headed Ant";
        }

        return "ERROR: NOT FOUND";
    }

    public GameObject BugEnumToScavengeModel(Bugs bug)
    {
        return scavengeBugModelList[(int)bug];
    }

    public GameObject BugEnumToCombatModel(Bugs bug)
    {
        return combatBugModelList[(int)bug];
    }
}

//Enumerator for loot tables
public enum Bugs
{
    AfricanBigHeadedAnt
}

//Enumerator for fragments
//NOTE: The order of values here must match exactly with the order of gameobjects in fragmentList
public enum Fragments
{
    AfricanBigHeadedAnt_Abdomen,
    AfricanBigHeadedAnt_Antenna,
    AfricanBigHeadedAnt_HeadMajor,
    AfricanBigHeadedAnt_HeadMinor,
    AfricanBigHeadedAnt_Leg,
    AfricanBigHeadedAnt_Thorax
}