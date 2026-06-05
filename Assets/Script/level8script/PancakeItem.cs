using UnityEngine;
using UnityEngine.EventSystems;

public class PancakeItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Settings")]
    public RectTransform snapTarget; 
    public bool isStrawberry = false;
    
    [Header("Visual Settings")]
    [SerializeField] private float tiltSensitivity = 0.3f;
    [SerializeField] private float maxTiltAngle = 10f;
    [SerializeField] private float pickupScale = 1.1f;
    
    [Header("Magnet Settings")]
    [SerializeField] private float magnetEnterDistance = 80f;
    [SerializeField] private float magnetExitDistance = 150f;

    [Header("Internal Tuning")]
    [SerializeField] private float normalSmoothSpeed = 20f;
    [SerializeField] private float magnetLerpSpeed = 15f; 

    [Header("State")]
    public bool isSnapped = false;
    public bool isLocked = false; 
    public bool isMagnetized = false;

    private RectTransform rectTransform;
    private Canvas canvas;
    private CanvasGroup canvasGroup;
    
    private Vector3 originalLocalScale;
    private Vector2 virtualAnchoredPos;
    
    private float targetTiltRotation;
    private bool isDragging = false;
    
    private PancakeScript manager;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
        originalLocalScale = transform.localScale;
        
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
            
        manager = FindObjectOfType<PancakeScript>();
    }

    private void Start()
    {
        if (rectTransform != null)
        {
            virtualAnchoredPos = rectTransform.anchoredPosition;
        }
    }

    public void SyncPosition()
    {
        if (rectTransform != null)
        {
            virtualAnchoredPos = rectTransform.anchoredPosition;
        }
    }

    public void SetLocked(bool locked)
    {
        isLocked = locked;
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
            if (isMagnetized && snapTarget != null)
            {
                float targetAngle = snapTarget.localEulerAngles.z;
                if (targetAngle > 180) targetAngle -= 360;
                desiredRot = targetAngle;
            }
            else
            {
                desiredRot = targetTiltRotation;
            }
        }
        else if (isSnapped && snapTarget != null)
        {
            float targetAngle = snapTarget.localEulerAngles.z;
            if (targetAngle > 180) targetAngle -= 360;
            desiredRot = targetAngle;
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

            if (isMagnetized && snapTarget != null)
            {
                targetWorldPos = snapTarget.position;
                currentSmooth = magnetLerpSpeed;
            }
            else
            {
                if (rectTransform.parent is RectTransform parentRT)
                {
                    Vector3 localPos = new Vector3(virtualAnchoredPos.x, virtualAnchoredPos.y, 0f);
                    targetWorldPos = rectTransform.parent.TransformPoint(localPos);
                }
                else
                {
                    targetWorldPos = transform.position;
                }
                currentSmooth = normalSmoothSpeed;
            }

            rectTransform.position = Vector3.Lerp(rectTransform.position, targetWorldPos, currentSmooth * Time.deltaTime);
        }
        else if (isSnapped && snapTarget != null)
        {
            rectTransform.position = Vector3.Lerp(rectTransform.position, snapTarget.position, normalSmoothSpeed * Time.deltaTime);
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (isLocked) return;

        if (isSnapped)
        {
            isSnapped = false;
        }

        isDragging = true;
        isMagnetized = false;
        virtualAnchoredPos = rectTransform.anchoredPosition;

        canvasGroup.blocksRaycasts = false;
        transform.SetAsLastSibling();
        
        transform.localScale = originalLocalScale * pickupScale;
        targetTiltRotation = 0f;

    }

    public void OnDrag(PointerEventData eventData)
    {
        if (isLocked || isSnapped || canvas == null) return;

        Vector2 delta = eventData.delta / canvas.scaleFactor;
        virtualAnchoredPos += delta;

        UpdateMagnetState();

        if (!isMagnetized)
        {
            float targetTilt = Mathf.Clamp(eventData.delta.x * tiltSensitivity * -1, -maxTiltAngle, maxTiltAngle);
            targetTiltRotation = Mathf.Lerp(targetTiltRotation, targetTilt, Time.deltaTime * 10f);
        }
        else
        {
            targetTiltRotation = 0;
        }
    }

    private void UpdateMagnetState()
    {
        if (snapTarget == null) return;

        Vector3 mouseWorldPos;
        if (rectTransform.parent is RectTransform parentRT)
        {
            Vector3 localPos = new Vector3(virtualAnchoredPos.x, virtualAnchoredPos.y, 0f);
            mouseWorldPos = rectTransform.parent.TransformPoint(localPos);
        }
        else
        {
            mouseWorldPos = transform.position;
        }

        if (!isMagnetized)
        {
            float d = Vector3.Distance(mouseWorldPos, snapTarget.position);
            if (d < magnetEnterDistance)
            {
                isMagnetized = true;
            }
        }
        else
        {
            float dist = Vector3.Distance(mouseWorldPos, snapTarget.position);
            if (dist > magnetExitDistance)
            {
                isMagnetized = false;
            }
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (isLocked) return;

        isDragging = false;
        transform.localScale = originalLocalScale;
        targetTiltRotation = 0f;
        canvasGroup.blocksRaycasts = true;

        if (isMagnetized && snapTarget != null)
        {
            rectTransform.position = snapTarget.position;
            isSnapped = true;

            if (manager != null)
            {
                manager.PlaySnapSFX(); // 🔊 SNAP
                manager.CheckWinCondition();
            }
        }
        else
        {
            if (manager != null)
            {
                manager.PlayDropSFX(); // 🔊 DROP
            }
        }
        
        isMagnetized = false;
    }
}