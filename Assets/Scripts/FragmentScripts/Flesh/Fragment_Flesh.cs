using UnityEngine;
using UnityEngine.EventSystems;

public class Fragment_Flesh : Fragment
{
    public GameObject connectorGO;
    public Fragment_Flesh()
    {
        shape = new bool[,]
        {
            {true, true},
            {true, true}
        };
    }

    protected override void Awake()
    {
        //Set dummy values, since this is derived from fragment
        maxHealth = 1;
        displayBugGroup = "Flesh";
        displayBodyPart = "";
        displayCommonName = "";
        displayScientificName = "";
        displayDescription = "You need it to grow.\nFragments can't be removed until their adjacent flesh is removed.";
        
        base.Awake();

        Pulse(); //Start idle pulsating
    }

    public override bool IsFlesh()
    {
        return true;
    }

    public override void TryFreeFragment()
    {
        //Do nothing; this fragment should not be movable to begin with
    }

    public override void Die()
    {
        if(isDead) return; //only want to call this once
        isDead = true;
        
        //Add flesh to player
        RoguelikeGameplayController.AddFlesh(4);

        //Do graphical effect
        LeanTween.scale(gameObject, Vector3.one * 1.3f, 0.25f)
        .setEaseOutBack()
        .setOnComplete(() =>
        {
            RoguelikeCameraController.SpawnParticle(transform.position, deathParticles);
            Destroy(this.gameObject);
        });
    }

    //Just overriding to fix the bug where tooltip appears while flesh is mid-death animation
    public override void OnPointerEnter(PointerEventData eventData)
    {
        if(isDead) return;

        base.OnPointerEnter(eventData);
    }
    
    //Method to do the graphical pulsating
    void Pulse()
    {
        float scale = Random.Range(1.03f, 1.08f);
        float rotation = Random.Range(-1f,1f);
        float time = Random.Range(0.75f, 1.05f);

        LeanTween.scale(gameObject, Vector3.one * scale, time)
            .setEaseInOutSine()
            .setOnComplete(() =>
            {
                LeanTween.scale(gameObject, Vector3.one, time)
                    .setEaseInOutSine()
                    .setOnComplete(Pulse);
            });

        LeanTween.rotateZ(gameObject, rotation, time)
            .setEaseInOutSine().setOnComplete(() =>
            {
                LeanTween.rotateZ(gameObject, 0, time)
                    .setEaseInOutSine();
            });
    }

    //Method to draw connectors to fragments (they'll have already all been placed down before this)
    public void GenerateConnectors(int rowOrigin, int colOrigin)
    {
        //Calculate offset to get center of squares, not just pivots
        Vector3 offset = new Vector3(belongedInventory.inventorySlotSize / 2f, -belongedInventory.inventorySlotSize / 2f, 0);

        for(int row = 0; row < shape.GetLength(0); row++)
        {
            for(int col = 0; col < shape.GetLength(1); col++)
            {
                //Check that this square of the shape is actually filled
                if(shape[row,col] == false) continue;

                int actualRow = rowOrigin + row;
                int actualCol = colOrigin + col;

                //Now, check the adjacent inventory slots for fragments
                Vector4 adjacentFragments = belongedInventory.GetAdjacentFragmentsFromSingleSlot(actualRow, actualCol);
                //Note that x corresponds to up, y is left, z is down, and w is right

                Vector3 thisSlotPosition = new Vector3(col * belongedInventory.inventorySlotSize, -row * belongedInventory.inventorySlotSize, 0);

                //Now draw connectors
                if(adjacentFragments.x == 1) Instantiate(connectorGO, transform.position + thisSlotPosition + offset, Quaternion.Euler(0, 0, 90 + Random.Range(-5f, 5f)), transform);
                if(adjacentFragments.y == 1) Instantiate(connectorGO, transform.position + thisSlotPosition + offset, Quaternion.Euler(0, 0, 180 + Random.Range(-5f, 5f)), transform);
                if(adjacentFragments.z == 1) Instantiate(connectorGO, transform.position + thisSlotPosition + offset, Quaternion.Euler(0, 0, -90 + Random.Range(-5f, 5f)), transform);
                if(adjacentFragments.w == 1) Instantiate(connectorGO, transform.position + thisSlotPosition + offset, Quaternion.Euler(0, 0, 0 + Random.Range(-5f, 5f)), transform);
            }
        }
    }
}
