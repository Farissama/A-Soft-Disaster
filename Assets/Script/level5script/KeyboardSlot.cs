using UnityEngine;
using UnityEngine.EventSystems;

public class KeyboardSlot : MonoBehaviour
{
    [Header("Slot Identity")]
    [Tooltip("ID unik untuk slot ini (contoh: 'A', 'Space', 'Enter'). Harus sama dengan KeyID pada keycap yang benar.")]
    public string slotID; 
    
    [Tooltip("Ukuran yang diterima (contoh: 'Std', 'Space', 'Enter'). Keycap hanya bisa menempel jika ukurannya sama.")]
    public string acceptedSize = "Std"; 

    [Header("State")]
    public KeyboardKey occupiedKey;

    /// <summary>
    /// Mengecek apakah keycap ini boleh menempel di slot ini berdasarkan ukuran.
    /// </summary>
    public bool CanAccept(KeyboardKey key)
    {
        if (key == null) return false;
        // Cek apakah key memiliki ukuran yang sama dengan slot ini
        // Menggunakan Equals untuk string comparison yang aman
        return string.Equals(key.sizeTag, acceptedSize, System.StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Mengecek apakah slot ini sudah terisi oleh keycap yang BENAR (sesuai ID).
    /// </summary>
    public bool IsCorrect()
    {
        if (occupiedKey == null) return false;
        return string.Equals(occupiedKey.keyID, slotID, System.StringComparison.OrdinalIgnoreCase);
    }
}
