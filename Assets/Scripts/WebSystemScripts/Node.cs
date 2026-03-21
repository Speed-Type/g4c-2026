using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

public class Node : MonoBehaviour
{
    //Static variables
    protected static NodeManager nodeManager;

    //Instance variables
    [SerializeField] private List<Node> nodeList;
    public NodeTypes nodeType;
    public bool isRevealed = false;
    public Sprite revealedSprite;
    private Image image;

    void Awake()
    {
        image = GetComponent<Image>();
    }

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

    //Method to reveal the actual node type
    public void RevealNode()
    {
        if(isRevealed) return;

        image.sprite = revealedSprite;
    }

    //Node traversal method
    public virtual void VisitNode()
    {
        //This method is called using super in subclasses
    }
}