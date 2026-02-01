using UnityEngine;
using UnityEngine.EventSystems;

public class BookItem : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler, IPointerEnterHandler
{
    public float height; // Set this in the Inspector for each book
    private ShelfManager shelfManager;
    private Vector3 targetPosition;
    private bool isDragging = false;
    private float lockedY; // To keep books on the ground level
    private RectTransform rectTransform;

    void Start()
    {
        shelfManager = GetComponentInParent<ShelfManager>();
        rectTransform = GetComponent<RectTransform>();
        
        if (shelfManager == null)
        {
            Debug.LogError("BookItem: No ShelfManager found in parents of " + gameObject.name);
        }
        
        // Use localPosition.y as the "ground" level
        lockedY = transform.localPosition.y;
        targetPosition = transform.localPosition;

        // FIX: Enable Alpha Hit Test so transparent parts don't block other books
        var img = GetComponent<UnityEngine.UI.Image>();
        if (img != null)
        {
            img.alphaHitTestMinimumThreshold = 0.1f; // Requires Read/Write Enabled on Sprite
        }
    }

    void Update()
    {
        if (!isDragging)
        {
            // Smoothly move to target position, keeping Y locked
            Vector3 lerpTarget = new Vector3(targetPosition.x, lockedY, targetPosition.z);
            transform.localPosition = Vector3.Lerp(transform.localPosition, lerpTarget, Time.deltaTime * 10f);
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        Debug.Log("BookItem: Selected " + gameObject.name);
        isDragging = true;
        
        // Bring to front visually
        transform.SetAsLastSibling();
        
        if (shelfManager != null) shelfManager.OnBookSelected(this);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging) return;

        // Follow mouse horizontal position only (locked to local rack space)
        Vector2 localMousePos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            transform.parent as RectTransform, 
            eventData.position, 
            eventData.pressEventCamera, 
            out localMousePos
        );

        // Clamp to Rack bounds
        RectTransform parentRect = transform.parent as RectTransform;
        if (parentRect != null)
        {
            float halfRackWidth = parentRect.rect.width / 2f;
            float halfBookWidth = rectTransform.rect.width * rectTransform.localScale.x / 2f;
            
            // Limit X so book stays fully inside
            float minX = -halfRackWidth + halfBookWidth;
            float maxX = halfRackWidth - halfBookWidth;
            
            localMousePos.x = Mathf.Clamp(localMousePos.x, minX, maxX);
        }

        transform.localPosition = new Vector3(localMousePos.x, lockedY, transform.localPosition.z);
        
        if (shelfManager != null) shelfManager.OnBookDragged(this);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        Debug.Log("BookItem: Dropped " + gameObject.name);
        isDragging = false;
        
        if (shelfManager != null)
        {
            shelfManager.OnBookDropped(this);
            // Removed SortHierarchy to prevent large items from permanently blocking others
            // shelfManager.SortHierarchy(); 
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        // DEBUG: Helps identify if this object is blocking the mouse even when invisible
        // Debug.Log("BookItem: Mouse Over " + gameObject.name);
    }

    public void SetTargetPosition(Vector3 newTarget)
    {
        targetPosition = newTarget;
    }

    private Vector3 GetMouseWorldPos()
    {
        Vector3 mousePoint = Input.mousePosition;
        mousePoint.z = Camera.main.WorldToScreenPoint(transform.position).z;
        return Camera.main.ScreenToWorldPoint(mousePoint);
    }
}
