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
    }

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
        NodeManager.instance.webNumRadii = parameterSet.webNumRadii;
        NodeManager.instance.webNumCircleWebs = parameterSet.webNumCircleWebs;
        NodeManager.instance.webRadiusSize = parameterSet.webRadiusSize;
        NodeManager.instance.nodeWeights = parameterSet.nodeWeights;
        NodeManager.instance.nodePositionRandomization = parameterSet.nodePositionRandomization;
        NodeManager.instance.chanceForNewBug = parameterSet.chanceForNewBug;

        //RG Controller
        RoguelikeGameplayController.instance.goalFlesh = parameterSet.goalFlesh;
        RoguelikeGameplayController.instance.maxSpiderAlertness = parameterSet.maxSpiderAlertness;
    }
}
