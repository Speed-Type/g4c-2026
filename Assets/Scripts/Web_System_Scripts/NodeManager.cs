using UnityEngine;
using System.Collections.Generic;
using System;

public class NodeManager : MonoBehaviour
{
    //Variables in Inspector
    public GameObject UIParentGO;
    public GameObject nodeGO;
    public GameObject playerMarkerGO;
    public GameObject threadGO;
    [SerializeField] private Node currentNode;
    [Header("Web Generation Parameters")]
    [Tooltip("Number of Web Radii must be >= 3")]
    public int webNumRadii;
    public int webNumCircleWebs;
    [Tooltip("The radius of the whole web")]
    public float webSize;

    public void GenerateWeb()
    {
        //Set Node's NodeManager
        Node.SetNodeManager(this);

        //Create center
        GameObject centerNodeGO = Instantiate(nodeGO, UIParentGO.transform);
        Node centerNode = centerNodeGO.GetComponent<Node>();
        centerNodeGO.name = "Center";

        //Setup array for circling node generation logic
        Node[] previousCircle = new Node[webNumRadii];
        for(int i = 0; i < webNumRadii; i++)
        {
            previousCircle[i] = centerNode;
        }

        //Create circles
        for(int i = 0; i < webNumCircleWebs; i++)
        {
            for(int j = 0; j < webNumRadii; j++)
            {
                GameObject newNodeGO = Instantiate(nodeGO, UIParentGO.transform);
                Node newNode = newNodeGO.GetComponent<Node>();
                newNodeGO.name = "Radius " + j + ", Circle " + i;
                newNode.AddNode(previousCircle[j]);
                previousCircle[j].AddNode(newNode);
                previousCircle[j] = newNode;

                //Reposition the new node graphically
                RectTransform newNodeRectTransform = newNodeGO.GetComponent<RectTransform>();
                Vector3 newPosition = new Vector3(webSize / webNumCircleWebs * (i+1) * (float)Math.Cos(2 * Math.PI * ((float)j / webNumRadii)), webSize / webNumCircleWebs * (i+1) * (float)Math.Sin(2 * Math.PI * ((float)j / webNumRadii)), 0);
                newNodeRectTransform.localPosition += newPosition;
            }

            //Connect all the nodes in the previousCircle array
            for(int j = 0; j < webNumRadii - 1; j++)
            {
                previousCircle[j].AddNode(previousCircle[j+1]);
                previousCircle[j+1].AddNode(previousCircle[j]);
            }

            //Connect first and last nodes
            previousCircle[0].AddNode(previousCircle[previousCircle.Length - 1]);
            previousCircle[previousCircle.Length - 1].AddNode(previousCircle[0]);
        }

        //Set current node to a random outermost-loop node
        currentNode = previousCircle[UnityEngine.Random.Range(0,previousCircle.Length)];
        UpdatePlayerMarker();
    }

    public bool TryVisitNode(Node nodeToVisit)
    {
        if(currentNode.HasNode(nodeToVisit))
        {
            currentNode = nodeToVisit; //update the current node
            UpdatePlayerMarker();
            return true;
        }
        return false;
    }

    public void UpdatePlayerMarker()
    {
        playerMarkerGO.GetComponent<RectTransform>().localPosition = currentNode.GetComponent<RectTransform>().localPosition;
    }

    void Start()
    {
        GenerateWeb(); //Temporary
    }
}
