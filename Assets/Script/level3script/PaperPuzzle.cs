using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PaperPuzzle : MonoBehaviour
{
    [Header("Game Settings")]
    public RectTransform safeArea; // Assign in Inspector
    public RectTransform snapTarget; // The "Center" or "Box" for the first paper
    [SerializeField] private float winDistanceThreshold = 50f;
    [SerializeField] private GameObject winUI;

    private List<PaperDraggable> papers = new List<PaperDraggable>();

    // ... Start and ScramblePapers ...

    public PaperDraggable GetPaperByIndex(int index)
    {
        return papers.FirstOrDefault(p => p.orderIndex == index);
    }
    
    // ... CheckWinCondition ...
    IEnumerator Start()
    {
        // Disable any LayoutGroup that might force papers back to position
        UnityEngine.UI.LayoutGroup layout = GetComponent<UnityEngine.UI.LayoutGroup>();
        if (layout != null)
        {
            layout.enabled = false;
        }

        // Force Canvas Update
        Canvas.ForceUpdateCanvases();

        // Wait a frame just to be sure
        yield return null;

        // Find all PaperDraggable scripts in the children of this object
        papers = GetComponentsInChildren<PaperDraggable>().ToList();

        ScramblePapers();
    }

    private void ScramblePapers()
    {
        if (safeArea == null)
        {
            Debug.LogError("[PaperPuzzle] Safe Area is NOT assigned in Inspector!");
            return;
        }

        // Get Safe Area World Bounds
        Vector3[] saCorners = new Vector3[4];
        safeArea.GetWorldCorners(saCorners);
        Vector3 saMin = saCorners[0];
        Vector3 saMax = saCorners[2];

        Debug.Log($"[PaperPuzzle] Safe Area Bounds: Min({saMin.x:F2}, {saMin.y:F2}) Max({saMax.x:F2}, {saMax.y:F2})");

        foreach (var paper in papers)
        {
            RectTransform rt = paper.GetComponent<RectTransform>();
            
            // Calculate paper world extents (approximate based on size * scale)
            // Assuming pivot is center (0.5, 0.5)
            Vector2 size = Vector2.Scale(rt.rect.size, rt.lossyScale);
            Vector2 extents = size * 0.5f;

            // Constrain min and max
            float minX = saMin.x + extents.x;
            float maxX = saMax.x - extents.x;
            float minY = saMin.y + extents.y;
            float maxY = saMax.y - extents.y;

            // Safety check if safe area is too small for paper
            if (minX > maxX) minX = maxX = (saMin.x + saMax.x) * 0.5f;
            if (minY > maxY) minY = maxY = (saMin.y + saMax.y) * 0.5f;

            float randomX = Random.Range(minX, maxX);
            float randomY = Random.Range(minY, maxY);
            
            Debug.Log($"[PaperPuzzle] Moving {paper.name}: RandomPos({randomX:F2}, {randomY:F2})");

            // Use the new method to update internal state of PaperDraggable
            paper.SetInitialPosition(new Vector3(randomX, randomY, rt.position.z));

            // Random Rotation (Scatter effect)
            float randomRot = Random.Range(-25f, 25f);
            rt.localEulerAngles = new Vector3(0, 0, randomRot);
        }
    }

    public void CheckWinCondition()
    {
        if (papers.Count == 0) return;

        // 1. Check if all papers are centered enough
        foreach (var paper in papers)
        {
            if (paper.GetComponent<RectTransform>().anchoredPosition.magnitude > winDistanceThreshold)
            {
                // Not centered yet
                return;
            }
        }

        // 2. Check if the visual order (Sibling Index) matches the required size order
        // We want Smallest on Top (High OrderIndex on High SiblingIndex)
        // Get papers sorted by their current Sibling Index (Bottom to Top)
        var currentStack = papers.OrderBy(p => p.transform.GetSiblingIndex()).ToList();

        for (int i = 0; i < currentStack.Count - 1; i++)
        {
            // If the current paper (lower in stack) has a higher OrderIndex (smaller size)
            // than the next paper (higher in stack), then it's wrong.
            // Requirement logic: 
            // Bottom of stack (i=0) should be Biggest (OrderIndex=0)
            // Top of stack (i=last) should be Smallest (OrderIndex=Max)
            
            // So OrderIndex should be Ascending as SiblingIndex increases
            if (currentStack[i].orderIndex > currentStack[i + 1].orderIndex)
            {
                // Wrong order
                return;
            }
        }

        // If we get here, valid!
        Debug.Log("Level 3 Cleared!");
        if (winUI != null) winUI.SetActive(true);
    }
}
