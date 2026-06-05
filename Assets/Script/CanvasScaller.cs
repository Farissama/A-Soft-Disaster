using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasScaler))]
public class CanvasScalerRatioController : MonoBehaviour
{
    [Header("Reference Ratio (Width : Height)")]
    [SerializeField] private float targetAspect = 16f / 9f;

    [Header("Match Values")]
    [SerializeField] private float matchWidth = 0f;
    [SerializeField] private float matchHeight = 1f;

    private CanvasScaler canvasScaler;

    private void Awake()
    {
        canvasScaler = GetComponent<CanvasScaler>();
        ApplyMatch();
    }

#if UNITY_EDITOR
    private void Update()
    {
        ApplyMatch();
    }
#endif

    private void ApplyMatch()
    {
        float currentAspect = (float)Screen.width / Screen.height;

        if (currentAspect >= targetAspect)
        {
            canvasScaler.matchWidthOrHeight = matchHeight;
        }
        else
        {
            canvasScaler.matchWidthOrHeight = matchWidth;
        }
    }
}