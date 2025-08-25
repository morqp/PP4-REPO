using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TransitionManager : MonoBehaviour
{
    public static TransitionManager Instance { get; private set; }

    [Header("Assign the Transition Canvas prefab (root has CanvasGroup + FadeController)")]
    public GameObject transitionCanvasPrefab;

    [Header("Optional: assign manually, otherwise it will be auto-found by name 'ProgressFill'")]
    public Image progressFill;

    [Header("Audio (optional)")]
    public AudioSource audioSource;

    [Header("Fade Durations (seconds)")]
    [Min(0f)] public float fadeInTime = 0.6f;   // to black, before load
    [Min(0f)] public float fadeOutTime = 0.6f;  // from black, after load

    [Header("Minimum black-screen hold (seconds)")]
    [Min(0f)] public float minBlackHold = 0.0f;

    [Header("Progress Bar Smoothing")]
    [Tooltip("If enabled, the bar fills smoothly over at least 'minFillSeconds', even if loading is instant.")]
    public bool smoothProgress = true;
    [Min(0f)] public float minFillSeconds = 1.0f;

    private FadeController fade;
    private bool busy;
    private float displayedProgress = 0f;

    // Ensure a manager exists (safe to call from buttons)
    public static TransitionManager Ensure()
    {
        if (Instance != null) return Instance;
        var go = new GameObject("TransitionManager");
        var tm = go.AddComponent<TransitionManager>();
        return tm;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[TransitionManager] Duplicate found, destroying this one.");
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

        // Try to auto-load prefab from Resources if not assigned
        if (transitionCanvasPrefab == null)
        {
            transitionCanvasPrefab = Resources.Load<GameObject>("TransitionCanvas");
            if (transitionCanvasPrefab == null)
                Debug.LogWarning("[TransitionManager] transitionCanvasPrefab not assigned and not found at Resources/TransitionCanvas.");
        }

        // Instantiate the fade/progress UI
        if (transitionCanvasPrefab != null)
        {
            var ui = Instantiate(transitionCanvasPrefab);
            DontDestroyOnLoad(ui);

            // Ensure FadeController on the UI root
            fade = ui.GetComponent<FadeController>();
            if (fade == null) fade = ui.AddComponent<FadeController>();

            // Auto-find "ProgressFill" if not assigned
            if (progressFill == null)
            {
                var t = ui.transform.Find("ProgressFill");
                if (t != null) progressFill = t.GetComponent<Image>();
            }

            // Configure progress bar if present
            if (progressFill != null)
            {
                if (progressFill.type != Image.Type.Filled) progressFill.type = Image.Type.Filled;
                progressFill.fillMethod = Image.FillMethod.Horizontal;
                progressFill.fillOrigin = 0;
                progressFill.fillAmount = 0f;
                progressFill.raycastTarget = false;
                progressFill.transform.SetAsLastSibling();
            }
        }
        else
        {
            Debug.LogWarning("[TransitionManager] transitionCanvasPrefab is not assigned. Fades will be skipped.");
        }
    }

    /// <summary>
    /// Triggers a scene change with fade.
    /// Set fadeTimeOverride to a non-negative value to override; pass -1f to use fadeInTime.
    /// </summary>
    public void LoadScene(string sceneName, float fadeTimeOverride = -1f, AudioClip sfx = null)
    {
        if (busy)
        {
            Debug.LogWarning("[TransitionManager] Ignored: already loading a scene.");
            return;
        }

        StartCoroutine(LoadSceneRoutine(sceneName, fadeTimeOverride, sfx));
    }

    // Optional: bind directly if you have the manager in the first scene
    public void LoadSceneWithFade(string sceneName)
    {
        LoadScene(sceneName, fadeInTime, null);
    }

    private IEnumerator LoadSceneRoutine(string sceneName, float fadeTimeOverride, AudioClip sfx)
    {
        busy = true;

        if (sfx != null && audioSource != null)
            audioSource.PlayOneShot(sfx);

        float toBlack = (fadeTimeOverride >= 0f) ? fadeTimeOverride : fadeInTime;
        float fromBlack = fadeOutTime;

        // Reset progress at start of transition
        displayedProgress = 0f;
        if (progressFill != null)
        {
            progressFill.enabled = true;
            progressFill.fillAmount = 0f;
        }

        // 1) Fade to black
        if (fade != null)
        {
            yield return fade.FadeTo(1f, toBlack);
        }

        // 2) Load while black
        float blackStart = Time.unscaledTime;

        AsyncOperation op = null;
        try
        {
            op = SceneManager.LoadSceneAsync(sceneName);
        }
        catch (System.Exception e)
        {
            Debug.LogError("[TransitionManager] LoadSceneAsync threw: " + e);
            busy = false;
            yield break;
        }

        if (op == null)
        {
            Debug.LogError("[TransitionManager] LoadSceneAsync returned null. Scene name wrong or not in Build Settings?");
            busy = false;
            yield break;
        }

        op.allowSceneActivation = false;

        while (op.progress < 0.9f)
        {
            float target = Mathf.Clamp01(op.progress / 0.9f);
            if (progressFill != null)
            {
                if (smoothProgress && minFillSeconds > 0f)
                {
                    displayedProgress = Mathf.MoveTowards(
                        displayedProgress,
                        target,
                        Time.unscaledDeltaTime / minFillSeconds
                    );
                    progressFill.fillAmount = displayedProgress;
                }
                else
                {
                    progressFill.fillAmount = target;
                }
            }
            yield return null;
        }

        if (progressFill != null)
        {
            if (smoothProgress && minFillSeconds > 0f)
            {
                while (displayedProgress < 1f - 0.0001f)
                {
                    displayedProgress = Mathf.MoveTowards(
                        displayedProgress,
                        1f,
                        Time.unscaledDeltaTime / minFillSeconds
                    );
                    progressFill.fillAmount = displayedProgress;
                    yield return null;
                }
            }
            progressFill.fillAmount = 1f;
        }

        float elapsedBlack = Time.unscaledTime - blackStart;
        if (minBlackHold > 0f && elapsedBlack < minBlackHold)
            yield return new WaitForSecondsRealtime(minBlackHold - elapsedBlack);

        // 3) Activate scene
        op.allowSceneActivation = true;
        yield return null;

        // 4) Fade from black
        if (fade != null)
        {
            yield return fade.FadeTo(0f, fromBlack);
        }

        // Reset bar when done
        if (progressFill != null)
        {
            progressFill.fillAmount = 0f;
            displayedProgress = 0f;
        }

        busy = false;
    }
}
