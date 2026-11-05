using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace SM64
{
    /// <summary>
    /// Handles screen fading (e.g. fade to black on player death/respawn).
    /// Automatically constructs a full-screen overlay Canvas with a black image if none is present in the scene.
    /// </summary>
    [DisallowMultipleComponent]
    public class ScreenFader : MonoBehaviour
    {
        public static ScreenFader Instance { get; private set; }

        [Header("Fade UI References")]
        [Tooltip("CanvasGroup controlling fade opacity. Auto-created if empty.")]
        [SerializeField] private CanvasGroup fadeCanvasGroup;

        [Tooltip("The black overlay Image. Auto-created if empty.")]
        [SerializeField] private Image fadeImage;

        private Coroutine _currentFadeRoutine;

        public float CurrentAlpha => fadeCanvasGroup != null ? fadeCanvasGroup.alpha : 0f;
        public bool IsFading => _currentFadeRoutine != null;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            EnsureOverlayExists();
        }

        /// <summary>
        /// Auto-generates a dedicated screen-space overlay canvas and full-screen black image if not set up in Inspector.
        /// </summary>
        public void EnsureOverlayExists()
        {
            if (fadeCanvasGroup != null && fadeImage != null)
                return;

            // Search in children
            fadeCanvasGroup = GetComponentInChildren<CanvasGroup>();
            if (fadeCanvasGroup != null)
            {
                fadeImage = fadeCanvasGroup.GetComponentInChildren<Image>();
                return;
            }

            // Create Canvas
            GameObject canvasGo = new GameObject("ScreenFadeCanvas");
            canvasGo.transform.SetParent(transform, false);

            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 9999; // Topmost

            canvasGo.AddComponent<CanvasScaler>();

            fadeCanvasGroup = canvasGo.AddComponent<CanvasGroup>();
            fadeCanvasGroup.alpha = 0f;
            fadeCanvasGroup.blocksRaycasts = false;
            fadeCanvasGroup.interactable = false;

            // Create full-screen black Image
            GameObject imageGo = new GameObject("FadeImage");
            imageGo.transform.SetParent(canvasGo.transform, false);

            RectTransform rt = imageGo.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            fadeImage = imageGo.AddComponent<Image>();
            fadeImage.color = Color.black;
            fadeImage.raycastTarget = false;
        }

        public Coroutine FadeOut(float duration, Action onComplete = null)
        {
            if (_currentFadeRoutine != null)
                StopCoroutine(_currentFadeRoutine);

            _currentFadeRoutine = StartCoroutine(FadeRoutine(CurrentAlpha, 1f, duration, onComplete));
            return _currentFadeRoutine;
        }

        public Coroutine FadeIn(float duration, Action onComplete = null)
        {
            if (_currentFadeRoutine != null)
                StopCoroutine(_currentFadeRoutine);

            _currentFadeRoutine = StartCoroutine(FadeRoutine(CurrentAlpha, 0f, duration, onComplete));
            return _currentFadeRoutine;
        }

        public IEnumerator FadeOutRoutine(float duration)
        {
            yield return FadeRoutine(CurrentAlpha, 1f, duration, null);
        }

        public IEnumerator FadeInRoutine(float duration)
        {
            yield return FadeRoutine(CurrentAlpha, 0f, duration, null);
        }

        private IEnumerator FadeRoutine(float fromAlpha, float toAlpha, float duration, Action onComplete)
        {
            EnsureOverlayExists();

            if (fadeCanvasGroup == null)
            {
                onComplete?.Invoke();
                yield break;
            }

            fadeCanvasGroup.blocksRaycasts = toAlpha > 0.05f;

            if (duration <= 0f)
            {
                fadeCanvasGroup.alpha = toAlpha;
                fadeCanvasGroup.blocksRaycasts = toAlpha > 0.5f;
                _currentFadeRoutine = null;
                onComplete?.Invoke();
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                fadeCanvasGroup.alpha = Mathf.Lerp(fromAlpha, toAlpha, t);
                yield return null;
            }

            fadeCanvasGroup.alpha = toAlpha;
            fadeCanvasGroup.blocksRaycasts = toAlpha > 0.5f;
            _currentFadeRoutine = null;
            onComplete?.Invoke();
        }
    }
}

