using System.Collections;
using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public class FadeController : MonoBehaviour
{
    private CanvasGroup group;

    private void Awake()
    {
        group = GetComponent<CanvasGroup>();
        if (group == null)
        {
            group = gameObject.AddComponent<CanvasGroup>();
        }
    }

    /// <summary>
    /// Fades the CanvasGroup alpha to target over duration (seconds).
    /// Blocks raycasts while alpha > 0 so clicks don’t pass through a black screen.
    /// </summary>
    public IEnumerator FadeTo(float target, float duration)
    {
        float start = group.alpha;
        float t = 0f;

        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / Mathf.Max(0.0001f, duration);
            group.alpha = Mathf.Lerp(start, target, t);
            yield return null;
        }

        group.alpha = target;
        group.blocksRaycasts = target > 0.001f;
    }
}
