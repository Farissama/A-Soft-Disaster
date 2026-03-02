using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AtkSnapZone : MonoBehaviour
{
    public enum ZoneType
    {
        Loose,  // Kotak wadah besar (menggunakan deteksi area persegi)
        Magnet  // Titik spesifik (menggunakan deteksi jarak dekat/radius)
    }

    [Header("Settings")]
    [Tooltip("Loose: Wadah area (tidak narik magnet). Magnet: Titik presisi (narik barang).")]
    public ZoneType zoneType = ZoneType.Loose;
    [Tooltip("Berapa banyak barang yang bisa ditampung zona ini.")]
    public int capacity = 10; // Default capacity tinggi untuk Loose box
    [Tooltip("Tag barang yang boleh masuk. Contoh: 'Pencil', 'BinderClip'. Kosongkan jika semua boleh.")]
    public List<string> acceptedTags; 
    
    [Header("Magnet Settings (Only for Magnet Type)")]
    public List<Transform> snapPoints; 

    [Header("State")]
    public List<AtkDraggable> currentItems = new List<AtkDraggable>();
    private Dictionary<AtkDraggable, Transform> itemToPointMap = new Dictionary<AtkDraggable, Transform>();
    
    // Components
    private RectTransform rectTransform;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    public bool CanSnap(AtkDraggable item)
    {
        // 1. Cek Kapasitas
        if (currentItems.Count >= capacity)
            return false;

        // 2. Cek Tag (PENTING: Pastikan Tag di Draggable match dengan Tag di sini)
        if (acceptedTags != null && acceptedTags.Count > 0)
        {
            if (!acceptedTags.Contains(item.itemTag))
                return false;
        }
        
        // 3. Magnet Check Slot
        if (zoneType == ZoneType.Magnet && snapPoints != null && snapPoints.Count > 0)
        {
             if (GetBestPointForItem(item) == null)
                 return false;
        }

        return true;
    }
    
    // Helper untuk cek apakah titik ada di dalam kotak ini (Untuk Loose Zone)
    public bool IsInsideZone(Vector3 worldPoint)
    {
        if (rectTransform == null) rectTransform = GetComponent<RectTransform>();
        
        // Menggunakan ScreenPoint logic atau LocalPoint logic
        // Paling mudah convert world point ke local point rect ini
        Vector3 localPos = rectTransform.InverseTransformPoint(worldPoint);
        return rectTransform.rect.Contains(localPos);
    }

    public void SnapItem(AtkDraggable item)
    {
        if (!currentItems.Contains(item))
        {
            currentItems.Add(item);
            
            // Magnet Logic: Assign Point
            if (zoneType == ZoneType.Magnet && snapPoints != null && snapPoints.Count > 0)
            {
                Transform bestPoint = GetBestPointForItem(item);
                if (bestPoint != null)
                {
                    itemToPointMap[item] = bestPoint;
                }
            }
        }
    }

    public void ReleaseItem(AtkDraggable item)
    {
        if (currentItems.Contains(item))
        {
            currentItems.Remove(item);
            if (itemToPointMap.ContainsKey(item))
            {
                itemToPointMap.Remove(item);
            }
        }
    }
    
    public Vector3 GetSnapPositionFor(AtkDraggable item)
    {
        if (zoneType == ZoneType.Magnet)
        {
            if (itemToPointMap.ContainsKey(item))
            {
                return itemToPointMap[item].position;
            }
            Transform bestPoint = GetBestPointForItem(item);
            if (bestPoint != null) return bestPoint.position;
            return transform.position;
        }
        // Loose: Keep Position
        return item.transform.position; 
    }

    private Transform GetBestPointForItem(AtkDraggable item)
    {
        if (snapPoints == null || snapPoints.Count == 0) return null;

        // 1. Prioritas: Cari slot yang namanya mengandung itemTag-nya (misal: "snaptargetpencil1" mengandung "pencil1")
        foreach (Transform point in snapPoints)
        {
            if (point == null || IsPointOccupied(point)) continue;

            // Cek apakah nama point mengandung tag atau nama item
            if (point.name.ToLower().Contains(item.itemTag.ToLower()) || point.name.ToLower().Contains(item.name.ToLower()))
            {
                return point;
            }
        }

        // 2. Fallback: Cari slot terdekat yang kosong (logic lama)
        Transform nearestPoint = null;
        float bestDist = float.MaxValue;
        foreach (Transform point in snapPoints)
        {
            if (point == null || IsPointOccupied(point)) continue;
            float dist = Vector3.Distance(item.transform.position, point.position);
            if (dist < bestDist)
            {
                bestDist = dist;
                nearestPoint = point;
            }
        }
        return nearestPoint;
    }

    private bool IsPointOccupied(Transform point)
    {
        foreach (var occupiedPoint in itemToPointMap.Values)
        {
            if (occupiedPoint == point) return true;
        }
        return false;
    }
}
