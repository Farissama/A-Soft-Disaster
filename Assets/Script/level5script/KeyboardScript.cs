using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KeyboardScript : MonoBehaviour
{
    [Header("Configuration")]
    [Tooltip("Area KESELURUHAN level (boundary luar). Keycaps akan disebar di dalam area ini.")]
    public RectTransform playArea; 

    [Tooltip("Area Keyboard (boundary dalam). Keycaps TIDAK AKAN muncul di area ini saat diacak.")]
    public RectTransform keyboardArea;
    
    [Tooltip("Jumlah keycaps yang akan diacak (keluar dari tempatnya). Sisanya akan tetap rapi di posisi benar.")]
    public int keysToScatter = 5;

    [Tooltip("GameObject UI yang akan muncul saat pemain menang.")]
    public GameObject winUI; 

    [Header("Debug Info")]
    public List<KeyboardKey> allKeys;
    public List<KeyboardSlot> allSlots;

    private bool isGameWon = false;

    // Start is called before the first frame update
    void Start()
    {
        // Cari semua komponen di scene
        allKeys = new List<KeyboardKey>(FindObjectsOfType<KeyboardKey>());
        allSlots = new List<KeyboardSlot>(FindObjectsOfType<KeyboardSlot>());

        // 1. TATA RAPI SEMUA (Auto-solve dulu untuk setup awal)
        SetupCorrectPositions();

        // 2. ACAK SEBAGIAN
        RandomizeKeys();
        
        // Sembunyikan UI menang di awal
        if (winUI != null) winUI.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        // Cheat key untuk debug reset (tekan R)
        if (Input.GetKeyDown(KeyCode.R))
        {
            SetupCorrectPositions();
            RandomizeKeys();
        }
    }

    void SetupCorrectPositions()
    {
        // Reset Win State
        isGameWon = false;
        if (winUI != null) winUI.SetActive(false);

        // Pasangkan setiap key ke slot yang benar
        foreach (var key in allKeys)
        {
            // Cari slot dengan ID yang sama
            KeyboardSlot correctSlot = allSlots.Find(s => s.slotID == key.keyID);
            
            if (correctSlot != null)
            {
                // Snap instant
                key.SnapTo(correctSlot);
                key.transform.position = correctSlot.transform.position;
                key.transform.rotation = Quaternion.identity;
                // Debug.Log($"Auto-snapped {key.keyID}");
            }
        }
    }

    public void RandomizeKeys()
    {
        if (playArea == null) 
        {
            Debug.LogWarning("Play Area belum di-assign di KeyboardScript!");
            return;
        }

        Rect outerRect = GetWorldRect(playArea);
        
        Rect exclusionRect = new Rect(0,0,0,0);
        bool useExclusion = false;
        if (keyboardArea != null)
        {
            exclusionRect = GetWorldRect(keyboardArea);
            useExclusion = true;
        }

        // --- PARTIAL RANDOMIZATION LOGIC ---
        
        // 1. Buat daftar acak dari semua keys
        List<KeyboardKey> shuffledKeys = new List<KeyboardKey>(allKeys);
        ShuffleList(shuffledKeys);

        // 2. Ambil sejumlah N keys untuk dihambur
        int countToMove = Mathf.Min(keysToScatter, shuffledKeys.Count);
        
        for (int i = 0; i < countToMove; i++)
        {
            KeyboardKey key = shuffledKeys[i];
            
            // Lepas dari slot
            key.SnapTo(null);

            // Cari posisi random valid
            Vector3 finalPos = key.transform.position;
            bool validPosFound = false;
            int attempts = 0;

            while (!validPosFound && attempts < 20)
            {
                float randX = Random.Range(outerRect.xMin, outerRect.xMax);
                float randY = Random.Range(outerRect.yMin, outerRect.yMax);
                Vector3 candidatePos = new Vector3(randX, randY, key.transform.position.z);

                if (useExclusion && exclusionRect.Contains(candidatePos))
                {
                    attempts++;
                }
                else
                {
                    finalPos = candidatePos;
                    validPosFound = true;
                }
            }
            
            key.transform.position = finalPos;
            key.transform.rotation = Quaternion.identity; 
        }
    }

    // Fisher-Yates Shuffle
    void ShuffleList<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            T temp = list[i];
            int randomIndex = Random.Range(i, list.Count);
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }
    
    Rect GetWorldRect(RectTransform rt)
    {
        Vector3[] corners = new Vector3[4];
        rt.GetWorldCorners(corners);
        Vector3 bottomLeft = corners[0];
        Vector3 topRight = corners[2];
        return new Rect(bottomLeft.x, bottomLeft.y, topRight.x - bottomLeft.x, topRight.y - bottomLeft.y);
    }

    public void CheckWin()
    {
        int correctCount = 0;
        foreach (var slot in allSlots)
        {
            if (slot.IsCorrect())
            {
                correctCount++;
            }
        }

        if (correctCount == allSlots.Count && allSlots.Count > 0)
        {
            Debug.Log("LEVEL 5 COMPLETED!");
            isGameWon = true;
            if (winUI != null) winUI.SetActive(true);
        }
    }

    private void OnDrawGizmos()
    {
        if (playArea != null)
        {
            Gizmos.color = Color.green;
            Rect r = GetWorldRect(playArea);
            Gizmos.DrawWireCube(r.center, r.size);
        }

        if (keyboardArea != null)
        {
            Gizmos.color = Color.red;
            Rect r = GetWorldRect(keyboardArea);
            Gizmos.DrawWireCube(r.center, r.size);
        }
    }
}
