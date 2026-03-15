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