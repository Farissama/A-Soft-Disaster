using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CoklatManager : MonoBehaviour
{
    public static CoklatManager Instance;

    [Header("UI References")]
    public Transform safeareaParent; 
    public Transform snapareaParent; 
    public RectTransform tutupSnapTarget; 
    public GameObject winPage;

    [Header("Tutup Settings")]
    public CoklatScript tutupItem; 
    
    [Header("Randomize Settings")]
    public RectTransform spawnBoundary; 

    // 🔊 SFX (TANPA AUDIOSOURCE)
    [Header("SFX")]
    public AudioClip dropSFX;
    public AudioClip snapSFX;

    private bool isInitialized = false;

    private List<CoklatScript> listCoklat = new List<CoklatScript>();
    private bool isAnimatingTutup = false;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (winPage != null)
            winPage.SetActive(false);

        if (tutupItem != null)
        {
            tutupItem.isTutup = true;
            tutupItem.SetLocked(true);
            tutupItem.snapTarget = tutupSnapTarget;
        }

        SetupChocolates();

        isInitialized = true;
    }

    private void SetupChocolates()
    {
        if (safeareaParent == null || snapareaParent == null) return;

        for (int i = 0; i < safeareaParent.childCount; i++)
        {
            Transform itemTransform = safeareaParent.GetChild(i);
            CoklatScript itemScript = itemTransform.GetComponent<CoklatScript>();
            
            if (itemScript != null)
            {
                listCoklat.Add(itemScript);

                Transform targetTransform = snapareaParent.Find(itemTransform.name);
                if (targetTransform != null)
                {
                    itemScript.snapTarget = targetTransform.GetComponent<RectTransform>();
                }
                else
                {
                    Debug.LogWarning("Target snap untuk " + itemTransform.name + " tidak ditemukan!");
                }

                RandomizeItemPos(itemTransform.GetComponent<RectTransform>());
            }
        }
    }

    private void RandomizeItemPos(RectTransform rect)
    {
        if (spawnBoundary == null) return;

        float randomX = Random.Range(-spawnBoundary.rect.width / 2f, spawnBoundary.rect.width / 2f);
        float randomY = Random.Range(-spawnBoundary.rect.height / 2f, spawnBoundary.rect.height / 2f);

        rect.anchoredPosition = new Vector2(randomX, randomY);
        
        CoklatScript script = rect.GetComponent<CoklatScript>();
        if (script != null)
        {
            script.SyncPosition();
        }
    }

    public void CheckChocolatesWinCondition()
    {
        if (isAnimatingTutup) return;

        foreach (CoklatScript coklat in listCoklat)
        {
            if (!coklat.isSnapped) return;
        }

        isAnimatingTutup = true;

        foreach (CoklatScript coklat in listCoklat)
        {
            coklat.SetLocked(true);
        }

        if (tutupItem != null && tutupSnapTarget != null)
        {
            StartCoroutine(TutupAnimationSequence());
        }
    }

    private IEnumerator TutupAnimationSequence()
    {
        Transform tTrans = tutupItem.transform;
        Vector3 startPos = tTrans.position;
        
        Vector3 leftPos = startPos - tTrans.right * (600f * tTrans.lossyScale.x);
        Vector3 finalPos = tutupSnapTarget.position;

        yield return StartCoroutine(MoveToPos(tTrans, leftPos, 1.0f));
        
        tTrans.SetAsLastSibling();
        
        yield return new WaitForSeconds(0.20f);

        yield return StartCoroutine(MoveToPos(tTrans, finalPos, 0.8f));

        tTrans.position = finalPos;
        tutupItem.isSnapped = true;

        CheckFinalWinCondition();
    }

    private IEnumerator MoveToPos(Transform targetTransform, Vector3 destination, float duration)
    {
        Vector3 initialPos = targetTransform.position;
        Quaternion initialRot = targetTransform.rotation;
        Quaternion destRot = tutupSnapTarget.rotation;

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            t = t * t * (3f - 2f * t); 
            
            targetTransform.position = Vector3.Lerp(initialPos, destination, t);
            targetTransform.rotation = Quaternion.Lerp(initialRot, destRot, t);

            yield return null;
        }

        targetTransform.position = destination;
        targetTransform.rotation = destRot;
    }

    public void CheckFinalWinCondition()
    {
        if (winPage != null)
        {
            winPage.SetActive(true);
        }
    }

    // 🔊 SFX TANPA AUDIOSOURCE
    public void PlayDropSFX()
    {
        if (!isInitialized || dropSFX == null) return;

        float pitch = Random.Range(0.95f, 1.05f);
        PlayClip(dropSFX, pitch);
    }

    public void PlaySnapSFX()
    {
        if (!isInitialized || snapSFX == null) return;

        float pitch = Random.Range(0.98f, 1.02f);
        PlayClip(snapSFX, pitch);
    }

    private void PlayClip(AudioClip clip, float pitch)
    {
        GameObject tempGO = new GameObject("TempAudio");
        tempGO.transform.position = Camera.main.transform.position;

        AudioSource aSource = tempGO.AddComponent<AudioSource>();
        aSource.clip = clip;
        aSource.pitch = pitch;
        aSource.Play();

        Destroy(tempGO, clip.length / pitch);
    }
}