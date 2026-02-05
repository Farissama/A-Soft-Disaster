using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class PencilMovement : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler
{
    [Header("Puzzle Settings")]
    public int pencilId; 
    
    [Header("Control Settings")]
    public float tiltSensitivity = 0.3f; // How much it leans when moving
    public float smoothSpeed = 20f;      // Rotation smoothing
    public float pickupScaleFactor = 1.1f; // Scale bump when grabbing
    
    private RectTransform rectTransform;
    private Canvas canvas;
    private PencilPuzzle manager;
    
    // State
    private float targetRotationZ;
    private Vector3 originalScale;
    private bool isDragging = false;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    private void Start()
    {
        manager = GetComponentInParent<PencilPuzzle>();
        canvas = GetComponentInParent<Canvas>();
        originalScale = rectTransform.localScale;
        
        // Initial target is whatever the randomizer set
        targetRotationZ = rectTransform.localEulerAngles.z;
    }

    private void Update()
    {
        // Smooth Rotation Logic
        // Always try to reach targetRotationZ
        float currentZ = rectTransform.localEulerAngles.z;
        float newZ = Mathf.LerpAngle(currentZ, targetRotationZ, Time.deltaTime * smoothSpeed);
        rectTransform.localEulerAngles = new Vector3(0, 0, newZ);
        
        // Logic: If dragging but not moving much, settle back to 0 (Straight)
        // This gives the "Straighten Up" feel when holding still
        if (isDragging)
        {
            // Decay target tilt back to 0 if no input adds to it?
            // Actually OnDrag sets the target. If Mouse stops, OnDrag stops firing.
            // We need to decay manually in Update if we consider 'tilt' as velocity-based.
            // But lets handle that in OnDrag or decay here.
            
            // Simple decay to 0 (Upright)
            targetRotationZ = Mathf.Lerp(targetRotationZ, 0, Time.deltaTime * 5f);
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        isDragging = true;
        transform.SetAsLastSibling(); // Bring to front
        
        // Pop Up
        rectTransform.localScale = originalScale * pickupScaleFactor;
        
        // Target: Straighten Up immediately
        targetRotationZ = 0f;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (canvas == null) return;

        // 1. Move
        rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;
        
        // 2. Tilt / Banking logic
        // Drag Right (+X) -> Tilt Right (CW / -Z)
        // Drag Left (-X) -> Tilt Left (CCW / +Z)
        float tilt = -eventData.delta.x * tiltSensitivity;
        
        // Apply tilt on top of 0 (Straight)
        // We set target directly to the tilt value. 
        // Since Update decays it to 0, continuous drag keeps it tilted.
        targetRotationZ = tilt;
        // Clamp tilt to avoid flipping
        targetRotationZ = Mathf.Clamp(targetRotationZ, -45f, 45f);

        // Bounds
        if (manager != null)
        {
            manager.KeepInBounds(rectTransform);
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        isDragging = false;
        
        // Pop Down
        rectTransform.localScale = originalScale;
        
        // Snap/Stay Straight
        targetRotationZ = 0f;
        
        if (manager != null)
        {
            manager.CheckWinCondition();
        }
    }

    // Public method for randomize - Updates target too so it doesn't snap back immediately
    public void SetInstantRotation(float z)
    {
        if (rectTransform == null) rectTransform = GetComponent<RectTransform>();
        
        rectTransform.localEulerAngles = new Vector3(0, 0, z);
        targetRotationZ = z;
    }
}