using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

//------------------------------------------------------------------------------------------

//Data structures for what'll be written into player prefs

[Serializable]
public class FragmentSaveData
{
    //Assembly-qualified type name from Fragments enum
    public string typeName;

    //Top-left anchor row passed to PlaceFragment(fragment, row, col)
    public int row;

    //Top-left anchor col passed to PlaceFragment(fragment, row, col)
    public int col;

    //Number of clockwise 90° rotations applied (0-3); matches Fragment.currentRotation
    public int rotation;

    //fragment.currentHealth at save time
    public int currentHealth;

    //True if fragment.isDead at save time
    public bool isDead;
    
    public int shapeRows;
    public int shapeCols;
}

[Serializable]
public class InventorySaveData
{
    public List<FragmentSaveData> fragments = new List<FragmentSaveData>();
}

//------------------------------------------------------------------------------------------

//Static class
//Functions are called like SaveSystem.Save(), etc.

public static class SaveSystem
{
    private const string SaveKey = "inventory_save"; //The player prefs key that will hold EVERYTHING

    //Scans the InventoryManager grid and writes every placed fragment to PlayerPrefs
    public static void Save(GeneralInventoryStructure inventoryManager)
    {
        InventorySaveData saveData = BuildSaveData(inventoryManager);

        try
        {
            string json = JsonUtility.ToJson(saveData);
            PlayerPrefs.SetString(SaveKey, json);
            PlayerPrefs.Save(); //VERY IMPORTANT LINE
            Debug.Log($"[SaveSystem] Saved {saveData.fragments.Count} fragment(s).");
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveSystem] Save failed: {e}"); //fuuuuuu
        }
    }

    //Reads from PlayerPrefs and restores every fragment back onto the grid.
    //
    //The InventoryManager grid must already be generated (Awake has run)
    //The FragmentPrefabRegistry singleton must be populated with prefabs for every fragment type expected
    public static void Load(GeneralInventoryStructure inventoryManager)
    {
        if(!SaveExists())
        {
            Debug.Log("[SaveSystem] No save data found — starting fresh.");
            return;
        }

        InventorySaveData saveData;
        try
        {
            string json = PlayerPrefs.GetString(SaveKey);
            saveData = JsonUtility.FromJson<InventorySaveData>(json);
        }
        catch(Exception e)
        {
            Debug.LogError($"[SaveSystem] Load failed: {e}");
            return;
        }

        if(saveData?.fragments == null || saveData.fragments.Count == 0)
        {
            Debug.Log("[SaveSystem] Save data is empty.");
            return;
        }

        int restored = 0;
        foreach(FragmentSaveData fd in saveData.fragments)
        {
            if(!TryRestoreFragment(fd, inventoryManager))
                Debug.LogWarning($"[SaveSystem] Could not restore fragment of type '{fd.typeName}' at ({fd.row},{fd.col}).");
            else
                restored++;
        }

        Debug.Log($"[SaveSystem] Restored {restored}/{saveData.fragments.Count} fragment(s).");
    }

    //Deletes the save data FOR THE BUG SPECIFICALLY (though in effect that's all that really matters)
    public static void DeleteSave()
    {
        if(!SaveExists()) return;
        PlayerPrefs.DeleteKey(SaveKey);
        PlayerPrefs.Save();
        Debug.Log("[SaveSystem] Save data deleted.");
    }

    public static bool SaveExists() => PlayerPrefs.HasKey(SaveKey);

    //------------------------------------------------------------------------------------------

    //APCSA all over agin
    private static InventorySaveData BuildSaveData(GeneralInventoryStructure inventoryManager)
    {
        var saveData = new InventorySaveData();

        //We iterate every grid slot but only record each fragment once,
        //using a HashSet to skip fragments we've already written
        var recorded = new HashSet<Fragment>();

        int rows = inventoryManager.gridNumRows;
        int cols = inventoryManager.gridNumCols;

        for(int row = 0; row < rows; row++)
        {
            for(int col = 0; col < cols; col++)
            {
                Fragment fragment = inventoryManager.GetFragmentAt(row, col);

                if(fragment == null || recorded.Contains(fragment))
                    continue;

                recorded.Add(fragment);

                //Find this fragment's anchor (top-left) position in the grid
                Vector2Int anchor = inventoryManager.GetAnchorPosition(fragment);

                saveData.fragments.Add(new FragmentSaveData
                {
                    typeName = fragment.fragmentEnum.ToString(), //From the Fragments enum in LootSystem
                    row = anchor.x,
                    col = anchor.y,
                    rotation = fragment.currentRotation,
                    currentHealth = fragment.currentHealth,
                    isDead = fragment.isDead,
                    shapeRows = fragment.shape.GetLength(0),
                    shapeCols = fragment.shape.GetLength(1),
                });
            }
        }

        return saveData;
    }

    private static bool TryRestoreFragment(FragmentSaveData fd, GeneralInventoryStructure inventoryManager)
    {
        //Get the prefab from LootManager's fragment list
        Fragments fragmentEnum;
        Enum.TryParse(fd.typeName, out fragmentEnum);

        GameObject prefab = LootManager.instance.FragmentEnumToFragmentPrefab(fragmentEnum);
        if(prefab == null)
        {
            Debug.LogWarning($"[SaveSystem] No prefab registered for type '{fd.typeName}'.");
            return false;
        }

        //Instantiate under the inventory parent
        GameObject go = UnityEngine.Object.Instantiate(prefab, inventoryManager.inventorySystemParentTransform);
        Fragment fragment = go.GetComponent<Fragment>();

        fragment.belongedInventory = inventoryManager;
        fragment.isFree = true;

        if(fragment == null)
        {
            Debug.LogError($"[SaveSystem] Prefab for '{fd.typeName}' has no Fragment component.");
            UnityEngine.Object.Destroy(go);
            return false;
        }

        //Re-apply rotation
        int targetRotation = ((fd.rotation % 4) + 4) % 4;
        for(int i = 0; i < targetRotation; i++)
            fragment.Rotate();

        if (fd.row < 0 || fd.col < 0 ||
            fd.row + fragment.shape.GetLength(0) > inventoryManager.gridNumRows ||
            fd.col + fragment.shape.GetLength(1) > inventoryManager.gridNumCols)
        {
            Debug.LogError($"[SaveSystem] Fragment '{fd.typeName}' at ({fd.row},{fd.col}) " +
                   $"with shape {fragment.shape.GetLength(0)}x{fragment.shape.GetLength(1)} " +
                   $"doesn't fit in grid {inventoryManager.gridNumRows}x{inventoryManager.gridNumCols}. Skipping.");
            UnityEngine.Object.Destroy(go);
            return false;
        }

        //Place on the grid (calls InventoryManager.PlaceFragment which spawns the 3D model, silk tuft, activates effects, etc.)
        inventoryManager.PlaceFragment(fragment, fd.row, fd.col, true);

        //Deal damage to bring it back to its normal health
        fragment.TakeDamage(fragment.maxHealth - fd.currentHealth);

        return true;
    }
}
