using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Rigidbody2D))]
public class MakaroniScript : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private RectTransform rectTransform;
    private Canvas canvas;
    private CanvasGroup canvasGroup;
    private Rigidbody2D rb;

    [Header("Movement Settings")]
    public float smoothSpeed = 20f;
    private Vector2 virtualAnchoredPos;
    private bool isDragging = false;
    public bool isLocked = false;
    
    [Header("Physics UI Settings")]
    [Tooltip("Karena memakai Canvas (pixel), gravitasi normal 9.8 akan terasa melayang. Butuh dibesarkan.")]
    public float uiGravityMultiplier = 80f; 
    
    // Audio Rate Limiter untuk gesekan supaya tidak berisik
    private float lastSoundTime = 0f;
    private float soundCooldown = 0.2f;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
        
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
        
        rb = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        virtualAnchoredPos = rectTransform.anchoredPosition;
        rb.isKinematic = true; // Nonaktifkan gravitasi di awal sebelum di-drag
        
        // Memaksa gravitasi lebih besar agar tidak mengambang lambat di UI
        rb.gravityScale = uiGravityMultiplier; 
        
        // Atur Collision Detection ke Continuous agar saat jatuh cepat tidak tembus
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        
        // Mengurangi efek gasing menggelinding tak wajar
        rb.angularDrag = 15f; 
        rb.drag = 0f; // Gesekan udara nol agar jatuh cepat seperti gravitasi asli
    }

    private void FixedUpdate()
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
            
            // Terapkan batas safe area jika diset
            if (Level9Manager.Instance != null)
            {
                Level9Manager.Instance.ClampToSafeArea(ref targetWorldPos);
            }
            
            // MENGGUNAKAN VELOCITY FISIKA AGAR BISA MENTOK / TERTAHAN DINDING TOPLES
            Vector2 direction = (targetWorldPos - transform.position);
            rb.velocity = direction * smoothSpeed; 
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (isLocked) return;
        isDragging = true;
        
        // TETAP DYNAMIC agar collider tetap aktif berinteraksi dengan kaca toples
        rb.isKinematic = false; 
        rb.gravityScale = 0f; // Nol-kan gravitasi agar tidak jatuh saat dipegang mouse
        rb.velocity = Vector2.zero;
        rb.angularVelocity = 0f;
        
        virtualAnchoredPos = rectTransform.anchoredPosition;
        canvasGroup.blocksRaycasts = false;
        transform.SetAsLastSibling(); // Pindah ke layer paling atas
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (isLocked || canvas == null) return;
        Vector2 delta = eventData.delta / canvas.scaleFactor;
        virtualAnchoredPos += delta;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (isLocked) return;
        isDragging = false;
        canvasGroup.blocksRaycasts = true;
        
        // Sesuaikan kembali posisi virtual dengan hasil gerak fisika kalo ada sebelumnya
        virtualAnchoredPos = rectTransform.anchoredPosition;
        
        // Makaroni dilepas, nyalakan gravitasi agar jatuh
        rb.gravityScale = uiGravityMultiplier; 
        
        // Cek apakah posisi drop ada di area toples
        if (Level9Manager.Instance != null && Level9Manager.Instance.IsInToplesArea(rectTransform))
        {
            isLocked = true; // Gak bisa ditarik/dipindah lagi
            Level9Manager.Instance.IncrementMakaroni();
        }

        Level9Manager.Instance.PlayDropSFX();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (Time.time - lastSoundTime < soundCooldown) return;

        if (Level9Manager.Instance == null) return;

        string objName = collision.gameObject.name.ToLower();

        // 🔊 Makaroni tabrakan
        if (objName.Contains("mcmi"))
        {
            Level9Manager.Instance.PlayDropSFX();
            lastSoundTime = Time.time;
        }
        // 🔊 Kena toples kaca
        else if (objName.Contains("toples"))
        {
            Level9Manager.Instance.PlayToplesKacaSFX();
            lastSoundTime = Time.time;
        }
    }
}
