using UnityEngine;
using System.Collections.Generic;

public class Node : MonoBehaviour
{
    //Static variables
    protected static NodeManager nodeManager;

    //Instance variables
    [SerializeField] private List<Node> nodeList;

    //Accessor methods
    public bool HasNode(Node other)
    {
        return nodeList.Contains(other);
    }

    //Mutator methods
    public void AddNode(Node other)
    {
        nodeList.Add(other);
    }

    public static void SetNodeManager(NodeManager other)
    {
        nodeManager = other;
    }

    //Node traversal methods

    //TODO: This method is called using super in subclasses
    public virtual void VisitNode()
    {
        //Send request to NodeManager; if this node is not accessible, the rest of the function won't run
        if(!nodeManager.TryVisitNode(this))
        {
            return;
        }
    }
}