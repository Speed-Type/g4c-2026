using UnityEngine;
using System.Collections.Generic;

public class NodeManager : MonoBehaviour
{
    //Instance variables
    public GameObject nodeGameObject;
    private Node currentNode;

    public void GenerateWeb()
    {
        //Set Node's NodeManager
        Node.SetNodeManager(this);

        //Set current node


    }

    public bool TryVisitNode(Node nodeToVisit)
    {
        if(currentNode.HasNode(nodeToVisit))
        {
            currentNode = nodeToVisit; //update the current node
            return true;
        }
        return false;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
