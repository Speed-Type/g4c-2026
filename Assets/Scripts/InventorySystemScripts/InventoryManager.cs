using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class InventoryManager : GeneralInventoryStructure
{
    [Header("Inventory Manager Settings")]
    public int inventoryNumCols;
    public int inventoryNumRows;
    public Transform playerModelTransform;
    public GameObject silkTuftGO;
    public float caterpillarRadius;
    public float caterpillarLength;

    protected override void Awake()
    {
        base.Awake();
        GenerateInventory(inventoryNumRows, inventoryNumCols);
    }
    
    //Overridden fragment placement
    public override void PlaceFragment(Fragment fragment, int x, int y)
    {
        base.PlaceFragment(fragment, x, y);

        //Activate fragment's effects
        fragment.ActivateEffects();
        fragment.isEquipped = true;

        //Note that x corresponds to row number, which is basically y in cartesian coordinates,
        //and y corresponds to column number, which is basically x in cartesian coordinates

        //Get center of fragment
        Vector2 centerOffset = fragment.GetCenterOffset();
        float newX = x + centerOffset.x;
        float newY = y + centerOffset.y;

        //Calculate spawn position
        float t = (float)newY / (inventoryNumCols - 1);
        float angle = Mathf.Lerp(0f, Mathf.PI, t);

        float heightOffset = (newX + 1 - inventoryNumRows * 0.5f) * (caterpillarLength / inventoryNumRows);

        Vector3 localPos = new Vector3(
            Mathf.Cos(angle) * caterpillarRadius,
            Mathf.Sin(angle) * caterpillarRadius,
            heightOffset
        );

        Vector3 spawnPosition = playerModelTransform.TransformPoint(localPos);

        //Now, calculate spawn rotation, such that the model points outward
        Vector3 localOutward = new Vector3(
            Mathf.Cos(angle),
            Mathf.Sin(angle),
            0f
        );

        Vector3 outward = playerModelTransform.TransformDirection(localOutward);

        Quaternion spawnRotation = Quaternion.LookRotation(outward, playerModelTransform.forward);

        //Apply some randomness to rotation
        float tilt = Random.Range(-10f, 10f);
        float twist = Random.Range(-180, 180);

        Quaternion tiltRot = Quaternion.AngleAxis(tilt, playerModelTransform.forward);
        Quaternion twistRot = Quaternion.AngleAxis(twist, outward);

        Quaternion randomizedSpawnRotation = twistRot * tiltRot * spawnRotation;

        //Spawn it as a child of the player model
        fragment.existingFragmentModel = Instantiate(fragment.fragmentModel, spawnPosition, randomizedSpawnRotation, playerModelTransform);
    
        //Also spawn a silk tuft
        Instantiate(silkTuftGO, spawnPosition, randomizedSpawnRotation, fragment.existingFragmentModel.transform);
    }

    //Overridden fragment removal
    public override bool RemoveFragment(Fragment fragment)
    {
        if(base.RemoveFragment(fragment))
        {
            //Deactivate fragment effects
            fragment.DeactivateEffects();
            fragment.isEquipped = false;

            //Remove from player model
            Destroy(fragment.existingFragmentModel);
            fragment.existingFragmentModel = null;
        }

        return true; //this does nothing
    }
}
