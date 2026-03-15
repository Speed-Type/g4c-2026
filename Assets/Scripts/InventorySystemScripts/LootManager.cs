using System.Collections.Generic;
using UnityEngine;

public class LootManager : GeneralInventoryStructure
{
    //Variable to hold all the different fragments
    public GameObject[] fragmentList;

    //Private struct to hold data for a loot table
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
            case LootTables.Test:
                bugLoot.numRows = 3;
                bugLoot.numCols = 5;
                bugLoot.minLoot = 1;
                bugLoot.maxLoot = 4;
                break;
            default:
                break;
        }

        return bugLoot;
    }

    //Method to generate new loot
    public void GenerateLoot(LootTables lootEnum)
    {
        //Get actual loot info and probabilities
        BugLoot bugLoot = LootEnumToBugLoot(lootEnum);

        //Generate inventory slots
        GenerateInventory(bugLoot.numRows, bugLoot.numCols);

        //TODO: determine which loots to instantiate

        //TODO: instantiate them and place them actually in the inventory
    }
}

//Enumerator for loot tables
public enum LootTables
{
    Test
}

//Enumerator for fragments
//NOTE: The order of values here must match exactly with the order of gameobjects in fragmentList
public enum Fragments
{
    Test_Test
}