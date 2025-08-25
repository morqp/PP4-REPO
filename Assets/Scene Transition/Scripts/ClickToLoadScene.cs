using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ClickToLoadScene : MonoBehaviour
{
    [Tooltip("Scene name exactly as in Build Settings")]
    public string targetScene = "SceneB";

    [Tooltip("Optional per-click override of Fade In Time (to black). Set < 0 to use manager setting.")]
    public float fadeInOverrideSeconds = -1f;

    [Tooltip("Optional whoosh/click SFX at fade start")]
    public AudioClip clickSfx;

    private void OnMouseDown()
    {
        if (TransitionManager.Instance == null)
        {
            Debug.LogError("ClickToLoadScene: TransitionManager not found in scene.");
            return;
        }

        float overrideTime;
        if (fadeInOverrideSeconds >= 0f)
        {
            overrideTime = fadeInOverrideSeconds;
        }
        else
        {
            overrideTime = -1f;
        }

        Debug.Log("[ClickToLoadScene] Click -> Load '" + targetScene + "' (override=" + overrideTime + ")");
        TransitionManager.Instance.LoadScene(targetScene, overrideTime, clickSfx);
    }
}
