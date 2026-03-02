using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AtkPuzzle : MonoBehaviour
{
    [Header("Configuration")]
    public RectTransform safeArea; // Area dimana item boleh muncul/digerakkan
    public GameObject winUI;
    public List<AtkDraggable> allItems;

    // Start is called before the first frame update
    void Start()
    {
        // Otomatis cari semua item jika list belum diisi manual
        if (allItems == null || allItems.Count == 0)
        {
            allItems = new List<AtkDraggable>(FindObjectsOfType<AtkDraggable>());
        }

        // Initialize Items
        foreach (var item in allItems)
        {
            if (item != null)
            {
                item.AssignSafeArea(safeArea);
            }
        }

        RandomizeItemPositions();
    }

    // Update is called once per frame
    void Update()
    {
        // Bisa tambah cheat key untuk re-shuffle misal tekan 'R'
        if (Input.GetKeyDown(KeyCode.R))
        {
            RandomizeItemPositions();
        }
    }

    public void RandomizeItemPositions()
    {
        if (safeArea == null)
        {
            Debug.LogError("Safe Area belum di-assign di Inspector!");
            return;
        }

        // Hitung batas area
        Rect safeRect = safeArea.rect;
        // Asumsi item child safeArea, jadi localPosition aman.
        // Jika tidak, kita gunakan logic bounds yang sedikit lebih complex, 
        // tapi untuk setup standard, ini cukup.
        
        float minX = safeRect.xMin * 0.9f; 
        float maxX = safeRect.xMax * 0.9f;
        float minY = safeRect.yMin * 0.9f;
        float maxY = safeRect.yMax * 0.9f;

        foreach (var item in allItems)
        {
            if (item == null) continue;

            // Generate random anchored position
            float randomX = Random.Range(minX, maxX);
            float randomY = Random.Range(minY, maxY);
            
            // Random Rotation
            float randomRot = Random.Range(-180f, 180f);

            RectTransform itemRect = item.GetComponent<RectTransform>();
            
            // Reset state
            item.isSnapped = false;
            
            // Set Position
            itemRect.anchoredPosition = new Vector2(randomX, randomY);
            
            // Set Rotation
            itemRect.localEulerAngles = new Vector3(0, 0, randomRot);
        }
    }

    public void CheckWinCondition()
    {
        int snappedCount = 0;
        foreach (var item in allItems)
        {
            if (item != null && item.isSnapped)
            {
                snappedCount++;
            }
        }

        // Print progress kyk BookScript
        Debug.Log($"Progress: {snappedCount} / {allItems.Count} objects placed.");

        if (snappedCount >= allItems.Count)
        {
            Debug.Log("LEVEL 4 COMPLETE!");
            if (winUI != null)
            {
                winUI.SetActive(true);
            }
            
            // Disable all draggables so they can't be moved after win
            foreach (var item in allItems)
            {
                if (item != null) item.enabled = false;
            }
        }
    }
}
