using System.Collections.Generic;
using UnityEngine;

public class LootManager : GeneralInventoryStructure
{
    //Public variables visible in inspector
    [Header("Loot Manager Settings")]
    public int maxFragmentPositioningAttempts; //Used in randomly placing fragments
    public GameObject[] fragmentList; //List to hold all the different fragments

    //Private variables for internal logic
    private InventoryManager inventoryManager;

    //Private helper class to hold data for a loot table
    private class BugLoot
    {
        public int numRows;
        public int numCols;
        public int minLoot;
        public int maxLoot;
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

    //Method to convert enumerator LootTables to struct of bug loot
    private BugLoot LootEnumToBugLoot(LootTables val)
    {
        BugLoot bugLoot = new BugLoot(fragmentList.Length);
        switch(val)
        {
            case LootTables.AfricanBigHeadedAnt:
                bugLoot.numRows = 3;
                bugLoot.numCols = 5;
                bugLoot.minLoot = 2;
                bugLoot.maxLoot = 5;
                bugLoot.SetChances(Fragments.AfricanBigHeadedAnt_Abdomen, 4);
                bugLoot.SetChances(Fragments.AfricanBigHeadedAnt_Antenna, 4);
                bugLoot.SetChances(Fragments.AfricanBigHeadedAnt_HeadMajor, 1);
                bugLoot.SetChances(Fragments.AfricanBigHeadedAnt_HeadMinor, 2);
                bugLoot.SetChances(Fragments.AfricanBigHeadedAnt_Leg, 10);
                bugLoot.SetChances(Fragments.AfricanBigHeadedAnt_Thorax, 4);
                break;
            default:
                break;
        }

        return bugLoot;
    }

    //Overridden awake method
    void Awake()
    {
        inventoryManager = FindFirstObjectByType<InventoryManager>();
    }

    //Method to generate new loot
    public void GenerateLoot(LootTables lootEnum)
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

            //Try to place it in the grid
            bool placementSuccessful = false;
            int numTriesSoFar = 0;
            while(!placementSuccessful && numTriesSoFar < maxFragmentPositioningAttempts)
            {
                numTriesSoFar++;
            }

            if(placementSuccessful)
            {
                //If placement successful, set new fragment's currently belonged inventory to the loot manager
                newFragment.belongedInventory = this;
            }
            else
            {
                //If unsuccessful, destroy that fragment
                Destroy(newFragmentGO);
            }
        }

        //TODO: Generate flesh
    }
}

//Enumerator for loot tables
public enum LootTables
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