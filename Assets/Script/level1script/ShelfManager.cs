using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShelfManager : MonoBehaviour
{
    public List<BookItem> books = new List<BookItem>();
    public float gap = 0f;
    public GameObject winUI;

    // 🔊 SFX
    private AudioSource audioSource;
    public AudioClip bookSlideSFX;
    private float sfxCooldown = 0.1f;
    private float lastSFXTime;

    private IEnumerator Start()
    {
        if (GetComponent<UnityEngine.UI.LayoutGroup>() != null)
        {
            Debug.LogError("ShelfManager: CRITICAL! Found a LayoutGroup.");
        }

        yield return null;

        InitializeShelf();

        if (books.Count > 0)
        {
            ShuffleBooks();
        }
        else
        {
            Debug.LogError("ShelfManager: No books found!");
        }
        
        audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    void InitializeShelf()
    {
        if (books.Count == 0)
        {
            foreach (Transform child in transform)
            {
                BookItem b = child.GetComponent<BookItem>();
                if (b != null)
                {
                    books.Add(b);
                }
            }
        }
    }

    void ShuffleBooks()
    {
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
    }

    // ✅ INI YANG DIPERBAIKI (BUKAN DITAMBAH BARU)
    public void OnBookDragged(BookItem draggedBook)
    {
        float currentX = draggedBook.transform.localPosition.x;
        int newIndex = CalculateIndexFromX(currentX);

        int oldIndex = books.IndexOf(draggedBook);
        
        if (newIndex != oldIndex && newIndex >= 0 && newIndex < books.Count)
        {
            books.RemoveAt(oldIndex);
            books.Insert(newIndex, draggedBook);
            UpdateBookPositions();

            // 🔊 PLAY SFX (ANTI SPAM)
            if (Time.time - lastSFXTime > sfxCooldown)
            {
                audioSource.pitch = Random.Range(0.9f, 1.1f);
                audioSource.PlayOneShot(bookSlideSFX);
                lastSFXTime = Time.time;
            }
        }
    }

    public void OnBookDropped(BookItem droppedBook)
    {
        UpdateBookPositions();
        CheckWinCondition();
    }

    void UpdateBookPositions()
    {
        float totalWidth = 0f;
        foreach (var book in books)
        {
            RectTransform rt = book.GetComponent<RectTransform>();
            totalWidth += rt.rect.width * rt.localScale.x;
        }

        totalWidth += (books.Count - 1) * gap;

        float currentX = -totalWidth / 2f;

        for (int i = 0; i < books.Count; i++)
        {
            RectTransform rt = books[i].GetComponent<RectTransform>();
            float bookWidth = rt.rect.width * rt.localScale.x;

            float posX = currentX + (bookWidth / 2f);
            float currentY = books[i].transform.localPosition.y;

            Vector3 target = new Vector3(posX, currentY, 0);
            books[i].SetTargetPosition(target);

            currentX += bookWidth + gap;
        }
    }

    public void SortHierarchy()
    {
        for (int i = 0; i < books.Count; i++)
        {
            books[i].transform.SetSiblingIndex(i);
        }
    }

    int CalculateIndexFromX(float xPos)
    {
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

            float center = currentScanX + (bookWidth / 2f);

            if (xPos < center)
            {
                return i;
            }

            currentScanX += bookWidth + gap;
        }

        return books.Count - 1;
    }

    void CheckWinCondition()
    {
        bool isSorted = true;

        for (int i = 0; i < books.Count - 1; i++)
        {
            if (books[i].height < books[i + 1].height)
            {
                isSorted = false;
            }
        }

        if (isSorted)
        {
            if (winUI != null) winUI.SetActive(true);
        }
    }
}