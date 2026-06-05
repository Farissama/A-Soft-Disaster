using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class PencilMovement : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler
{
    [Header("Puzzle Settings")]
    public int pencilId; 
    
    [Header("Control Settings")]
    [SerializeField] private float smoothSpeed = 15f;
    [SerializeField] private float tiltSensitivity = 0.3f;
    [SerializeField] private float maxTiltAngle = 15f;
    [SerializeField] private float pickupScaleFactor = 1.1f;
    
    [Header("Sticky Magnet Settings")]
    public RectTransform snapTarget;
    [SerializeField] private float magnetEnterDistance = 80f; 
    [SerializeField] private float magnetExitDistance = 150f; 

    [Header("Internal Tuning")]
    [SerializeField] private float normalSmoothSpeed = 20f;
    [SerializeField] private float magnetLerpSpeed = 15f; 

    private RectTransform rectTransform;
    private Canvas canvas;
    private PencilPuzzle manager;
    
    // State
    private Vector3 originalScale;
    private float targetRotationZ = 0f;
    private bool isDragging = false;
    public bool isSnapped = false; 
    
    // Virtual Drag Position (Penting agar tidak stuck!)
    private Vector2 virtualAnchoredPos; 

    // Magnet State
    private bool isMagnetized = false;
    private Vector3 stickyWorldPos;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    private void Start()
    {
        manager = GetComponentInParent<PencilPuzzle>();
        canvas = GetComponentInParent<Canvas>();
        originalScale = rectTransform.localScale;
        
        // Initial target is whatever the current rotation is
        targetRotationZ = rectTransform.localEulerAngles.z;
        if (targetRotationZ > 180) targetRotationZ -= 360;
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
            desiredRot = isMagnetized ? 0f : targetRotationZ;
        }
        else if (isSnapped)
        {
            desiredRot = 0f;
        }
        else
        {
            desiredRot = currentAngle;
        }

        float resultAngle = Mathf.LerpAngle(currentAngle, desiredRot, normalSmoothSpeed * Time.deltaTime);
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
        else if (isSnapped && snapTarget != null)
        {
            rectTransform.position = Vector3.Lerp(rectTransform.position, snapTarget.position, smoothSpeed * Time.deltaTime);
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        isDragging = true;
        isSnapped = false;
        isMagnetized = false;
        
        transform.SetAsLastSibling(); // Bring to front
        rectTransform.localScale = originalScale * pickupScaleFactor;
        
        // Target: Straighten Up immediately
        targetRotationZ = 0f;
        
        // Initialize virtual pos
        virtualAnchoredPos = rectTransform.anchoredPosition;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (canvas == null) return;

        // 1. Update VIRTUAL Position (MOUSE)
        Vector2 delta = eventData.delta / canvas.scaleFactor;
        virtualAnchoredPos += delta;
        
        // 2. Cek Magnet
        UpdateMagnetState();

        // 3. Visual Tilt (Hanya jika tidak nempel)
        if (!isMagnetized)
        {
            float targetTilt = Mathf.Clamp(-eventData.delta.x * tiltSensitivity, -maxTiltAngle, maxTiltAngle);
            targetRotationZ = targetTilt;
        }
        else
        {
            targetRotationZ = 0;
        }

        // Bounds check for virtual position to keep mouse inside
        if (manager != null && !isMagnetized)
        {
            // We use a temporary apply and check for virtual pos
            Vector2 savedPos = rectTransform.anchoredPosition;
            rectTransform.anchoredPosition = virtualAnchoredPos;
            manager.KeepInBounds(rectTransform);
            virtualAnchoredPos = rectTransform.anchoredPosition;
            rectTransform.anchoredPosition = savedPos;
        }
    }

    private void UpdateMagnetState()
    {
        Vector3 mouseWorldPos = GetWorldPosFromAnchored(virtualAnchoredPos);

        if (!isMagnetized)
        {
            if (snapTarget != null)
            {
                float d = Vector3.Distance(mouseWorldPos, snapTarget.position);
                if (d < magnetEnterDistance)
                {
                    isMagnetized = true;
                    stickyWorldPos = snapTarget.position;
                }
            }
        }
        else
        {
            // Cek jarak MOUSE (virtual) terhadap titik magnet
            float dist = Vector3.Distance(mouseWorldPos, stickyWorldPos);
            if (dist > magnetExitDistance)
            {
                isMagnetized = false;
            }
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        isDragging = false;
        rectTransform.localScale = originalScale;
        targetRotationZ = 0; 
        
        if (isMagnetized && snapTarget != null)
        {
            isSnapped = true;
            rectTransform.position = snapTarget.position;
        }
        else
        {
            isSnapped = false;
            // Ensure bounds on release
            if (manager != null)
            {
                manager.KeepInBounds(rectTransform);
            }
        }
        
        isMagnetized = false;

        if (manager != null)
        {
            manager.CheckWinCondition();
        }

        FindObjectOfType<PencilPuzzle>()?.PlayDropSFX();
    }

    public void SetInstantRotation(float z)
    {
        if (rectTransform == null) rectTransform = GetComponent<RectTransform>();
        rectTransform.localEulerAngles = new Vector3(0, 0, z);
        targetRotationZ = z;
        if (targetRotationZ > 180) targetRotationZ -= 360;
    }

    private Vector3 GetWorldPosFromAnchored(Vector2 anchored)
    {
        if (rectTransform.parent != null)
        {
            Vector3 localPos = new Vector3(anchored.x, anchored.y, 0f);
            return rectTransform.parent.TransformPoint(localPos);
        }
        return transform.position;
    }
}