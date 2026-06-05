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

    // 🔊 SFX (DITAMBAHKAN)
    [SerializeField] private AudioClip pencilDropSFX;
    private AudioSource audioSource;
    
    private List<PencilMovement> pencils = new List<PencilMovement>();

    void Start()
    {
        // 🔊 INIT AUDIO (DITAMBAHKAN)
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // Auto-find pencils if not assigned
        pencils = new List<PencilMovement>(GetComponentsInChildren<PencilMovement>());
        
        // Auto-assign snap targets
        if (snapAreaContainer != null)
        {
            AssignSnapTargets();
        }
        else
        {
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
        foreach (var pen in pencils)
        {
            Transform target = snapAreaContainer.Find(pen.name);
            if (target != null)
            {
                pen.snapTarget = target.GetComponent<RectTransform>();
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
            
            int randomStep = Random.Range(0, 8); 
            float randomAngle = randomStep * 45f;
            
            pen.SetInstantRotation(randomAngle);
            
            float maxDim = Mathf.Max(rt.rect.width * rt.localScale.x, rt.rect.height * rt.localScale.y);
            float safeMargin = (maxDim / 2f) + padding;

            float minX = areaRect.xMin + safeMargin;
            float maxX = areaRect.xMax - safeMargin;
            float minY = areaRect.yMin + safeMargin;
            float maxY = areaRect.yMax - safeMargin;
            
            if (minX > maxX) minX = maxX = areaRect.center.x;
            if (minY > maxY) minY = maxY = areaRect.center.y;

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
        
        float angleRad = pencilRect.localEulerAngles.z * Mathf.Deg2Rad;
        float w = pencilRectLocal.width * pencilRect.localScale.x;
        float h = pencilRectLocal.height * pencilRect.localScale.y;

        float projectedWidth = Mathf.Abs(w * Mathf.Cos(angleRad)) + Mathf.Abs(h * Mathf.Sin(angleRad));
        float projectedHeight = Mathf.Abs(w * Mathf.Sin(angleRad)) + Mathf.Abs(h * Mathf.Cos(angleRad));
        
        float halfW = projectedWidth / 2f;
        float halfH = projectedHeight / 2f;
        
        float allowedMinX = areaRect.xMin + halfW + padding;
        float allowedMaxX = areaRect.xMax - halfW - padding;
        float allowedMinY = areaRect.yMin + halfH + padding;
        float allowedMaxY = areaRect.yMax - halfH - padding;
        
        if (allowedMinX > allowedMaxX) allowedMinX = allowedMaxX = areaRect.center.x;
        if (allowedMinY > allowedMaxY) allowedMinY = allowedMaxY = areaRect.center.y;

        float clampedX = Mathf.Clamp(pos.x, allowedMinX, allowedMaxX);
        float clampedY = Mathf.Clamp(pos.y, allowedMinY, allowedMaxY);
        
        pencilRect.anchoredPosition = new Vector3(clampedX, clampedY, 0);
    }

    public void CheckWinCondition()
    {
        bool allSnapped = true;
        foreach (var pen in pencils)
        {
            if (!pen.isSnapped)
            {
                allSnapped = false;
                break;
            }
        }

        if (!allSnapped) return; 

        List<PencilMovement> sortedPencils = pencils.OrderBy(p => p.GetComponent<RectTransform>().anchoredPosition.x).ToList();
        
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

    // 🔊 FUNCTION SFX (DITAMBAHKAN)
    public void PlayDropSFX()
    {
        if (audioSource != null && pencilDropSFX != null)
        {
            audioSource.pitch = Random.Range(0.95f, 1.05f);
            audioSource.PlayOneShot(pencilDropSFX);
        }
    }
}