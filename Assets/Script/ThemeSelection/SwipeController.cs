using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SwipeController : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] int maxPage;
    int currentPage ;
    Vector3 targetPos;
    [SerializeField] Vector3 pageStep;
    [SerializeField] RectTransform levelPageRect;

    [SerializeField] float tweenTime;
    [SerializeField] LeanTweenType tweenType;
    float dragThreshould;
    [SerializeField] Button nextButton;
    [SerializeField] Button prevButton;

    private void Awake()
    {
        currentPage = 1;
        targetPos = levelPageRect.localPosition;
        dragThreshould = Screen.width / 15;

        UpdateButtonVisibility(); // ✅ penting
    }
    
    public void Next()
    {
        if (currentPage < maxPage)
        {
            currentPage++;
            targetPos += pageStep;
            MovePage();
            UpdateButtonVisibility(); // ✅ tambah ini
        }
    }

    public void Previous()
    {
        if (currentPage > 1)
        {
            currentPage--;
            targetPos -= pageStep;
            MovePage();
            UpdateButtonVisibility(); // ✅ tambah ini
        }
    }

    void MovePage()
    {
        levelPageRect.LeanMoveLocal(targetPos, tweenTime).setEase(tweenType);
    }

     public void OnBeginDrag(PointerEventData eventData)
    {
        // bisa dikosongkan
    }

    // ✅ WAJIB ADA
    public void OnDrag(PointerEventData eventData)
    {
        // bisa dipakai buat real-time drag kalau mau
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if(Mathf.Abs(eventData.position.x-eventData.pressPosition.x)>dragThreshould)
        {
            if(eventData.position.x>eventData.pressPosition.x) Previous();
            else Next();
        }
        else
        {
            MovePage();
        }
    }

    void UpdateButtonVisibility()
    {
    // Kalau di page pertama → Prev mati
    if (currentPage <= 1)
        prevButton.gameObject.SetActive(false);
    else
        prevButton.gameObject.SetActive(true);

    // Kalau di page terakhir → Next mati
    if (currentPage >= maxPage)
        nextButton.gameObject.SetActive(false);
    else
        nextButton.gameObject.SetActive(true);
    }
}
