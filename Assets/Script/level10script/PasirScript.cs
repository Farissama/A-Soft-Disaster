using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class PasirScript : MonoBehaviour
{
    [Header("UI Groups")]
    public Transform atasGroup;
    public Transform bawahGroup;
    public GameObject winUI;
    
    [Header("Audio")]
    public AudioSource sfxAudioSource; 
    public AudioClip clinkSound;
    
    // Lists untuk menyimpan data slot dan item
    private List<Vector3> atasSlots = new List<Vector3>();
    private List<Vector3> bawahSlots = new List<Vector3>();
    
    public List<PasirDragItem> atasJars = new List<PasirDragItem>();
    public List<PasirDragItem> bawahJars = new List<PasirDragItem>();
    
    private bool isInitialized = false;

    IEnumerator Start()
    {
        if (atasGroup == null) atasGroup = GameObject.Find("atas")?.transform;
        if (bawahGroup == null) bawahGroup = GameObject.Find("bawah")?.transform;
        
        // Tunggu satu frame agar Horizontal Layout Group sempat mengatur posisi dengan benar
        yield return null; 
        
        if (atasGroup != null) SetupGroup(atasGroup, atasJars, atasSlots);
        if (bawahGroup != null) SetupGroup(bawahGroup, bawahJars, bawahSlots);
        
        // Setelah posisi X dan Y dicatat sebagai slot, MATIKAN Horizontal Layout Group
        // Ini memungkinkan kita menggerakkan toples secara manual (smooth lerp) seperti di game Buku
        RemoveLayoutGroup(atasGroup);
        RemoveLayoutGroup(bawahGroup);
        
        if (atasJars.Count > 0) ShuffleJars(atasJars, atasSlots);
        if (bawahJars.Count > 0) ShuffleJars(bawahJars, bawahSlots);
        
        isInitialized = true;
    }
    
    void SetupGroup(Transform group, List<PasirDragItem> jarList, List<Vector3> slotList)
    {
        for (int i = 0; i < group.childCount; i++)
        {
            Transform child = group.GetChild(i);
            
            // Simpan posisi slot asli hasil dari Horizontal Layout Group
            slotList.Add(child.localPosition);
            
            PasirDragItem item = child.gameObject.GetComponent<PasirDragItem>();
            if (item == null) item = child.gameObject.AddComponent<PasirDragItem>();
            
            item.manager = this;
            item.correctIndex = i; 
            item.originalGroup = group;
            item.targetPosition = child.localPosition; // Target awal
            item.lockedY = child.localPosition.y;      // Kunci pergerakan vertikal
            
            // Tambah Alpha Hit Test agar drag responsif cuma di area gambar yang terlihat
            Image img = child.GetComponent<Image>();
            if (img != null && img.sprite != null && img.sprite.texture != null)
            {
                // Harus centang "Read/Write" di setting Sprite inspector untuk Alpha Hit Test
                if (img.sprite.texture.isReadable)
                {
                    img.alphaHitTestMinimumThreshold = 0.1f;
                }
            }

            jarList.Add(item);
        }
    }

    void RemoveLayoutGroup(Transform group)
    {
        if (group != null)
        {
            HorizontalLayoutGroup hlg = group.GetComponent<HorizontalLayoutGroup>();
            if (hlg != null) Destroy(hlg); // Buang layout group supaya bisa digeser bebas
        }
    }
    
    void ShuffleJars(List<PasirDragItem> jarList, List<Vector3> slotList)
    {
        for (int i = 0; i < jarList.Count; i++)
        {
            PasirDragItem temp = jarList[i];
            int randomIndex = Random.Range(i, jarList.Count);
            jarList[i] = jarList[randomIndex];
            jarList[randomIndex] = temp;
        }
        
        UpdateTargets(jarList, slotList);
    }

    public void OnJarDragged(PasirDragItem draggedItem, Transform group)
    {
        if (!isInitialized) return;

        List<PasirDragItem> jarList = (group == atasGroup) ? atasJars : bawahJars;
        List<Vector3> slotList = (group == atasGroup) ? atasSlots : bawahSlots;

        // Hitung slot mana yang paling dekat dengan kursor (posisi toples ini)
        float currentX = draggedItem.transform.localPosition.x;
        int newIndex = GetClosestSlotIndex(currentX, slotList);
        int oldIndex = jarList.IndexOf(draggedItem);
        
        if (newIndex != oldIndex && newIndex >= 0 && newIndex < jarList.Count)
        {
            // Tukar posisi di list item
            jarList.RemoveAt(oldIndex);
            jarList.Insert(newIndex, draggedItem);
            
            // Update target untuk semua list
            UpdateTargets(jarList, slotList);
            PlayClinkSound(); // Suara benturan
        }
    }
    
    int GetClosestSlotIndex(float xPos, List<Vector3> slotList)
    {
        if (slotList.Count == 0) return 0;
        
        int bestIndex = 0;
        float minDistance = float.MaxValue;
        
        for (int i = 0; i < slotList.Count; i++)
        {
            float dist = Mathf.Abs(slotList[i].x - xPos);
            if (dist < minDistance)
            {
                minDistance = dist;
                bestIndex = i;
            }
        }
        return bestIndex;
    }

    public void UpdateTargets(List<PasirDragItem> jarList, List<Vector3> slotList)
    {
        for (int i = 0; i < jarList.Count; i++)
        {
            jarList[i].targetPosition = slotList[i];
            // Sinkronkan layer (z-order) berdasarkan urutan supaya terlihat urut
            jarList[i].transform.SetSiblingIndex(i); 
        }
    }
    
    public void CheckWin()
    {
        if (!isInitialized) return;

        if (IsGroupSorted(atasJars) && IsGroupSorted(bawahJars))
        {
            Debug.Log("Puzzles Solved! Toples Pasir sudah terurut.");
            if (winUI != null) winUI.SetActive(true);
        }
    }
    
    bool IsGroupSorted(List<PasirDragItem> jarList)
    {
        if (jarList.Count == 0) return false;

        // Cek Ascending
        bool asc = true;
        for (int i = 0; i < jarList.Count; i++)
        {
            if (jarList[i].correctIndex != i)
            {
                asc = false;
                break;
            }
        }
        
        if (asc) return true;
        
        // Cek Descending
        bool desc = true;
        for (int i = 0; i < jarList.Count; i++)
        {
            if (jarList[i].correctIndex != (jarList.Count - 1 - i))
            {
                desc = false;
                break;
            }
        }
        
        return desc;
    }
    
    public void PlayClinkSound()
    {
        if (sfxAudioSource != null && clinkSound != null)
        {
            // Tambah sedikit variasi pitch supaya terdengar alami (opsional)
            sfxAudioSource.pitch = Random.Range(0.9f, 1.1f);
            sfxAudioSource.PlayOneShot(clinkSound);
        }
    }

    public void PlayDropSound()
    {
        if (sfxAudioSource != null && clinkSound != null)
        {
            sfxAudioSource.pitch = 1.0f;
            sfxAudioSource.PlayOneShot(clinkSound);
        }
    }
}

public class PasirDragItem : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    public PasirScript manager;
    public int correctIndex;
    public Transform originalGroup;
    
    public Vector3 targetPosition;
    public float lockedY;
    
    private bool isDragging = false;
    private RectTransform rectTransform;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    void Update()
    {
        if (!isDragging)
        {
            // Animasi halus bergerak ke tempat tujuannya
            Vector3 lerpTarget = new Vector3(targetPosition.x, lockedY, targetPosition.z);
            transform.localPosition = Vector3.Lerp(transform.localPosition, lerpTarget, Time.deltaTime * 15f);
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        isDragging = true;
        
        // Saat diclick, pindahkan visual paling depan
        transform.SetAsLastSibling();
    }
    
    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging) return;

        // Cari rect parent grup
        RectTransform groupRt = originalGroup as RectTransform;
        Vector2 localPointerPos;
        
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(groupRt, eventData.position, eventData.pressEventCamera, out localPointerPos))
        {
            // 1. Kunci geseran HANYA ke sumbu X secara horizontal.
            // 2. Batasi agar toples tidak bisa ditarik tembus keluar batas wajar grup.
            float minX = -groupRt.rect.width / 2f + (rectTransform.rect.width * rectTransform.localScale.x / 2f);
            float maxX = groupRt.rect.width / 2f - (rectTransform.rect.width * rectTransform.localScale.x / 2f);
            
            // Terapkan batas dengan nilai grup rect
            localPointerPos.x = Mathf.Clamp(localPointerPos.x, minX, maxX);
            transform.localPosition = new Vector3(localPointerPos.x, lockedY, transform.localPosition.z);
        }
        
        // Beritahu manager untuk menghitung pergeseran toples lain (Smooth Swapping)
        manager.OnJarDragged(this, originalGroup);
    }
    
    public void OnPointerUp(PointerEventData eventData)
    {
        isDragging = false;
        
        // Kembalikan hirarki visual sesuai urutannya yang benar biar gampang ter-sortir
        List<PasirDragItem> currentList = (originalGroup == manager.atasGroup) ? manager.atasJars : manager.bawahJars;
        int correctHierarchyIndex = currentList.IndexOf(this);
        transform.SetSiblingIndex(correctHierarchyIndex);

        manager.PlayDropSound(); 
        manager.CheckWin();
    }
}
