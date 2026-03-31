using UnityEngine;
using System;
using System.Collections.Generic;

public class ParameterLoader : MonoBehaviour
{
    [System.Serializable]
    public class ParameterSet
    {
        [Header("NodeManager Parameters")]
        public int webNumRadii;
        public int webNumCircleWebs;
        public float[] webRadiusSize;
        public float[] nodeWeights;
        public float[] nodePositionRandomization;
        public float chanceForNewBug;

        [Header("RG Controller")]
        public int goalFlesh;
        public int maxSpiderAlertness;
        public int spiderMovesPerTurn;

        [Header("Inventory")]
        public int inventoryNumRows;
        public int inventoryNumCols;
    }

    [Header("Script References")]
    public NodeManager nodeManager;
    public RoguelikeGameplayController roguelikeGameplayController;
    public InventoryManager playerInventory;

    public List<ParameterSet> parameterList;

    void Awake()
    {
        switch(PlayerPrefs.GetInt("CurrentStage", 0))
        {
            case 0:
                ApplyParameters(parameterList[0]);
                break;
            case 2:
                ApplyParameters(parameterList[1]);
                break;
            case 4:
                ApplyParameters(parameterList[2]);
                break;
        }
    }

    private void ApplyParameters(ParameterSet parameterSet)
    {
        //Node manager
        nodeManager.webNumRadii = parameterSet.webNumRadii;
        nodeManager.webNumCircleWebs = parameterSet.webNumCircleWebs;
        nodeManager.webRadiusSize = parameterSet.webRadiusSize;
        nodeManager.nodeWeights = parameterSet.nodeWeights;
        nodeManager.nodePositionRandomization = parameterSet.nodePositionRandomization;
        nodeManager.chanceForNewBug = parameterSet.chanceForNewBug;

        //RG Controller
        roguelikeGameplayController.goalFlesh = parameterSet.goalFlesh;
        roguelikeGameplayController.maxSpiderAlertness = parameterSet.maxSpiderAlertness;
        roguelikeGameplayController.spiderMovesPerTurn = parameterSet.spiderMovesPerTurn;

        //Inventory
        playerInventory.inventoryNumRows = parameterSet.inventoryNumRows;
        playerInventory.inventoryNumCols = parameterSet.inventoryNumCols;
    }
}
