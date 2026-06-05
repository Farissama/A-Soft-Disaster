using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PaperPuzzle : MonoBehaviour
{
    [Header("Game Settings")]
    public RectTransform safeArea;
    public RectTransform snapTarget;
    [SerializeField] private float winDistanceThreshold = 50f;
    [SerializeField] private GameObject winUI;

    // 🔊 SFX (DITAMBAHKAN)
    [SerializeField] private AudioClip paperSlideSFX;
    private AudioSource audioSource;
    private float sfxCooldown = 0.1f;
    private float lastSFXTime;

    private List<PaperDraggable> papers = new List<PaperDraggable>();

    public PaperDraggable GetPaperByIndex(int index)
    {
        return papers.FirstOrDefault(p => p.orderIndex == index);
    }
    
    IEnumerator Start()
    {
        // 🔊 INIT AUDIO (DITAMBAHKAN)
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        UnityEngine.UI.LayoutGroup layout = GetComponent<UnityEngine.UI.LayoutGroup>();
        if (layout != null)
        {
            layout.enabled = false;
        }

        Canvas.ForceUpdateCanvases();
        yield return null;

        papers = GetComponentsInChildren<PaperDraggable>().ToList();

        ScramblePapers();
    }

    private void ScramblePapers()
    {
        if (safeArea == null)
        {
            Debug.LogError("[PaperPuzzle] Safe Area is NOT assigned in Inspector!");
            return;
        }

        Vector3[] saCorners = new Vector3[4];
        safeArea.GetWorldCorners(saCorners);
        Vector3 saMin = saCorners[0];
        Vector3 saMax = saCorners[2];

        Debug.Log($"[PaperPuzzle] Safe Area Bounds: Min({saMin.x:F2}, {saMin.y:F2}) Max({saMax.x:F2}, {saMax.y:F2})");

        foreach (var paper in papers)
        {
            RectTransform rt = paper.GetComponent<RectTransform>();
            
            Vector2 size = Vector2.Scale(rt.rect.size, rt.lossyScale);
            Vector2 extents = size * 0.5f;

            float minX = saMin.x + extents.x;
            float maxX = saMax.x - extents.x;
            float minY = saMin.y + extents.y;
            float maxY = saMax.y - extents.y;

            if (minX > maxX) minX = maxX = (saMin.x + saMax.x) * 0.5f;
            if (minY > maxY) minY = maxY = (saMin.y + saMax.y) * 0.5f;

            float randomX = Random.Range(minX, maxX);
            float randomY = Random.Range(minY, maxY);
            
            Debug.Log($"[PaperPuzzle] Moving {paper.name}: RandomPos({randomX:F2}, {randomY:F2})");

            paper.SetInitialPosition(new Vector3(randomX, randomY, rt.position.z));

            float randomRot = Random.Range(-25f, 25f);
            rt.localEulerAngles = new Vector3(0, 0, randomRot);
        }
    }

    public void CheckWinCondition()
    {
        if (papers.Count == 0) return;

        foreach (var paper in papers)
        {
            if (paper.GetComponent<RectTransform>().anchoredPosition.magnitude > winDistanceThreshold)
            {
                return;
            }
        }

        var currentStack = papers.OrderBy(p => p.transform.GetSiblingIndex()).ToList();

        for (int i = 0; i < currentStack.Count - 1; i++)
        {
            if (currentStack[i].orderIndex > currentStack[i + 1].orderIndex)
            {
                return;
            }
        }

        Debug.Log("Level 3 Cleared!");
        if (winUI != null) winUI.SetActive(true);
    }

    // 🔊 FUNCTION SFX DRAG (DITAMBAHKAN)
    public void PlayDragSFX()
    {
        if (audioSource != null && paperSlideSFX != null)
        {
            if (Time.time - lastSFXTime > sfxCooldown)
            {
                audioSource.pitch = Random.Range(0.95f, 1.05f);
                audioSource.PlayOneShot(paperSlideSFX);
                lastSFXTime = Time.time;
            }
        }
    }
}