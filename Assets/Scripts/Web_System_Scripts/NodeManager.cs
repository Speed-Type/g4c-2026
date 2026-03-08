using UnityEngine;
using System.Collections.Generic;
using System;
using System.Numerics;
using Unity.VisualScripting;

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
        centerNode.GetComponent<RectTransform>().localPosition = UnityEngine.Vector3.zero;
        centerNodeGO.name = "Center"; //rename

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
                //Create new node
                GameObject newNodeGO = Instantiate(nodeGO, UIParentGO.transform);
                Node newNode = newNodeGO.GetComponent<Node>();
                newNodeGO.name = "Radius " + j + ", Circle " + i; //rename

                //Connect pointers, add to list
                newNode.AddNode(previousCircle[j]);
                previousCircle[j].AddNode(newNode);

                //Save data before replacing nodes in the array
                RectTransform previousNodeRectTransform = previousCircle[j].gameObject.GetComponent<RectTransform>();
                RectTransform newNodeRectTransform = newNodeGO.GetComponent<RectTransform>();

                //Replace previous node in the list with the new node
                previousCircle[j] = newNode;

                //Reposition the new node graphically
                UnityEngine.Vector3 newPosition = new UnityEngine.Vector3(webSize / webNumCircleWebs * (i+1) * (float)Math.Cos(2 * Math.PI * ((float)j / webNumRadii)), webSize / webNumCircleWebs * (i+1) * (float)Math.Sin(2 * Math.PI * ((float)j / webNumRadii)), 0);
                newNodeRectTransform.localPosition = newPosition;    

                //Add web thread between graphically
                AddThread(previousNodeRectTransform, newNodeRectTransform);
            }

            //Connect all the nodes in the previousCircle array
            for(int j = 0; j < webNumRadii - 1; j++)
            {
                previousCircle[j].AddNode(previousCircle[j+1]);
                previousCircle[j+1].AddNode(previousCircle[j]);
                AddThread(previousCircle[j].GetComponent<RectTransform>(), previousCircle[j+1].GetComponent<RectTransform>());
            }

            //Connect first and last nodes
            previousCircle[0].AddNode(previousCircle[previousCircle.Length - 1]);
            previousCircle[previousCircle.Length - 1].AddNode(previousCircle[0]);
            AddThread(previousCircle[0].GetComponent<RectTransform>(), previousCircle[previousCircle.Length - 1].GetComponent<RectTransform>());
        }

        //Set current node to a random outermost-loop node
        currentNode = previousCircle[UnityEngine.Random.Range(0,previousCircle.Length)];
        UpdatePlayerMarker();
    }

    private void AddThread(RectTransform rectTransform1, RectTransform rectTransform2)
    {
        //Create new thread
        RectTransform threadRectTransform = Instantiate(threadGO, UIParentGO.transform).GetComponent<RectTransform>();
        threadRectTransform.SetAsFirstSibling();

        //Set thread center to midpoint
        threadRectTransform.localPosition = (rectTransform1.localPosition + rectTransform2.localPosition) / 2;
        
        //rotates thread as necessary
        UnityEngine.Vector3 differenceVector = rectTransform2.localPosition - rectTransform1.localPosition;
        float angle;
        if(differenceVector.x == 0)
        {
            angle = 0;
        }
        else
        {
            angle = (float)Math.Atan2(differenceVector.y, differenceVector.x) * 180 / (float)Math.PI;
        }
        threadRectTransform.localEulerAngles = new UnityEngine.Vector3(0, 0, angle + 90);

        //scales thread as necessary
        threadRectTransform.sizeDelta *= new UnityEngine.Vector2(1, (float)Math.Sqrt(Math.Pow(rectTransform2.localPosition.x - rectTransform1.localPosition.x, 2) + Math.Pow(rectTransform2.localPosition.y - rectTransform1.localPosition.y, 2)));
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

    private void UpdatePlayerMarker()
    {
        playerMarkerGO.GetComponent<RectTransform>().localPosition = currentNode.GetComponent<RectTransform>().localPosition;
    }

    void Start()
    {
        GenerateWeb(); //Temporary
    }
}
