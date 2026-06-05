using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class MobilScript : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public static List<MobilScript> allCars = new List<MobilScript>();

    [Header("Magnet Settings")]
    public float magnetEnterDistance = 100f; 
    public float magnetExitDistance = 180f;  
    public float smoothSpeed = 15f;          
    
    [Header("Bouncing / Collision Settings")]
    [Tooltip("Karena kita pakai sistem Capsule Slide, jarak radius kapsul sangat efektif di angka ini.")]
    public float capsuleCollisionRadius = 160f;
    public float slideSpeed = 10f;

    [Header("SFX Klakson")]
    public AudioClip hornSFX;                

    // 🔊 SFX PARKING (DITAMBAHKAN)
    [Header("SFX Parking")]
    public AudioClip parkingSFX;
    [SerializeField] private float parkingDelay = 2f;
    
    // Internal state
    private RectTransform rectTransform;
    private Canvas canvas;
    private AudioSource audioSource;
    private static GameObject winPage;       
    private static float lastHornTime;       

    private Vector2 virtualAnchoredPos;      
    private bool isDragging = false;
    public bool isSnapped = false;
    private bool isMagnetized = false;
    
    private RectTransform stickyTarget;
    private List<RectTransform> validTargets = new List<RectTransform>();
    private Vector3 originalScale;

    private bool positionInitialized = false;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
        
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) 
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }
        
        allCars.Add(this);
        originalScale = transform.localScale;
    }

    private void Start()
    {
        if (winPage == null)
        {
            winPage = GameObject.Find("winPage") ?? GameObject.Find("WinPage") ?? GameObject.Find("winpage");
            if (winPage == null)
            {
                Transform wp = transform.root.Find("WinPage") ?? transform.root.Find("winPage");
                if (wp != null) winPage = wp.gameObject;
            }
            if (winPage != null) winPage.SetActive(false); 
        }

        GameObject snapArea = GameObject.Find("snaparea");
        if (snapArea != null)
        {
            string myName = gameObject.name.Replace("(Clone)", "").Trim();

            foreach (Transform child in snapArea.transform)
            {
                string targetName = child.name.Replace("(Clone)", "").Trim();
                if (targetName == myName)
                {
                    validTargets.Add(child.GetComponent<RectTransform>());
                }
            }
        }

        RectTransform safeAreaRect = transform.parent as RectTransform;
        if (safeAreaRect != null && !isSnapped)
        {
            float spawnWidth = (safeAreaRect.rect.width / 2f) - 90f;
            float spawnHeight = (safeAreaRect.rect.height / 2f) - 150f;
            
            for (int i = 0; i < 50; i++)
            {
                float randX = Random.Range(-spawnWidth, spawnWidth);
                float randY = Random.Range(-spawnHeight, spawnHeight);
                float randRot = Random.Range(0f, 360f);
                
                rectTransform.anchoredPosition = new Vector2(randX, randY);
                virtualAnchoredPos = rectTransform.anchoredPosition;
                rectTransform.localEulerAngles = new Vector3(0, 0, randRot);
                
                bool overlap = false;
                foreach (var other in allCars)
                {
                    if (other == this || !other.positionInitialized) continue;
                    
                    Vector3[] myCaps = GetCapsulePointsLocal();
                    Vector3[] otherCaps = other.GetCapsulePointsLocal();

                    for (int a = 0; a < 2; a++)
                    {
                        for (int b = 0; b < 2; b++)
                        {
                            if (Vector3.Distance(myCaps[a], otherCaps[b]) < capsuleCollisionRadius)
                            {
                                overlap = true; break;
                            }
                        }
                        if (overlap) break;
                    }
                    if (overlap) break;
                }

                if (!overlap || i == 49) 
                {
                    break;
                }
            }
        }
        positionInitialized = true;
    }

    private void OnDestroy()
    {
        allCars.Remove(this);
    }

    private void Update()
    {
        HandlePositioning();
        HandleCollisions();
    }

    private void HandlePositioning()
    {
        if (isDragging)
        {
            Vector3 targetWorldPos;
            Quaternion targetRot = Quaternion.Euler(0, 0, 0);

            if (isMagnetized && stickyTarget != null)
            {
                targetWorldPos = stickyTarget.position;
                targetRot = stickyTarget.rotation;
            }
            else 
            {
                targetWorldPos = GetWorldPosFromAnchored(virtualAnchoredPos);
            }

            rectTransform.position = Vector3.Lerp(rectTransform.position, targetWorldPos, smoothSpeed * Time.deltaTime);
            rectTransform.rotation = Quaternion.Slerp(rectTransform.rotation, targetRot, smoothSpeed * Time.deltaTime);
        }
        else if (isSnapped && stickyTarget != null)
        {
            rectTransform.position = Vector3.Lerp(rectTransform.position, stickyTarget.position, smoothSpeed * Time.deltaTime);
            rectTransform.rotation = Quaternion.Slerp(rectTransform.rotation, stickyTarget.rotation, smoothSpeed * Time.deltaTime);
        }
    }

    private Vector3[] GetCapsulePointsLocal()
    {
        Vector3 myPos = rectTransform.localPosition;
        float angleRad = rectTransform.localEulerAngles.z * Mathf.Deg2Rad;
        
        float offsetX = -Mathf.Sin(angleRad) * 85f;
        float offsetY = Mathf.Cos(angleRad) * 85f;
        
        Vector3 offset = new Vector3(offsetX, offsetY, 0);
        
        return new Vector3[] { myPos + offset, myPos - offset };
    }

    private void HandleCollisions()
    {
        if (isDragging) return;

        foreach (MobilScript other in allCars)
        {
            if (other == this) continue;
            if (other.isDragging) continue;

            Vector3[] myCaps = GetCapsulePointsLocal();
            Vector3[] otherCaps = other.GetCapsulePointsLocal();

            for (int i = 0; i < 2; i++)
            {
                for (int j = 0; j < 2; j++)
                {
                    float dist = Vector3.Distance(myCaps[i], otherCaps[j]);
                    if (dist < capsuleCollisionRadius)
                    {
                        Vector3 dir = (myCaps[i] - otherCaps[j]).normalized;
                        if (dir == Vector3.zero) dir = Random.insideUnitCircle.normalized;

                        float overlap = capsuleCollisionRadius - dist;

                        if (!isSnapped && !other.isSnapped)
                        {
                            rectTransform.localPosition += dir * (overlap * slideSpeed * Time.deltaTime);
                        }
                        else if (!isSnapped && other.isSnapped)
                        {
                            rectTransform.localPosition += dir * (overlap * slideSpeed * 2f * Time.deltaTime);
                        }
                    }
                }
            }
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        isDragging = true;
        isSnapped = false;
        isMagnetized = false;
        stickyTarget = null;
        
        virtualAnchoredPos = rectTransform.anchoredPosition;
        transform.SetAsLastSibling();
        transform.localScale = originalScale * 1.05f; 
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (canvas == null) return;

        virtualAnchoredPos += eventData.delta / canvas.scaleFactor;
        UpdateMagnetState();
    }

    private void UpdateMagnetState()
    {
        Vector3 mouseWorldPos = GetWorldPosFromAnchored(virtualAnchoredPos);
        
        if (!isMagnetized)
        {
            RectTransform bestTarget = null;
            float minD = magnetEnterDistance;

            foreach (var target in validTargets)
            {
                if (IsTargetOccupiedByOther(target)) continue;

                float d = Vector3.Distance(mouseWorldPos, target.position);
                if (d < minD)
                {
                    minD = d;
                    bestTarget = target;
                }
            }

            if (bestTarget != null)
            {
                isMagnetized = true;
                stickyTarget = bestTarget;
            }
        }
        else
        {
            if (stickyTarget != null)
            {
                float dist = Vector3.Distance(mouseWorldPos, stickyTarget.position);
                if (dist > magnetExitDistance)
                {
                    isMagnetized = false;
                    stickyTarget = null;
                }
            }
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        isDragging = false;
        transform.localScale = originalScale;

        if (isMagnetized && stickyTarget != null)
        {
            isSnapped = true;

            // 🔊 PARKING SFX DELAY (DITAMBAHKAN)
            StartCoroutine(PlayParkingSFXDelayed());

            CheckWinCondition();
        }
        else
        {
            isSnapped = false;
            stickyTarget = null;
            
            if (hornSFX != null && Time.time - lastHornTime > 1.0f)
            {
                bool droppedOver = false;
                foreach (var other in allCars)
                {
                    if (other == this) continue;
                    
                    Vector3[] myCaps = GetCapsulePointsLocal();
                    Vector3[] otherCaps = other.GetCapsulePointsLocal();

                    for (int i = 0; i < 2; i++)
                    {
                        for (int j = 0; j < 2; j++)
                        {
                            if (Vector3.Distance(myCaps[i], otherCaps[j]) < capsuleCollisionRadius)
                            {
                                droppedOver = true; break;
                            }
                        }
                        if (droppedOver) break;
                    }
                    if (droppedOver) break;
                }

                if (droppedOver)
                {
                    audioSource.PlayOneShot(hornSFX);
                    lastHornTime = Time.time;
                }
            }
        }
        isMagnetized = false;
    }

    // 🔊 PARKING SFX FUNCTION (DITAMBAHKAN)
    private IEnumerator PlayParkingSFXDelayed()
    {
        yield return new WaitForSeconds(parkingDelay);

        if (parkingSFX != null)
        {
            GameObject tempGO = new GameObject("TempParkingSFX");
            tempGO.transform.position = Camera.main.transform.position;

            AudioSource aSource = tempGO.AddComponent<AudioSource>();
            aSource.clip = parkingSFX;
            aSource.pitch = Random.Range(0.98f, 1.02f);
            aSource.Play();

            Destroy(tempGO, parkingSFX.length);
        }
    }

    private bool IsTargetOccupiedByOther(RectTransform target)
    {
        foreach (MobilScript other in allCars)
        {
            if (other != this && other.isSnapped && other.stickyTarget == target)
            {
                return true;
            }
        }
        return false;
    }

    private Vector3 GetWorldPosFromAnchored(Vector2 anchored)
    {
        if (rectTransform.parent is RectTransform parentRT)
        {
            Vector3 localPos = new Vector3(anchored.x, anchored.y, 0f);
            return parentRT.TransformPoint(localPos);
        }
        return transform.position; 
    }

    private void CheckWinCondition()
    {
        int snappedCount = 0;
        foreach (MobilScript car in allCars)
        {
            if (car.isSnapped) snappedCount++;
        }

        if (snappedCount == allCars.Count)
        {
            Debug.Log("Level 7 Selesai! Semua mobil telah diparkir.");
            if (winPage != null)
            {
                winPage.SetActive(true);
            }
            else
            {
                StartCoroutine(ShowWinPageCoroutine());
            }

            foreach(MobilScript car in allCars)
            {
                car.enabled = false;
            }
        }
    }

    private IEnumerator ShowWinPageCoroutine()
    {
        yield return new WaitForSeconds(0.3f);
        Transform[] allTransforms = Resources.FindObjectsOfTypeAll<Transform>();
        foreach(Transform t in allTransforms)
        {
            if(t.name.ToLower().Contains("winpage") && t.parent != null && t.parent.name.ToLower().Contains("canvas"))
            {
                t.gameObject.SetActive(true);
                break;
            }
        }
    }
}