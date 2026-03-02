using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class PencilPuzzle : MonoBehaviour
{
    [Header("Configuration")]
    public RectTransform playArea; // The red boundary area
    public GameObject winUI;
    
    [Header("Settings")]
    public float winRotationTolerance = 2f; // Degrees
    public float padding = 50f; // Padding from playArea edges
    
    [Header("Snap Configuration")]
    public Transform snapAreaContainer; // Parent of the snap targets
    
    private List<PencilMovement> pencils = new List<PencilMovement>();

    void Start()
    {
        // Auto-find pencils if not assigned
        pencils = new List<PencilMovement>(GetComponentsInChildren<PencilMovement>());
        
        // Auto-assign snap targets
        if (snapAreaContainer != null)
        {
            AssignSnapTargets();
        }
        else
        {
            // Try to find it by name if not assigned
            GameObject snapObj = GameObject.Find("snaparea");
            if (snapObj != null)
            {
                snapAreaContainer = snapObj.transform;
                AssignSnapTargets();
            }
        }
        
        if (pencils.Count > 0)
        {
            RandomizePencils();
        }
    }
    
    void AssignSnapTargets()
    {
        // Assumption: Snap Targets are children of snapAreaContainer
        // And they are ordered 1 to 6 (left to right) matching IDs or just order?
        // Let's assume name matching 'pen1' -> 'pen1' OR order matching.
        // User screenshot showed 'pen1' inside 'snaparea'.
        
        foreach (var pen in pencils)
        {
            // Try to find child with same name
            Transform target = snapAreaContainer.Find(pen.name);
            if (target != null)
            {
                pen.snapTarget = target.GetComponent<RectTransform>();
            }
            else
            {
                // Fallback: Try by index ?
                // If pen names are 'pen1', 'pen2', etc. this should work.
            }
        }
    }

    public void RandomizePencils()
    {
        if (playArea == null)
        {
            Debug.LogError("PencilPuzzle: Play Area is not assigned!");
            return;
        }

        Rect areaRect = playArea.rect;
        
        foreach (var pen in pencils)
        {
            RectTransform rt = pen.GetComponent<RectTransform>();
            
            // Random Rotation first
            int randomStep = Random.Range(0, 8); 
            float randomAngle = randomStep * 45f;
            
            // Use the movement script to set rotation so it syncs with the smoothing target
            pen.SetInstantRotation(randomAngle);
            
            // Calculate size of the pencil to determine safe margins
            // Assuming simplified AABB for spawn check (using max dimension is safe)
            float maxDim = Mathf.Max(rt.rect.width * rt.localScale.x, rt.rect.height * rt.localScale.y);
            float safeMargin = (maxDim / 2f) + padding;

            float minX = areaRect.xMin + safeMargin;
            float maxX = areaRect.xMax - safeMargin;
            float minY = areaRect.yMin + safeMargin;
            float maxY = areaRect.yMax - safeMargin;
            
            // Ensure min <= max
            if (minX > maxX) minX = maxX = areaRect.center.x;
            if (minY > maxY) minY = maxY = areaRect.center.y;

            // Random Position
            float randomX = Random.Range(minX, maxX);
            float randomY = Random.Range(minY, maxY);
            rt.anchoredPosition = new Vector2(randomX, randomY);
        }
    }

    public void KeepInBounds(RectTransform pencilRect)
    {
        if (playArea == null) return;

        Vector3 pos = pencilRect.anchoredPosition;
        Rect areaRect = playArea.rect;
        Rect pencilRectLocal = pencilRect.rect;
        
        // Calculate Projected Size (AABB) based on rotation
        // This ensures that even if rotated, the corners won't clip.
        float angleRad = pencilRect.localEulerAngles.z * Mathf.Deg2Rad;
        float w = pencilRectLocal.width * pencilRect.localScale.x;
        float h = pencilRectLocal.height * pencilRect.localScale.y;

        // Formula for AABB width/height of rotated rectangle
        float projectedWidth = Mathf.Abs(w * Mathf.Cos(angleRad)) + Mathf.Abs(h * Mathf.Sin(angleRad));
        float projectedHeight = Mathf.Abs(w * Mathf.Sin(angleRad)) + Mathf.Abs(h * Mathf.Cos(angleRad));
        
        // Pivot offset compensation if pivot is not center
        // To simplify, we enforce bounds based on the EXTENTS from the pivot.
        // But AABB assumes center.
        // Let's rely on half-extents for clamping Center Position, assuming Pivot roughly centers the object interactions OR we compensate.
        // Since we use Virtual Pivot in Drag, the actual RectTransform pivot might stay wherever (likely center if user reset, or whatever Randomize/Prefab set).
        // Let's assume Pivot 0.5,0.5 for boundary logic to be safest or calculate edges.
        
        float halfW = projectedWidth / 2f;
        float halfH = projectedHeight / 2f;
        
        // Note: This bounds check assumes the AnchoredPosition represents the CENTER. 
        // If pivot is not center, this will be slightly off. 
        // Correcting for pivot is complex without world space, but AABB at center is robust enough for "Safe Area".
        // Let's add extra padding to be safe.
        
        float allowedMinX = areaRect.xMin + halfW + padding;
        float allowedMaxX = areaRect.xMax - halfW - padding;
        float allowedMinY = areaRect.yMin + halfH + padding;
        float allowedMaxY = areaRect.yMax - halfH - padding;
        
        // Handle cases where area is too small
        if (allowedMinX > allowedMaxX) allowedMinX = allowedMaxX = areaRect.center.x;
        if (allowedMinY > allowedMaxY) allowedMinY = allowedMaxY = areaRect.center.y;

        float clampedX = Mathf.Clamp(pos.x, allowedMinX, allowedMaxX);
        float clampedY = Mathf.Clamp(pos.y, allowedMinY, allowedMaxY);
        
        pencilRect.anchoredPosition = new Vector3(clampedX, clampedY, 0);
    }

    public void CheckWinCondition()
    {
        // 1. Check if ALL pencils are neatly snapped
        bool allSnapped = true;
        foreach (var pen in pencils)
        {
            if (!pen.isSnapped)
            {
                allSnapped = false;
                break;
            }
        }

        if (!allSnapped)
        {
            // Debug.Log("Still sorting... Pencils are not all snapped yet.");
            return; 
        }

        // 2. Sort pencils list based on current X position to verify sequence
        List<PencilMovement> sortedPencils = pencils.OrderBy(p => p.GetComponent<RectTransform>().anchoredPosition.x).ToList();
        
        // 3. Check Sequence (Descending: Big ID Left, Small ID Right)
        bool sequenceCorrect = true;
        int count = sortedPencils.Count;
        
        for (int i = 0; i < count; i++)
        {
            if (sortedPencils[i].pencilId != (count - i))
            {
                sequenceCorrect = false;
                break;
            }
        }

        // 4. Check Rotations (Snapped pencils should already be at 0, but extra safety check)
        bool rotationCorrect = true;
        foreach (var pen in sortedPencils)
        {
            float z = pen.GetComponent<RectTransform>().localEulerAngles.z;
            if (z > 180) z -= 360;
            
            if (Mathf.Abs(z) > winRotationTolerance)
            {
                rotationCorrect = false;
                break;
            }
        }

        if (sequenceCorrect && rotationCorrect)
        {
            Debug.Log("WIN! All pencils are snapped, sorted, and upright.");
            if (winUI != null) winUI.SetActive(true);
            
            foreach(var p in pencils) p.enabled = false;
        }
    }
}