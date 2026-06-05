using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AtkPuzzle : MonoBehaviour
{
    [Header("Configuration")]
    public RectTransform safeArea;
    public GameObject winUI;
    public List<AtkDraggable> allItems;

    // 🔊 SFX (DITAMBAHKAN)
    [SerializeField] private AudioClip dropSFX;
    private AudioSource audioSource;

    void Start()
    {
        // 🔊 INIT AUDIO (DITAMBAHKAN)
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        if (allItems == null || allItems.Count == 0)
        {
            allItems = new List<AtkDraggable>(FindObjectsOfType<AtkDraggable>());
        }

        foreach (var item in allItems)
        {
            if (item != null)
            {
                item.AssignSafeArea(safeArea);
            }
        }

        RandomizeItemPositions();
    }

    void Update()
    {
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

        Rect safeRect = safeArea.rect;
        
        float minX = safeRect.xMin * 0.9f; 
        float maxX = safeRect.xMax * 0.9f;
        float minY = safeRect.yMin * 0.9f;
        float maxY = safeRect.yMax * 0.9f;

        foreach (var item in allItems)
        {
            if (item == null) continue;

            float randomX = Random.Range(minX, maxX);
            float randomY = Random.Range(minY, maxY);
            
            float randomRot = Random.Range(-180f, 180f);

            RectTransform itemRect = item.GetComponent<RectTransform>();
            
            item.isSnapped = false;
            
            itemRect.anchoredPosition = new Vector2(randomX, randomY);
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

        Debug.Log($"Progress: {snappedCount} / {allItems.Count} objects placed.");

        if (snappedCount >= allItems.Count)
        {
            Debug.Log("LEVEL 4 COMPLETE!");
            if (winUI != null)
            {
                winUI.SetActive(true);
            }
            
            foreach (var item in allItems)
            {
                if (item != null) item.enabled = false;
            }
        }
    }

    // 🔊 FUNCTION PLAY SFX (DITAMBAHKAN)
    public void PlayDropSFX()
    {
        if (audioSource != null && dropSFX != null)
        {
            audioSource.pitch = Random.Range(0.95f, 1.05f);
            audioSource.PlayOneShot(dropSFX);
        }
    }
}