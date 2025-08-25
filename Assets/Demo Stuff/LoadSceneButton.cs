using UnityEngine;

public class LoadSceneButton : MonoBehaviour
{
    [Tooltip("Scene name exactly as in Build Settings")]
    public string sceneName = "scene";

    [Tooltip("Optional: override fade-in seconds. Set < 0 to use manager default.")]
    public float fadeInOverrideSeconds = -1f;

    [Tooltip("Optional click SFX")]
    public AudioClip clickSfx;

    // Call this from a UI Button to load a scene
    public void Click()
    {
        Debug.LogWarning("starting Click method");

        // Ensure the persistent manager exists
        TransitionManager.Ensure();

        if (TransitionManager.Instance == null)
        {
            Debug.Log("[LoadSceneButton] No TransitionManager available.");
            return;
        }

        float overrideTime = (fadeInOverrideSeconds >= 0f) ? fadeInOverrideSeconds : -1f;
        TransitionManager.Instance.LoadScene(sceneName, overrideTime, clickSfx);
        Debug.Log("called .loadscene in transitionmanager");

        // Optional: disable the parent/root canvas immediately
        var canvas = GetComponentInParent<Canvas>(true);
        if (canvas != null)
        {
            var root = (canvas.rootCanvas != null) ? canvas.rootCanvas.gameObject : canvas.gameObject;
            root.SetActive(false);
            Debug.Log("disabled canvas");
        }

        Debug.LogWarning("finished Click method");
    }

    // Call this from a UI Button to exit the app
    public void QuitApp()
    {

        Application.Quit();
    }
}
