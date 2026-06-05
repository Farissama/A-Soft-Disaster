using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PancakeScript : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Parent of draggable items (blueberries and strawberries)")]
    public Transform itemsParent; 
    [Tooltip("Parent of snap targets")]
    public Transform snapTargetsParent; 
    [Tooltip("Winning Page Panel")]
    public GameObject winPage;

    [Header("Randomize Settings")]
    [Tooltip("The starting plate where items will be randomized (inrandom)")]
    public RectTransform spawnPlate; 
    [Tooltip("Radius within the plate to randomize")]
    public float spawnRadius = 150f; 

    // 🔊 SFX (UPDATED - TANPA AUDIOSOURCE)
    [Header("SFX Settings")]
    public AudioClip dropSFX;
    public AudioClip snapSFX;

    private List<PancakeItem> listItems = new List<PancakeItem>();
    private bool isWon = false;

    private void Start()
    {
        if (winPage != null)
            winPage.SetActive(false);

        SetupItems();
    }

    private void SetupItems()
    {
        if (itemsParent == null || snapTargetsParent == null) return;

        for (int i = 0; i < itemsParent.childCount; i++)
        {
            Transform itemTransform = itemsParent.GetChild(i);
            PancakeItem itemScript = itemTransform.GetComponent<PancakeItem>();
            
            if (itemScript != null)
            {
                listItems.Add(itemScript);

                Transform targetTransform = snapTargetsParent.Find(itemTransform.name);
                if (targetTransform != null)
                {
                    itemScript.snapTarget = targetTransform.GetComponent<RectTransform>();
                }
                else
                {
                    Debug.LogWarning("Target snap untuk " + itemTransform.name + " tidak ditemukan!");
                }

                RandomizeItemPosWithinCircle(itemTransform.GetComponent<RectTransform>());
            }
        }
    }

    private void RandomizeItemPosWithinCircle(RectTransform rect)
    {
        if (spawnPlate == null) return;

        Vector2 randomPointInsideCircle = Random.insideUnitCircle * spawnRadius;
        rect.position = spawnPlate.TransformPoint(randomPointInsideCircle);
        
        PancakeItem script = rect.GetComponent<PancakeItem>();
        if (script != null)
        {
            script.SyncPosition();
        }
    }

    // 🔊 DROP SFX
    public void PlayDropSFX()
    {
        if (dropSFX == null) return;

        PlayClip(dropSFX, Random.Range(0.95f, 1.05f));
    }

    // 🔊 SNAP SFX
    public void PlaySnapSFX()
    {
        if (snapSFX == null) return;

        PlayClip(snapSFX, Random.Range(0.98f, 1.02f));
    }

    // 🔊 CORE AUDIO PLAYER (TANPA AUDIOSOURCE DI SCENE)
    private void PlayClip(AudioClip clip, float pitch)
    {
        GameObject tempGO = new GameObject("TempPancakeSFX");
        tempGO.transform.position = Camera.main.transform.position;

        AudioSource aSource = tempGO.AddComponent<AudioSource>();
        aSource.clip = clip;
        aSource.pitch = pitch;
        aSource.Play();

        Destroy(tempGO, clip.length / pitch);
    }

    public void CheckWinCondition()
    {
        if (isWon) return;

        bool allCorrect = true;

        foreach (PancakeItem item in listItems)
        {
            if (!item.isSnapped)
            {
                allCorrect = false;
                break;
            }
        }

        if (allCorrect)
        {
            Debug.Log("Game Selesai! Menampilkan Win Page.");
            isWon = true;

            foreach (PancakeItem item in listItems)
            {
                item.SetLocked(true);
            }

            StartCoroutine(ShowWinPageDelay());
        }
    }

    private IEnumerator ShowWinPageDelay()
    {
        yield return new WaitForSeconds(0.5f);
        if (winPage != null)
        {
            winPage.SetActive(true);
        }
    }
}