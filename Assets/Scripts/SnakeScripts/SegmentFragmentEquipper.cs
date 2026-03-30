using UnityEngine;

public class SegmentFragmentEquipper : MonoBehaviour
{
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private GameObject silkTuftGO;

    public void EquipFragment()
    {
        Fragment fragment = RoguelikeGameplayController.instance.inventoryManager.GetRandomEquippedFragment();

        if(fragment != null)
        {
            //Apply some randomness to rotation
            Vector3 randRot = new Vector3(Random.Range(-20f, 20f), Random.Range(-20f, 20f), Random.Range(-180f, 180f));

            Instantiate(fragment.fragmentModel, spawnPoint.position, spawnPoint.rotation, transform).transform.Rotate(randRot);
            Instantiate(silkTuftGO, spawnPoint.position, spawnPoint.rotation, transform).transform.Rotate(randRot);
        }
    }
}
