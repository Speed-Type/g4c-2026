using UnityEngine;
using System.Collections;

public class FoodSpawner : MonoBehaviour
{
    //Public variables

    [Header("Prefab")]
    public GameObject foodPrefab;

    [Header("Spawn area")]
    public Vector3 spawnCenter = Vector3.zero;
    [Tooltip("Half-extents of the spawn rectangle (x and z); y is ignored")]
    public Vector3 spawnHalfExtents = new Vector3(18f, 0f, 18f);
    [Tooltip("y position food spawns at")]
    public float groundY = 0f;

    [Header("Spawn rules")]
    [Tooltip("Maximum number of food objects alive at once")]
    public int maxFoodCount = 5;
    [Tooltip("Seconds between spawn attempts.")]
    public float spawnInterval = 2f;
    [Tooltip("Minimum distance from any existing food when picking a spawn position")]
    public float minSpacing = 2f;
    [Tooltip("How many random positions to try before giving up on a spawn attempt")]
    public int maxPlacementTries = 10;

    //Private variables

    private int activeCount = 0;

    //----------------------------------------------------------------------------------------------------------------

    public void OnEnable()
    {
        StartCoroutine(SpawnLoop());
    }

    private IEnumerator SpawnLoop()
    {
        while(true)
        {
            yield return new WaitForSeconds(spawnInterval);

            if(activeCount < maxFoodCount)
                TrySpawn();
        }
    }

    private void TrySpawn()
    {
        for(int attempt = 0; attempt < maxPlacementTries; attempt++)
        {
            Vector3 candidate = RandomPosition();

            if(IsClearOfOtherFood(candidate))
            {
                SpawnAt(candidate);
                return;
            }
        }

        //All attempts failed
    }

    private void SpawnAt(Vector3 position)
    {
        GameObject food = Instantiate(foodPrefab, position, Quaternion.identity, transform);

        //Track count via the food object's destruction
        FoodItem item = food.GetComponent<FoodItem>();
        if(item == null) item = food.AddComponent<FoodItem>();
        item.onDestroyed += () => activeCount--;

        activeCount++;
    }

    //----------------------------------------------------------------------------------------------------------------

    //Helper methods

    private Vector3 RandomPosition()
    {
        return new Vector3(
            spawnCenter.x + Random.Range(-spawnHalfExtents.x, spawnHalfExtents.x),
            groundY,
            spawnCenter.z + Random.Range(-spawnHalfExtents.z, spawnHalfExtents.z));
    }

    private bool IsClearOfOtherFood(Vector3 candidate)
    {
        //Find all active food items and check spacing
        FoodItem[] existing = FindObjectsByType<FoodItem>(FindObjectsSortMode.None);
        foreach(var f in existing)
        {
            if(Vector3.Distance(candidate, f.transform.position) < minSpacing)
                return false;
        }
        return true;
    }

    //----------------------------------------------------------------------------------------------------------------

    //Destroys all active food and resets the count
    public void ClearAll()
    {
        FoodItem[] existing = FindObjectsByType<FoodItem>(FindObjectsSortMode.None);
        foreach (var f in existing)
            Destroy(f.gameObject);
        activeCount = 0;
    }

    //----------------------------------------------------------------------------------------------------------------

    //Nice gizmo for visualization

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 1f, 0f, 0.2f);
        Gizmos.DrawWireCube(
            new Vector3(spawnCenter.x, groundY, spawnCenter.z),
            new Vector3(spawnHalfExtents.x * 2f, 0.1f, spawnHalfExtents.z * 2f));
    }
#endif
}
