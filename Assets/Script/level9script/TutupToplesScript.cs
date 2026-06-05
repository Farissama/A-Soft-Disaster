using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

public class TutupToplesScript : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private RectTransform rectTransform;
    private Canvas canvas;
    private CanvasGroup canvasGroup;

    [Header("Snap Settings")]
    [Tooltip("Target area tutup toples (bisa pakai snaptutuptoples)")]
    public RectTransform snapTarget; 
    public float snapDistance = 150f;
    
    [Header("Animation Settings")]
    public float topOffsetOffset = 30f; // Lebih pendek jedanya supaya nggak mental ke atas layar
    public float animationDuration = 0.8f; // Lebih diperlama supaya halus dan nampak jelas

    [Header("Movement")]
    public float smoothSpeed = 20f;
    
    private Vector2 virtualAnchoredPos;
    private bool isDragging = false;
    private bool isSnapped = false;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
        
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    private void Start()
    {
        virtualAnchoredPos = rectTransform.anchoredPosition;
    }

    private void Update()
    {
        if (isDragging)
        {
            Vector3 targetWorldPos;
            if (rectTransform.parent is RectTransform parentRT)
            {
                Vector3 localPos = new Vector3(virtualAnchoredPos.x, virtualAnchoredPos.y, 0f);
                targetWorldPos = rectTransform.parent.TransformPoint(localPos);
            }
            else
            {
                targetWorldPos = transform.position; 
            }
            rectTransform.position = Vector3.Lerp(rectTransform.position, targetWorldPos, smoothSpeed * Time.deltaTime);
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        // Tutup toples bisa ditarik walau sudah snapped
        isDragging = true;
        isSnapped = false;
        
        // Hentikan animasi menutupi jika ada
        StopAllCoroutines();
        
        // Kirim sinyal dicopot
        if (Level9Manager.Instance != null)
        {
            Level9Manager.Instance.TutupRemoved();
        }

        virtualAnchoredPos = rectTransform.anchoredPosition;
        canvasGroup.blocksRaycasts = false;
        transform.SetAsLastSibling();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (canvas == null) return;
        Vector2 delta = eventData.delta / canvas.scaleFactor;
        virtualAnchoredPos += delta;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        isDragging = false;
        canvasGroup.blocksRaycasts = true;

        if (snapTarget != null)
        {
            float dist = Vector3.Distance(transform.position, snapTarget.position);
            if (dist <= snapDistance)
            {
                isSnapped = true;
                StartCoroutine(TutupAnimationRoutine());
            }
        }
    }

    private IEnumerator TutupAnimationRoutine()
    {
        // 1. Ambil posisi target magnetnya
        Vector3 targetPos = snapTarget.position;
        // 2. Buat posisi "Agak Atas"
        Vector3 abovePos = targetPos + new Vector3(0, topOffsetOffset, 0); 
        
        // Langsung snap ke titik yang agak atas tersebut (jeda sebelum nutup jar)
        rectTransform.position = abovePos;
        
        // Tunggu sedikit agar memberikan efek "jeda" 
        yield return new WaitForSeconds(0.2f);
        
        // Gerak mulus menutup dari atas ke snap target di bawah
        float elapsed = 0f;
        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / animationDuration;
            
            // Gunakan ease-in (t * t) biar makin ngebut ke bawah seperti membanting nutupnya
            rectTransform.position = Vector3.Lerp(abovePos, targetPos, t * t);
            yield return null;
        }
        
        rectTransform.position = targetPos;
        
        // Kasih info ke Manager kalau tutup berhasil disegel
        if (Level9Manager.Instance != null)
        {
            Level9Manager.Instance.TutupPlaced();
        }
    }
}
