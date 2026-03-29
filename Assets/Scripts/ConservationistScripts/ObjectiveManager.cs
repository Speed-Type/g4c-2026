using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class ObjectiveManager : MonoBehaviour
{
    //Static instance
    public static ObjectiveManager instance {get; private set;}

    //Data structure for a single objective
    [System.Serializable]
    public class Objective
    {
        [Tooltip("Unique ID referenced by InteractableObjects")]
        public string id;

        [Tooltip("Text shown in the objective list")]
        public string description;

        [Tooltip("Mandatory objectives must all be completed to continue; optional ones are bonus")]
        public bool isMandatory = true;

        [Tooltip("Can re-mark as complete (useful for tutorial skips)")]
        public bool isComplete = false;

        [Tooltip("Interaction IDs that mark this objective complete. If empty, completion is manual.")]
        public List<string> completedByInteractions = new();
    }

    [Header("Objectives")]
    public List<Objective> objectives = new();

    [Header("Events")]
    public UnityEvent onAllMandatoryComplete;
    public UnityEvent<Objective> onObjectiveCompleted;

    //------------------------------------------------------------------------------------------

    void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        RefreshUI();
    }

    //------------------------------------------------------------------------------------------

    //Called by InteractableObject when it is clicked
    public void NotifyInteraction(string interactionID)
    {
        bool changed = false;

        foreach(var obj in objectives)
        {
            if(obj.completedByInteractions.Contains(interactionID) && !obj.isComplete)
            {
                obj.isComplete = true;
                onObjectiveCompleted?.Invoke(obj);
                changed = true;
            }
        }

        if(changed)
        {
            RefreshUI();
            CheckCompletion();
        }
    }

    //Manually complete an objective by its ID
    public void CompleteObjective(string id)
    {
        var obj = GetObjective(id);
        if (obj == null || obj.isComplete) return;
        obj.isComplete = true;
        onObjectiveCompleted?.Invoke(obj);
        RefreshUI();
        CheckCompletion();
    }

    public Objective GetObjective(string id) =>
        objectives.Find(o => o.id == id);

    public bool AreAllMandatoryComplete() =>
        objectives.TrueForAll(o => !o.isMandatory || o.isComplete);

    public bool AreAllObjectivesComplete() =>
        objectives.TrueForAll(o => o.isComplete);

    //------------------------------------------------------------------------------------------

    private void RefreshUI()
    {
        ObjectiveUI.instance?.Refresh(objectives);
    }

    private void CheckCompletion()
    {
        if(AreAllMandatoryComplete())
            onAllMandatoryComplete?.Invoke();
    }
}
