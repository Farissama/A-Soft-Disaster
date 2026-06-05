using UnityEngine;
using System.Collections.Generic;

public class Level9Manager : MonoBehaviour
{
    public static Level9Manager Instance;
    
    [Header("Makaroni Spawning")]
    public GameObject makaroniTemplate; 
    public RectTransform spawnArea; 
    public int totalMakaroniTarget = 20;
    
    [Header("Toples Settings")]
    public RectTransform toplesDropArea; 
    
    [Header("Batas Safe Area")]
    public RectTransform safeAreaRect; 
    
    [Header("Win Condition")]
    public GameObject winPage;
    public int makaroniInToplesCount = 0;
    public bool isTutupPlaced = false;

    // 🔊 SFX (UPDATED TANPA AUDIOSOURCE)
    [Header("Audio SFX")]
    public AudioClip dropSFX;
    public AudioClip toplesKacaSFX;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (winPage != null) winPage.SetActive(false);
        DuplicateMakaroni();
        CreateBoundaryFromSafeArea();
    }

    private void CreateBoundaryFromSafeArea()
    {
        if (safeAreaRect == null) return;

        float t = 100f;
        float xMin = safeAreaRect.rect.xMin;
        float xMax = safeAreaRect.rect.xMax;
        float yMin = safeAreaRect.rect.yMin;
        float yMax = safeAreaRect.rect.yMax;
        float xCenter = (xMax + xMin) / 2f;
        float yCenter = (yMax + yMin) / 2f;
        float w = xMax - xMin;
        float h = yMax - yMin;

        BoxCollider2D left = safeAreaRect.gameObject.AddComponent<BoxCollider2D>();
        left.size = new Vector2(t, h + t*2);
        left.offset = new Vector2(xMin - t/2f, yCenter);

        BoxCollider2D right = safeAreaRect.gameObject.AddComponent<BoxCollider2D>();
        right.size = new Vector2(t, h + t*2);
        right.offset = new Vector2(xMax + t/2f, yCenter);

        BoxCollider2D bottom = safeAreaRect.gameObject.AddComponent<BoxCollider2D>();
        bottom.size = new Vector2(w + t*2, t);
        bottom.offset = new Vector2(xCenter, yMin - t/2f);

        BoxCollider2D top = safeAreaRect.gameObject.AddComponent<BoxCollider2D>();
        top.size = new Vector2(w + t*2, t);
        top.offset = new Vector2(xCenter, yMax + t/2f);
    }

    private void DuplicateMakaroni()
    {
        if (makaroniTemplate == null || spawnArea == null) return;

        List<GameObject> allMakaronis = new List<GameObject> { makaroniTemplate };
        
        for (int i = 1; i < totalMakaroniTarget; i++)
        {
            GameObject clone = Instantiate(makaroniTemplate, makaroniTemplate.transform.parent);
            clone.name = makaroniTemplate.name + "_" + i;
            allMakaronis.Add(clone);
        }
        
        foreach (GameObject makaroni in allMakaronis)
        {
            RectTransform rt = makaroni.GetComponent<RectTransform>();
            float randomX = Random.Range(-spawnArea.rect.width / 2f, spawnArea.rect.width / 2f);
            float randomY = Random.Range(-spawnArea.rect.height / 2f, spawnArea.rect.height / 2f);
            
            rt.position = spawnArea.TransformPoint(new Vector3(randomX, randomY, 0));
            rt.localRotation = Quaternion.Euler(0, 0, Random.Range(0f, 360f));
        }
    }

    public bool IsInToplesArea(RectTransform makaroniRT)
    {
        if (toplesDropArea == null) return false;
        
        Canvas canvas = toplesDropArea.GetComponentInParent<Canvas>();
        Camera cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            toplesDropArea, 
            RectTransformUtility.WorldToScreenPoint(cam, makaroniRT.position), 
            cam, 
            out localPoint);
            
        return toplesDropArea.rect.Contains(localPoint);
    }
    
    public bool ClampToSafeArea(ref Vector3 targetWorldPos)
    {
        if (safeAreaRect == null) return false;
        
        Vector3[] corners = new Vector3[4];
        safeAreaRect.GetWorldCorners(corners);
        
        float minX = corners[0].x;
        float maxX = corners[2].x;
        float minY = corners[0].y;
        float maxY = corners[2].y;
        
        targetWorldPos.x = Mathf.Clamp(targetWorldPos.x, minX, maxX);
        targetWorldPos.y = Mathf.Clamp(targetWorldPos.y, minY, maxY);
        return true;
    }

    public void IncrementMakaroni()
    {
        makaroniInToplesCount++;

        // 🔊 SFX KENA KACA (MASUK TOPLES)
        PlayToplesKacaSFX();

        CheckWinCondition();
    }

    public void TutupPlaced()
    {
        isTutupPlaced = true;
        CheckWinCondition();
    }

    public void TutupRemoved()
    {
        isTutupPlaced = false;
    }

    private void CheckWinCondition()
    {
        if (makaroniInToplesCount >= totalMakaroniTarget && isTutupPlaced)
        {
            if (winPage != null && !winPage.activeSelf)
            {
                Debug.Log("Level 9 Menang!");
                winPage.SetActive(true);
            }
        }
    }

    // 🔊 DROP SFX
    public void PlayDropSFX()
    {
        if (dropSFX == null) return;
        PlayClip(dropSFX, Random.Range(0.95f, 1.05f));
    }

    // 🔊 KACA SFX
    public void PlayToplesKacaSFX()
    {
        if (toplesKacaSFX == null) return;
        PlayClip(toplesKacaSFX, Random.Range(0.98f, 1.02f));
    }

    // 🔊 CORE PLAYER TANPA AUDIOSOURCE
    private void PlayClip(AudioClip clip, float pitch)
    {
        GameObject tempGO = new GameObject("TempMakaroniSFX");
        tempGO.transform.position = Camera.main.transform.position;

        AudioSource aSource = tempGO.AddComponent<AudioSource>();
        aSource.clip = clip;
        aSource.pitch = pitch;
        aSource.Play();

        Destroy(tempGO, clip.length / pitch);
    }
}