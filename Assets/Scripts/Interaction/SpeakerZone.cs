using System.Collections;
using UnityEngine;

public class SpeakerZone : MonoBehaviour
{
    private SpeakerZoneFeedback feedback;

    [Header("Progress")]
    [SerializeField] private SpriteRenderer progress;
    [SerializeField] private SpriteRenderer delayedProgress;
    [SerializeField, Min(0f)] private float delayedStartTime = 0.15f;
    [SerializeField, Min(0f)] private float decreaseDuration = 0.4f;

    private ProgressVisual progressVisual;
    private ProgressVisual delayedProgressVisual;
    private Coroutine delayedProgressRoutine;
    private int maxProgressCount = 1;
    private int currentProgressCount;
    private float delayedFill = 1f;
    private bool progressVisualsCached;
    private bool progressInitialized;
    private bool countEventRegistered;

    private struct ProgressVisual
    {
        public Transform Transform;
        public Vector3 FullScale;
        public Vector3 FullPosition;
        public float FullWidth;
    }

    public Transform speakerZoneTransform;
    public Collider2D speakerZoneCollider;
    public LayerMask speakerZoneLayerMask;

    private void OnEnable()
    {
        if (speakerZoneTransform == null)
        {
            speakerZoneTransform = transform;
        }

        if (speakerZoneCollider == null)
        {
            speakerZoneCollider = GetComponent<Collider2D>();
        }

        feedback = GetComponent<SpeakerZoneFeedback>();

        if (progress == null && delayedProgress == null)
        {
            return;
        }

        if (progress == null || delayedProgress == null)
        {
            Debug.LogWarning(
                "SpeakerZone progress requires both Progress SpriteRenderers.",
                this);
            return;
        }

        CacheProgressVisuals();
        EventManager.Instance.EventRegister<int>(
            GameEvents.CountChanged,
            SetProgressCount);
        countEventRegistered = true;
    }

    private void OnDisable()
    {
        if (countEventRegistered)
        {
            EventManager.Instance.EventUnregister<int>(
                GameEvents.CountChanged,
                SetProgressCount);
            countEventRegistered = false;
        }

        StopDelayedProgress();
    }

    public void PlayAcceptedDropFeedback()
    {
        feedback?.Play();
    }

    public void InitializeProgress(int maxCount, int currentCount)
    {
        if (progress == null || delayedProgress == null)
        {
            return;
        }

        CacheProgressVisuals();
        StopDelayedProgress();

        maxProgressCount = Mathf.Max(1, maxCount);
        currentProgressCount = Mathf.Clamp(currentCount, 0, maxProgressCount);
        float fill = GetFill(currentProgressCount);

        ApplyFill(progressVisual, fill);
        ApplyFill(delayedProgressVisual, fill);
        delayedFill = fill;
        progressInitialized = true;
    }

    private void SetProgressCount(int currentCount)
    {
        if (!progressInitialized)
        {
            return;
        }

        int clampedCount = Mathf.Clamp(currentCount, 0, maxProgressCount);
        bool isDecrease = clampedCount < currentProgressCount;
        currentProgressCount = clampedCount;
        float targetFill = GetFill(currentProgressCount);

        ApplyFill(progressVisual, targetFill);
        StopDelayedProgress();

        if (!isDecrease)
        {
            delayedFill = targetFill;
            ApplyFill(delayedProgressVisual, targetFill);
            return;
        }

        delayedProgressRoutine = StartCoroutine(
            AnimateDelayedProgress(targetFill));
    }

    private IEnumerator AnimateDelayedProgress(float targetFill)
    {
        float elapsed = 0f;
        while (elapsed < delayedStartTime)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        if (decreaseDuration <= 0f)
        {
            delayedFill = targetFill;
            ApplyFill(delayedProgressVisual, delayedFill);
            delayedProgressRoutine = null;
            yield break;
        }

        float startFill = delayedFill;
        elapsed = 0f;
        while (elapsed < decreaseDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / decreaseDuration);
            delayedFill = Mathf.Lerp(
                startFill,
                targetFill,
                Mathf.SmoothStep(0f, 1f, t));
            ApplyFill(delayedProgressVisual, delayedFill);
            yield return null;
        }

        delayedFill = targetFill;
        ApplyFill(delayedProgressVisual, delayedFill);
        delayedProgressRoutine = null;
    }

    private void CacheProgressVisuals()
    {
        if (progressVisualsCached || progress == null || delayedProgress == null)
        {
            return;
        }

        progressVisual = CreateProgressVisual(progress);
        delayedProgressVisual = CreateProgressVisual(delayedProgress);
        progressVisualsCached = true;
    }

    private static ProgressVisual CreateProgressVisual(SpriteRenderer renderer)
    {
        Transform target = renderer.transform;
        float spriteWidth = renderer.sprite != null
            ? renderer.sprite.bounds.size.x
            : 0f;

        return new ProgressVisual
        {
            Transform = target,
            FullScale = target.localScale,
            FullPosition = target.localPosition,
            FullWidth = spriteWidth * Mathf.Abs(target.localScale.x)
        };
    }

    private static void ApplyFill(ProgressVisual visual, float fill)
    {
        if (visual.Transform == null)
        {
            return;
        }

        float clampedFill = Mathf.Clamp01(fill);
        Vector3 scale = visual.FullScale;
        scale.x = visual.FullScale.x * clampedFill;

        Vector3 position = visual.FullPosition;
        position.x -= visual.FullWidth * (1f - clampedFill) * 0.5f;

        visual.Transform.localScale = scale;
        visual.Transform.localPosition = position;
    }

    private float GetFill(int count)
    {
        return Mathf.Clamp01((float)count / maxProgressCount);
    }

    private void StopDelayedProgress()
    {
        if (delayedProgressRoutine == null)
        {
            return;
        }

        StopCoroutine(delayedProgressRoutine);
        delayedProgressRoutine = null;
    }
}
