using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShelfManager : MonoBehaviour
{
    public List<BookItem> books = new List<BookItem>();
    public float gap = 0f; // Small gap between books if needed
    public GameObject winUI; // Assign your Win Panel here

    private IEnumerator Start()
    {
        // Check if Rack has a LayoutGroup which conflicts with this script
        if (GetComponent<UnityEngine.UI.LayoutGroup>() != null)
        {
            Debug.LogError("ShelfManager: CRITICAL! Found a LayoutGroup (Horizontal/Vertical) on the Rack. This will conflict with drag and drop! Please REMOVE the LayoutGroup component causing the conflict.");
        }

        // Wait a frame to ensure UI layout (RectTransform) is calculated
        yield return null; 

        InitializeShelf();
        if (books.Count > 0)
        {
            ShuffleBooks();
        }
        else
        {
            Debug.LogError("ShelfManager: No books found! Make sure books are children of Rack and have BookItem script.");
        }
    }

    void InitializeShelf()
    {
        if (books.Count == 0)
        {
            Debug.Log("ShelfManager: Searching for books in children...");
            foreach (Transform child in transform)
            {
                BookItem b = child.GetComponent<BookItem>();
                if (b != null)
                {
                    books.Add(b);
                    Debug.Log("ShelfManager: Found book " + child.name);
                }
            }
        }
    }

    void ShuffleBooks()
    {
        // Simple Fisher-Yates shuffle
        for (int i = 0; i < books.Count; i++)
        {
            BookItem temp = books[i];
            int randomIndex = Random.Range(i, books.Count);
            books[i] = books[randomIndex];
            books[randomIndex] = temp;
        }
        UpdateBookPositions();
    }

    public void OnBookSelected(BookItem draggedBook)
    {
        // Optional: darken other books or highlight dragged one
    }

    public void OnBookDragged(BookItem draggedBook)
    {
        // Find current horizontal index based on dragged book's position
        float currentX = draggedBook.transform.localPosition.x;
        int newIndex = CalculateIndexFromX(currentX);

        int oldIndex = books.IndexOf(draggedBook);
        
        if (newIndex != oldIndex && newIndex >= 0 && newIndex < books.Count)
        {
            // Swap in list and update targets
            books.RemoveAt(oldIndex);
            books.Insert(newIndex, draggedBook);
            UpdateBookPositions();
        }
    }

    public void OnBookDropped(BookItem droppedBook)
    {
        UpdateBookPositions();
        CheckWinCondition();
    }

    void UpdateBookPositions()
    {
        // Calculate total width of all books
        float totalWidth = 0f;
        foreach (var book in books)
        {
            RectTransform rt = book.GetComponent<RectTransform>();
            totalWidth += rt.rect.width * rt.localScale.x;
        }

        // Add gaps
        totalWidth += (books.Count - 1) * gap;

        // Start position (far left)
        float currentX = -totalWidth / 2f;

        for (int i = 0; i < books.Count; i++)
        {
            RectTransform rt = books[i].GetComponent<RectTransform>();
            float bookWidth = rt.rect.width * rt.localScale.x;
            
            // Position is center of the book, so add half width
            float posX = currentX + (bookWidth / 2f);
            
            // Maintain Y locked
            float currentY = books[i].transform.localPosition.y;
            Vector3 target = new Vector3(posX, currentY, 0);
            books[i].SetTargetPosition(target);

            // Move currentX to the end of this book + gap
            currentX += bookWidth + gap;
        }
    }

    public void SortHierarchy()
    {
        // Re-order hierarchy to match the list so they render in correct Z-order (left to right)
        for (int i = 0; i < books.Count; i++)
        {
            books[i].transform.SetSiblingIndex(i);
        }
    }

    int CalculateIndexFromX(float xPos)
    {
        // Find the index by checking which book slot is closest or within bounds
        // This is a bit trickier with variable widths, so we iterate and check distances
        // Simple approach: Check if xPos is to the left of the center of a book
        
        // Re-simulate the layout to find the split points
        float totalWidth = 0f;
        foreach (var book in books)
        {
            RectTransform rt = book.GetComponent<RectTransform>();
            totalWidth += rt.rect.width * rt.localScale.x;
        }
        totalWidth += (books.Count - 1) * gap;
        
        float currentScanX = -totalWidth / 2f;
        
        for (int i = 0; i < books.Count; i++)
        {
            RectTransform rt = books[i].GetComponent<RectTransform>();
            float bookWidth = rt.rect.width * rt.localScale.x;
            
            // The boundary for this slot is from currentScanX to currentScanX + bookWidth
            // We check if the mouse xPos is roughly within this book's left-half or previous book's right-half
            
            float center = currentScanX + (bookWidth / 2f);
            
            // If xPos is less than the center of this book, it belongs in this index (shifting this book right)
            if (xPos < center)
            {
                return i;
            }
            
            currentScanX += bookWidth + gap;
        }
        
        // If we fall through, it belongs at the end
        return books.Count - 1;
    }

    void CheckWinCondition()
    {
        bool isSorted = true;
        string debugOrder = "Order: ";
        
        for (int i = 0; i < books.Count - 1; i++)
        {
            debugOrder += books[i].height + " -> ";
            
            // USER REQUEST: Sort Descending (Left=Big, Right=Small)
            // So if checks[i] < checks[i+1], it's WRONG (Ascending)
            if (books[i].height < books[i+1].height)
            {
                isSorted = false;
            }
        }
        debugOrder += books[books.Count - 1].height;
        Debug.Log(debugOrder + " | Sorted (Desc): " + isSorted);

        if (isSorted)
        {
            Debug.Log("Puzzles Solved! Books are sorted by height.");
            if (winUI != null) winUI.SetActive(true);
        }
    }
}
