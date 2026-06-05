using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class AtkDraggable : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Identity")]
    public string itemTag = "Untagged"; 

    [Header("Visual Settings")]
    [SerializeField] private float smoothSpeed = 15f;
    [SerializeField] private float tiltSensitivity = 0.3f;
    [SerializeField] private float maxTiltAngle = 10f;
    [SerializeField] private float pickupScale = 1.1f;

    [Header("Sticky Magnet Settings")]
    [SerializeField] private float magnetEnterDistance = 80f; // Jarak untuk mulai nempel
    [SerializeField] private float magnetExitDistance = 150f; // Jarak untuk lepas (tarik mouse keluar radius ini)

    private RectTransform rectTransform;
    private Canvas canvas;
    private RectTransform safeAreaRect;
    private AtkSnapZone[] cachedZones;
    private AtkPuzzle manager;

    [Header("Internal Tuning")]
    [SerializeField] private float normalSmoothSpeed = 20f;
    [SerializeField] private float magnetLerpSpeed = 15f; 

    // State
    private Vector3 originalScale;
    private float targetRotation = 0f;
    private bool isDragging = false;
    public bool isSnapped = false; 
    
    // Virtual Drag Position (Penting agar tidak stuck!)
    // Ini melacak kemana MOUSE sebenarnya menarik barang
    private Vector2 virtualAnchoredPos; 

    // Magnet State
    private bool isMagnetized = false;
    private AtkSnapZone stickyZone;
    private Vector3 stickyWorldPos;

    // Final result
    private AtkSnapZone confirmedSnapZone; 

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
        manager = GetComponentInParent<AtkPuzzle>();
        originalScale = transform.localScale;
    }

    public void AssignSafeArea(RectTransform safeArea)
    {
        this.safeAreaRect = safeArea;
    }

    private void Update()
    {
        HandleRotation();
        HandlePositioning();
    }

    private void HandleRotation()
    {
        float currentAngle = rectTransform.localEulerAngles.z;
        if (currentAngle > 180) currentAngle -= 360;
        
        float desiredRot = 0f;

        if (isDragging)
        {
            desiredRot = isMagnetized ? 0f : targetRotation;
        }
        else if (isSnapped && confirmedSnapZone != null && confirmedSnapZone.zoneType == AtkSnapZone.ZoneType.Magnet)
        {
            desiredRot = 0f;
        }
        else
        {
            desiredRot = currentAngle;
        }

        float resultAngle = Mathf.Lerp(currentAngle, desiredRot, smoothSpeed * Time.deltaTime);
        rectTransform.localEulerAngles = new Vector3(0, 0, resultAngle);
    }

    private void HandlePositioning()
    {
        if (isDragging)
        {
            Vector3 targetWorldPos;
            float currentSmooth;

            if (isMagnetized)
            {
                // Smooth pull ke magnet
                targetWorldPos = stickyWorldPos;
                currentSmooth = magnetLerpSpeed;
            }
            else
            {
                // Smooth follow mouse (virtualPos)
                targetWorldPos = GetWorldPosFromAnchored(virtualAnchoredPos);
                currentSmooth = normalSmoothSpeed;
            }

            // Lerp World Position for absolute smoothness
            rectTransform.position = Vector3.Lerp(rectTransform.position, targetWorldPos, currentSmooth * Time.deltaTime);
        }
        else if (isSnapped && confirmedSnapZone != null && confirmedSnapZone.zoneType == AtkSnapZone.ZoneType.Magnet)
        {
            Vector3 targetPos = confirmedSnapZone.GetSnapPositionFor(this);
            rectTransform.position = Vector3.Lerp(rectTransform.position, targetPos, smoothSpeed * Time.deltaTime);
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        isDragging = true;
        isSnapped = false;
        isMagnetized = false;
        
        // Cache zones di awal drag agar OnDrag tidak berat (FindObjectsOfType)
        cachedZones = FindObjectsOfType<AtkSnapZone>();

        // Inisialisasi virtual position ke posisi saat ini
        virtualAnchoredPos = rectTransform.anchoredPosition;

        if (confirmedSnapZone != null)
        {
            confirmedSnapZone.ReleaseItem(this);
            confirmedSnapZone = null;
        }

        transform.SetAsLastSibling();
        transform.localScale = originalScale * pickupScale;
        targetRotation = 0f;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (canvas == null) return;

        // 1. Update VIRTUAL Position (MOUSE)
        Vector2 delta = eventData.delta / canvas.scaleFactor;
        virtualAnchoredPos += delta;
        
        if (safeAreaRect != null) virtualAnchoredPos = ClampToSafeArea(virtualAnchoredPos);

        // 2. Cek Magnet (Bisa di Update untuk stabilitas, tapi deteksi OnDrag cukup)
        UpdateMagnetState();

        // 3. Visual Tilt (Hanya jika tidak nempel)
        if (!isMagnetized)
        {
            float targetTilt = Mathf.Clamp(eventData.delta.x * tiltSensitivity * -1, -maxTiltAngle, maxTiltAngle);
            targetRotation = Mathf.Lerp(targetRotation, targetTilt, Time.deltaTime * 10f); // Smooth out tilt jumps
        }
        else
        {
            targetRotation = 0;
        }
    }

    private void UpdateMagnetState()
    {
        // Konversi virtualAnchoredPos (mouse) ke World Space untuk hitung jarak real
        // Ini kuncinya supaya tidak stuck!
        Vector3 mouseWorldPos = GetWorldPosFromAnchored(virtualAnchoredPos);

        if (!isMagnetized)
        {
            AtkSnapZone bestZone = null;
            Vector3 bestPos = Vector3.zero;
            float minD = magnetEnterDistance;

            if (cachedZones == null) cachedZones = FindObjectsOfType<AtkSnapZone>();

            foreach (var zone in cachedZones)
            {
                if (zone == null || zone.zoneType != AtkSnapZone.ZoneType.Magnet) continue;
                if (!zone.CanSnap(this)) continue;

                Vector3 target = zone.GetSnapPositionFor(this);
                float d = Vector3.Distance(mouseWorldPos, target);
                
                if (d < minD)
                {
                    minD = d;
                    bestZone = zone;
                    bestPos = target;
                }
            }

            if (bestZone != null)
            {
                isMagnetized = true;
                stickyZone = bestZone;
                stickyWorldPos = bestPos;
            }
        }
        else
        {
            // Cek jarak MOUSE (virtual) terhadap titik magnet
            // Jika mouse ditarik menjauh > magnetExitDistance, magnet LEPAS.
            float dist = Vector3.Distance(mouseWorldPos, stickyWorldPos);
            if (dist > magnetExitDistance)
            {
                isMagnetized = false;
                stickyZone = null;
            }
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        isDragging = false;
        transform.localScale = originalScale;
        targetRotation = 0; 
        
        FinalizeSnap();
        
        // Pastikan posisi final benar-benar masuk safe area jika tidak snap
        if (!isSnapped && safeAreaRect != null)
        {
             rectTransform.anchoredPosition = ClampToSafeArea(rectTransform.anchoredPosition);
        }
        
        isMagnetized = false;

        if (manager != null)
        {
            manager.CheckWinCondition();
        }

        FindObjectOfType<AtkPuzzle>()?.PlayDropSFX();
    }

    private void FinalizeSnap()
    {
        if (isMagnetized && stickyZone != null)
        {
            SnapTo(stickyZone);
            return;
        }

        if (cachedZones == null) cachedZones = FindObjectsOfType<AtkSnapZone>();

        foreach (var zone in cachedZones)
        {
            if (zone != null && zone.zoneType == AtkSnapZone.ZoneType.Loose && zone.IsInsideZone(rectTransform.position))
            {
                if (zone.CanSnap(this))
                {
                    SnapTo(zone);
                    break;
                }
            }
        }
    }

    public void SnapTo(AtkSnapZone zone)
    {
        confirmedSnapZone = zone;
        if (zone != null)
        {
            zone.SnapItem(this);
            isSnapped = true;
            targetRotation = 0;
        }
    }

    private Vector2 ClampToSafeArea(Vector2 candidateAnchoredPos)
    {
         if (safeAreaRect == null) return candidateAnchoredPos;
         Rect rect = safeAreaRect.rect;
         float clampedX = Mathf.Clamp(candidateAnchoredPos.x, rect.xMin, rect.xMax);
         float clampedY = Mathf.Clamp(candidateAnchoredPos.y, rect.yMin, rect.yMax);
         return new Vector2(clampedX, clampedY);
    }

    private Vector3 GetWorldPosFromAnchored(Vector2 anchored)
    {
        // Alternatif yang lebih ringan: gunakan parent transform point
        // Asumsi: anchored position relatif terhadap parent rect
        if (rectTransform.parent is RectTransform parentRT)
        {
            // AnchoredPosition biasanya dari pivot parent? 
            // Untuk simple drag, kita bisa gunakan pendekatan ini:
            Vector3 localPos = new Vector3(anchored.x, anchored.y, 0f);
            return rectTransform.parent.TransformPoint(localPos);
        }
        return transform.position; // Fallback
    }
}
