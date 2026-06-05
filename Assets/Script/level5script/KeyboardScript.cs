using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class KeyboardScript : MonoBehaviour
{
    
    private bool isInitialized = false;
    
    [Header("Configuration")]
    public RectTransform playArea; 
    public RectTransform keyboardArea;
    public int keysToScatter = 5;
    public GameObject winUI; 

    [Header("Debug Info")]
    public List<KeyboardKey> allKeys;
    public List<KeyboardSlot> allSlots;

    // 🔊 SFX (DITAMBAHKAN)
    [SerializeField] private AudioClip dropSFX;
    [SerializeField] private AudioClip snapSFX;
    private AudioSource audioSource;

    private bool isGameWon = false;

    void Start()
    {
        // 🔊 INIT AUDIO (DITAMBAHKAN)
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        allKeys = new List<KeyboardKey>(FindObjectsOfType<KeyboardKey>());
        allSlots = new List<KeyboardSlot>(FindObjectsOfType<KeyboardSlot>());

        SetupCorrectPositions();
        RandomizeKeys();
        
        if (winUI != null) winUI.SetActive(false);

        isInitialized = true;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            SetupCorrectPositions();
            RandomizeKeys();
        }
    }

    void SetupCorrectPositions()
    {
        isGameWon = false;
        if (winUI != null) winUI.SetActive(false);

        foreach (var key in allKeys)
        {
            KeyboardSlot correctSlot = allSlots.Find(s => s.slotID == key.keyID);
            
            if (correctSlot != null)
            {
                key.SnapTo(correctSlot);
                key.transform.position = correctSlot.transform.position;
                key.transform.rotation = Quaternion.identity;
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

        List<KeyboardKey> shuffledKeys = new List<KeyboardKey>(allKeys);
        ShuffleList(shuffledKeys);

        int countToMove = Mathf.Min(keysToScatter, shuffledKeys.Count);
        
        for (int i = 0; i < countToMove; i++)
        {
            KeyboardKey key = shuffledKeys[i];
            key.SnapTo(null);

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

    // 🔊 SFX DROP (DITAMBAHKAN)
    public void PlayDropSFX()
    {
        if (!isInitialized) return;

        if (audioSource != null && dropSFX != null)
        {
            audioSource.pitch = Random.Range(0.95f, 1.05f);
            audioSource.PlayOneShot(dropSFX);
        }
    }

    // 🔊 SFX SNAP (DITAMBAHKAN)
    public void PlaySnapSFX()
    {
        if (!isInitialized) return;

        if (audioSource != null && snapSFX != null)
        {
            audioSource.pitch = Random.Range(0.98f, 1.02f);
            audioSource.PlayOneShot(snapSFX);
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