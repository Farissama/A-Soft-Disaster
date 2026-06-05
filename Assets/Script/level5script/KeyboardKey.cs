using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class KeyboardKey : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Identity")]
    [Tooltip("ID unik key (contoh: 'A').")]
    public string keyID; 
    [Tooltip("Ukuran key (contoh: 'Std'). Harus match dengan Slot.")]
    public string sizeTag = "Std"; 

    [Header("Visual Settings")]
    [SerializeField] private float smoothSpeed = 15f;
    [SerializeField] private float tiltSensitivity = 0.3f;
    [SerializeField] private float maxTiltAngle = 10f;
    [SerializeField] private float pickupScale = 1.1f;
    [SerializeField] private float magnetDistance = 50f;

    [Header("References")]
    private RectTransform rectTransform;
    private Canvas canvas;
    private KeyboardScript gameManager;

    // State
    private Vector3 originalScale;
    private KeyboardSlot currentSlot;
    private bool isDragging = false;
    private float targetRotation = 0f;
    
    // Magnet Preview
    private Vector3 magnetPreviewPos;
    private bool isMagnetized = false;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
        gameManager = FindObjectOfType<KeyboardScript>();
        originalScale = transform.localScale;
    }

    private void Update()
    {
        HandleRotation();
        HandleSnapMovement();
    }

    private void HandleRotation()
    {
        float currentAngle = rectTransform.localEulerAngles.z;
        if (currentAngle > 180) currentAngle -= 360;
        
        float desiredRot = 0f;

        if (isDragging)
        {
            // Saat di-drag, rotasi sesuai arah mouse (tilt) atau 0 jika kena magnet
            desiredRot = isMagnetized ? 0f : targetRotation;
        }
        else if (currentSlot != null)
        {
            // Jika snapped, luruskan
            desiredRot = 0f;
        }
        else
        {
            // Jika loose/idle, pertahankan rotasi acak
            desiredRot = currentAngle;
        }

        float resultAngle = Mathf.Lerp(currentAngle, desiredRot, smoothSpeed * Time.deltaTime);
        rectTransform.localEulerAngles = new Vector3(0, 0, resultAngle);
    }

    private void HandleSnapMovement()
    {
        // Jika sudah snapped di slot dan tidak sedang di-drag, pastikan posisi pas di tengah slot
        if (currentSlot != null && !isDragging)
        {
            rectTransform.position = Vector3.Lerp(rectTransform.position, currentSlot.transform.position, smoothSpeed * Time.deltaTime);
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        isDragging = true;
        isMagnetized = false;
        
        // Lepas dari slot lama jika ada
        if (currentSlot != null)
        {
            currentSlot.occupiedKey = null;
            currentSlot = null;
        }

        transform.SetAsLastSibling(); // Render di paling atas agar tidak tertutup
        transform.localScale = originalScale * pickupScale; // Efek "pop up"
        
        // Memastikan Canvas ditemukan untuk konversi drag
        if (canvas == null) canvas = GetComponentInParent<Canvas>();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (canvas == null) return;

        // 1. Follow Mouse
        Vector2 delta = eventData.delta / canvas.scaleFactor;
        rectTransform.anchoredPosition += delta;

        // 2. Tile Effect (Miring saat digeser cepat)
        if (!isMagnetized)
        {
            float tilt = Mathf.Clamp(eventData.delta.x * tiltSensitivity * -1, -maxTiltAngle, maxTiltAngle);
            targetRotation = tilt;
        }
        else
        {
            targetRotation = 0;
        }

        // 3. Cek Magnet Preview (Visual Feedback sebelum lepas)
        CheckMagnetPreview();
        
        if (isMagnetized)
        {
             // Opsional: Tarik dikit visual ke magnet target biar kerasa "sticky"
             rectTransform.position = Vector3.Lerp(rectTransform.position, magnetPreviewPos, smoothSpeed * Time.deltaTime);
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        isDragging = false;
        isMagnetized = false;
        transform.localScale = originalScale;
        targetRotation = 0; 
        
        FinalizeSnap();
        
        // Cek kemenangan setiap kali lepas drag
        if (gameManager != null)
        {
            gameManager.CheckWin();
        }

        FindObjectOfType<KeyboardScript>()?.PlayDropSFX();
    }

    // Mencari slot terdekat untuk visual feedback saat drag
    private void CheckMagnetPreview()
    {
        KeyboardSlot bestSlot = FindBestSlot();
        
        if (bestSlot != null)
        {
            isMagnetized = true;
            magnetPreviewPos = bestSlot.transform.position;
        }
        else
        {
            isMagnetized = false;
        }
    }

    // Finalisasi snap saat mouse dilepas
    private void FinalizeSnap()
    {
        KeyboardSlot bestSlot = FindBestSlot();

        if (bestSlot != null)
        {
            SnapTo(bestSlot);
        }
        else
        {
            // Jika tidak ada slot valid, biarkan di posisi terakhir (Loose)
            // Pastikan tidak keluar layar (bisa tambah clamp logic jika perlu)
        }
    }

    private KeyboardSlot FindBestSlot()
    {
        KeyboardSlot nearestSlot = null;
        float minDistance = float.MaxValue;
        
        // Cari semua slot. (Optimasi: Bisa di-cache di GameManager jika berat)
        KeyboardSlot[] allSlots = FindObjectsOfType<KeyboardSlot>();
        
        foreach (var slot in allSlots)
        {
            // Skip slot yang sudah penuh (kecuali oleh diri sendiri - edge case re-snap)
            if (slot.occupiedKey != null && slot.occupiedKey != this) continue;

            // Cek jarak
            float dist = Vector3.Distance(transform.position, slot.transform.position);
            
            // Logika Magnet
            if (dist < magnetDistance && dist < minDistance)
            {
                // SYARAT UTAMA: Ukuran harus sama
                if (slot.CanAccept(this))
                {
                    minDistance = dist;
                    nearestSlot = slot;
                }
            }
        }
        return nearestSlot;
    }

    public void SnapTo(KeyboardSlot slot)
    {
        // 1. Lepas dari slot lama jika ada
        if (currentSlot != null)
        {
            currentSlot.occupiedKey = null;
        }

        // 2. Pasang ke slot baru
        currentSlot = slot;
        if (slot != null)
        {
            slot.occupiedKey = this;
            targetRotation = 0;
        }

        FindObjectOfType<KeyboardScript>()?.PlaySnapSFX();
    }
}
