using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class PaperDraggable : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Puzzle Settings")]
    public int orderIndex; // 0 = Bottom (Largest), Higher = Top (Smallest)
    
    [Header("Movement Settings")]
    [Header("Movement Settings")]
    [SerializeField] private float maxTiltAngle = 15f;
    [SerializeField] private float smoothSpeed = 20f; // Faster smoothing
    [SerializeField] private float pickupScale = 1.05f;

    [Header("Snap Settings")]
    [SerializeField] private float snapEnterDistance = 250f; // Distance to engage magnet
    [SerializeField] private float snapExitDistance = 300f;  // Distance to break magnet (Hysteresis)
    [SerializeField] private float bounceStrength = 20f;
    [SerializeField] private float bounceDuration = 0.4f;

    public bool isSnapped = false;
    private bool isMagnetized = false; // Internal flag for "Close enough to target"

    private RectTransform rectTransform;
    private Canvas canvas;
    private Vector3 originalLocalScale; // Changed to Vector3
    private float targetRotation;
    
    // Tracking current "Virtual" position (where the mouse creates the paper to be)
    private Vector2 currentDragPos;
    private Vector2 bounceOffset; // Visual offset for the bounce animation
    
    private PaperPuzzle puzzleManager;
    private RectTransform currentSnapTarget;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        puzzleManager = FindObjectOfType<PaperPuzzle>();
        canvas = GetComponentInParent<Canvas>();
        originalLocalScale = transform.localScale;
    }

    private void Start()
    {
        // Initialize currentDragPos to current location so we don't snap to 0,0
        if (rectTransform != null)
        {
            currentDragPos = rectTransform.anchoredPosition;
        }
    }

    // Called by PaperPuzzle to randomize position without it being overwritten by Update
    public void SetInitialPosition(Vector3 newWorldPos)
    {
        transform.position = newWorldPos;
        if (rectTransform != null)
        {
            currentDragPos = rectTransform.anchoredPosition;
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        isSnapped = false;
        isMagnetized = false;
        StopAllCoroutines(); 
        
        // Initialize drag pos to current actual pos
        currentDragPos = rectTransform.anchoredPosition;
        bounceOffset = Vector2.zero;

        // Visuals
        transform.SetAsLastSibling();
        transform.localScale = originalLocalScale * pickupScale;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (canvas == null) return;

        // 1. Update the "Virtual" Drag Position based on Mouse Delta
        Vector2 deltaStep = eventData.delta / canvas.scaleFactor;
        currentDragPos += deltaStep;

        // 2. Clamp "Virtual" Pos to Safe Area
        currentDragPos = ClampToSafeArea(currentDragPos);

        // 3. Check Magnet Logic
        CheckMagnetLogic(currentDragPos);

        // 4. Calculate Tilt (Only if not magnetized)
        if (!isMagnetized)
        {
            if (Mathf.Abs(eventData.delta.x) > 0.1f)
            {
                float direction = Mathf.Sign(eventData.delta.x);
                targetRotation = -direction * maxTiltAngle; 
            }
        }
        else
        {
            targetRotation = 0f;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        transform.localScale = originalLocalScale;
        
        // If we are currently magnetized, confirm the snap
        if (isMagnetized && currentSnapTarget != null)
        {
            rectTransform.position = currentSnapTarget.position;
            rectTransform.localEulerAngles = Vector3.zero;
            isSnapped = true;
            Debug.Log($"[Snap Check] {name} confirmed snap to {currentSnapTarget.name}");
        }
        else
        {
            isSnapped = false;
            isMagnetized = false;
        }

        // Check Win Condition
        if (puzzleManager != null)
        {
            puzzleManager.CheckWinCondition();
        }
    }

    private void Update()
    {
        // Decide final target position (World Space for simplicity in logic, mostly Anchored driven)
        // But for smoothing, we iterate on anchored or world.
        
        // Strategy: We have currentDragPos (Anchored).
        // If Magnetized -> We want to be at currentSnapTarget.position (World).
        // If Not -> We want to be at currentDragPos (Anchored).
        
        Vector3 finalTargetPos = transform.position; // fallback

        if (isMagnetized && currentSnapTarget != null)
        {
            finalTargetPos = currentSnapTarget.position;
        }
        else
        {
            // Convert anchored drag pos to world for smooth lerp
            // Or just apply to anchoredPosition directly?
            // Let's smooth move towards the "Drag Pos" to avoid jitter, or direct set for responsiveness.
            // Direct set is better for drag feeling usually.
            
            // To be safe, we temporarily set anchored, then read world, then revert? 
            // Easier: Convert Anchor to World manually? Or just use anchoredPosition if parent is same?
            // Assuming flat hierarchy roughly. Let's just set anchoredPosition in Update if not magnetized.
            
            // Wait, we need to mix World (SnapTarget) and Anchored (Mouse).
            // Let's strictly use World Position for the actual Transform update.
            
            // Convert currentDragPos (Anchored) to World
            // Doing this is tricky if anchors are complex. 
            // Alternative: Set Anchored Position, then if Magnetized, Override World Position.
            rectTransform.anchoredPosition = currentDragPos;
            finalTargetPos = rectTransform.position; // This is where the mouse wants it
            
            if (isMagnetized && currentSnapTarget != null)
            {
                finalTargetPos = currentSnapTarget.position;
            }
        }

        // Apply visual bounce offset
        Vector3 visualPos = finalTargetPos + (Vector3)bounceOffset;

        // Apply to Rect (Lerp for smoothness or Direct?)
        // User wants "Langsung ke snap target" (Instant) but "Bounce".
        // So we set it fairly directly, but the BounceOffset handles the animation.
        // Let's add a small Lerp speed for general smoothness.
        
        rectTransform.position = Vector3.Lerp(rectTransform.position, visualPos, smoothSpeed * Time.deltaTime);
        
        // Rotation
        float currentAngle = rectTransform.localEulerAngles.z;
        if (currentAngle > 180) currentAngle -= 360;
        float smoothAngle = Mathf.Lerp(currentAngle, targetRotation, smoothSpeed * Time.deltaTime);
        rectTransform.localEulerAngles = new Vector3(0, 0, smoothAngle);
    }

    private void CheckMagnetLogic(Vector2 virtualAnchoredPos)
    {
        if (puzzleManager == null) return;

        // Find potential target
        RectTransform potentialTarget = null;

        if (orderIndex == 0)
        {
            potentialTarget = puzzleManager.snapTarget;
        }
        else
        {
            PaperDraggable predecessor = puzzleManager.GetPaperByIndex(orderIndex - 1);
            if (predecessor != null && predecessor.isSnapped)
            {
                potentialTarget = predecessor.GetComponent<RectTransform>();
            }
        }

        if (potentialTarget == null)
        {
            isMagnetized = false;
            currentSnapTarget = null;
            return;
        }

        // Calculate distance in World Space (most reliable)
        // We know where the mouse "wants" to be: converts virtualAnchoredPos to World?
        // Let's approximate: Use current Rect Position if not magnetized, or calculate relative?
        // Better: Use `rectTransform.TransformPoint(virtualAnchoredPos)` logic? NO.
        // Let's just create a temporary World Pos from the virtualAnchoredPos.
        // Hack: temporarily set anchored pos to check distance? Fast enough.
        Vector3 originalPos = rectTransform.anchoredPosition;
        rectTransform.anchoredPosition = virtualAnchoredPos;
        float dist = Vector2.Distance(rectTransform.position, potentialTarget.position);
        rectTransform.anchoredPosition = originalPos; // Revert
        
        // State Machine
        if (!isMagnetized)
        {
            // Try to Enter
            if (dist < snapEnterDistance)
            {
                isMagnetized = true;
                currentSnapTarget = potentialTarget;
                StartCoroutine(DoBounce());
            }
        }
        else
        {
            // Check if Broken (Hysteresis)
            if (dist > snapExitDistance || currentSnapTarget != potentialTarget)
            {
                isMagnetized = false;
                currentSnapTarget = null;
            }
        }
    }

    private IEnumerator DoBounce()
    {
        float timer = 0f;
        while (timer < bounceDuration)
        {
            timer += Time.deltaTime;
            float t = timer / bounceDuration;
            
            // Simple Punch/Elastic effect on Y axis or Scale?
            // User asked for "Bounce". Usually position wobble.
            float wobble = Mathf.Sin(t * Mathf.PI * 3f) * (bounceStrength * (1f - t));
            
            bounceOffset = new Vector2(0, wobble); // Bounce up/down slightly
            
            yield return null;
        }
        bounceOffset = Vector2.zero;
    }

    private Vector2 ClampToSafeArea(Vector2 candidatePos)
    {
        if (puzzleManager == null || puzzleManager.safeArea == null) return candidatePos;

        Vector3[] corners = new Vector3[4];
        puzzleManager.safeArea.GetWorldCorners(corners);
        Vector3 saMin = corners[0];
        Vector3 saMax = corners[2];
        
        // We need to convert World Safe Area limits to Anchored Position constraints?
        // Or just Clamp the final World Position?
        // Doing it on Anchored Pos is tricky without context.
        // Let's stick to the previous World-Clamping method inside OnDrag, but adapted.
        
        // Actually, to correctly clamp CurrentDragPos (which is Anchored), 
        // we should simply return candidatePos for now and rely on visual clamping?
        // Or implement the logic properly.
        // Let's ignore complex clamp refactor for safe area for a second to prioritize Magnet.
        // But to call `CheckMagnetLogic` accurately, we need `candidatePos`.
        // Let's skip detailed Safe Area clamping in this step to ensure functionality, 
        // OR reuse the previous logic.
        
        // Previous logic used World Position clamping.
        // Updating currentDragPos (Anchored) based on World Clamping:
        
        // 1. Calc World Pos from candidate Anchored
        Vector3 original = rectTransform.anchoredPosition;
        rectTransform.anchoredPosition = candidatePos;
        Vector3 worldPos = rectTransform.position;
        
        // 2. Clamp World
        // Use Original Scale to avoid locking when "Popped"
        // We approximate the world size by using the originalLocalScale relative to current lossyScale
        // Current lossyScale = parentLossy * localScale.
        // We want = parentLossy * originalLocalScale.
        // Factor = originalLocalScale / localScale (component wise)
        
        Vector3 scaleFactor = new Vector3(
            originalLocalScale.x / transform.localScale.x,
            originalLocalScale.y / transform.localScale.y,
            originalLocalScale.z / transform.localScale.z
        );
        
        Vector3 calcScale = Vector3.Scale(transform.lossyScale, scaleFactor);
        
        Vector2 size = Vector2.Scale(rectTransform.rect.size, calcScale);
        Vector2 extents = size * 0.5f;
        
        float minX = saMin.x + extents.x;
        float maxX = saMax.x - extents.x;
        float minY = saMin.y + extents.y;
        float maxY = saMax.y - extents.y;
        
        if (minX > maxX) minX = maxX = (saMin.x + saMax.x) * 0.5f;
        if (minY > maxY) minY = maxY = (saMin.y + saMax.y) * 0.5f;
        
        worldPos.x = Mathf.Clamp(worldPos.x, minX, maxX);
        worldPos.y = Mathf.Clamp(worldPos.y, minY, maxY);
        
        // 3. Convert back to Anchored
        rectTransform.position = worldPos;
        Vector2 clampedAnchored = rectTransform.anchoredPosition;
        rectTransform.anchoredPosition = original; // Revert
        
        return clampedAnchored;
    }
}
