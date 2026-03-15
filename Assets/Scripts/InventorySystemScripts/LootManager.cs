using System.Collections.Generic;
using UnityEngine;

public class LootManager : GeneralInventoryStructure
{
    //Private struct to hold data for a loot table
    struct BugLoot
    {
        public int numRows;
        public int numCols;

        public BugLoot(int pNumRows, int pNumCols)
        {
            numRows = pNumRows;
            numCols = pNumCols;
        }
    }

    void Awake()
    {
        //TEMPORARY
        GenerateInventory(3,5);
    }

    //Method to convert enumerator LootTables to struct of bug loot
    private BugLoot LootEnumToBugLoot(LootTables val)
    {
        switch(val)
        {
            case LootTables.Test:
                return new BugLoot(3, 5);
            default:
                return new BugLoot();
        }
    }

    //Method to generate new loot
    public void GenerateLoot(LootTables lootEnum)
    {
        BugLoot bugLoot = LootEnumToBugLoot(lootEnum);
        GenerateInventory(bugLoot.numRows, bugLoot.numCols);
    }
}

//Enumerator for loot tables
public enum LootTables
{
    Test
}