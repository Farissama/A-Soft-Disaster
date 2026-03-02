using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class LampFlickerUI : MonoBehaviour
{
    Image img;

    [SerializeField] float minAlpha = 0.6f;
    [SerializeField] float maxAlpha = 1f;

    [SerializeField] float flickerChance = 0.3f;
    [SerializeField] float flickerSpeed = 0.05f;

    void Start()
    {
        img = GetComponent<Image>();
        StartCoroutine(FlickerRoutine());
    }

    IEnumerator FlickerRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(1.5f, 4f));

            if (Random.value < flickerChance)
            {
                int flickCount = Random.Range(1, 4);

                for (int i = 0; i < flickCount; i++)
                {
                    SetAlpha(minAlpha);
                    yield return new WaitForSeconds(flickerSpeed);

                    SetAlpha(maxAlpha);
                    yield return new WaitForSeconds(flickerSpeed);
                }
            }
        }
    }

    void SetAlpha(float a)
    {
        Color c = img.color;
        c.a = a;
        img.color = c;
    }
}
