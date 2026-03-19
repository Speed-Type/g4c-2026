using UnityEngine;
using System.Collections.Generic;

public class Node : MonoBehaviour
{
    //Static variables
    protected static NodeManager nodeManager;

    //Instance variables
    [SerializeField] private List<Node> nodeList;
    public NodeTypes nodeType;

    //Accessor methods
    public bool HasNode(Node other)
    {
        return nodeList.Contains(other);
    }

    public List<Node> GetNodeList()
    {
        return nodeList;
    }

    //Mutator methods
    public void AddNode(Node other)
    {
        nodeList.Add(other);
    }

    public void RemoveNode(Node other)
    {
        nodeList.Remove(other);
    }

    public static void SetNodeManager(NodeManager other)
    {
        nodeManager = other;
    }

    //Node traversal methods

    
    public virtual void VisitNode()
    {
        //This method is called using super in subclasses
    }
}