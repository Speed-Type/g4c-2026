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
    [Tooltip("This should be the node border image, not the fill")]
    [SerializeField] private Image nodeTypeImage;
    [SerializeField] private Sprite revealedSprite;
    private Image backgroundImage; //this would be the background
    private bool hasBeenVisited = false;
    private RectTransform rectTransform;
    private float revealAnimSizeMultiplier, revealAnimLength;


    protected virtual void Awake()
    {
        //Initialize variables
        backgroundImage = GetComponent<Image>();
        rectTransform = GetComponent<RectTransform>();

        //Get animation parameters from the ButtonHoverAnim script that should also be on this object
        ButtonHoverAnim buttonHoverAnim = GetComponent<ButtonHoverAnim>();
        revealAnimSizeMultiplier = buttonHoverAnim.initialSizeMultiplier;
        revealAnimLength = buttonHoverAnim.initialGrowthSpeed;
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
        isRevealed = true;

        nodeTypeImage.sprite = revealedSprite;

        //Do visual effect
        LeanTween.scale(rectTransform, Vector3.one * revealAnimSizeMultiplier, revealAnimLength)
            .setEaseOutElastic()
            .setOnComplete(() =>
            {
                LeanTween.scale(rectTransform, Vector3.one, revealAnimLength).setEaseOutElastic();
            });
    }

    //Node traversal method
    public virtual void VisitNode()
    {
        //This method is called using super in subclasses
    }

    //Method called in child classes to change node bg color to show it has been visited
    public void SetNodeBGColorVisited()
    {
        if(hasBeenVisited) return; //ensures this code only runs once
        hasBeenVisited = true;

        backgroundImage.color = Color.white;

        RevealNode(); //If the player has visited a node it should also reveal its type
    }
}