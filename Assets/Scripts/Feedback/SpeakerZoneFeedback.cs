using System.Collections;
using UnityEngine;

public class SpeakerZoneFeedback : MonoBehaviour
{
    private const string RuntimeVisualName = "SpeakerVisualFeedbackTarget";

    [SerializeField] private Transform visualTarget;
    [SerializeField] private float duration = 0.16f;
    [SerializeField] private float strength = 0.08f;

    private readonly System.Random _random = new System.Random();
    private Vector3 _baselineLocalPosition;
    private Coroutine _routine;
    private bool _isPrepared;
    private bool _missingVisualWarningLogged;

    private void Awake()
    {
        PrepareVisual();
    }

    public Transform PrepareVisual()
    {
        if (_isPrepared && visualTarget != null)
        {
            return visualTarget;
        }

        if (visualTarget == null)
        {
            visualTarget = transform.Find(RuntimeVisualName);
        }

        if (visualTarget == null)
        {
            visualTarget = CreateVisualProxy();
        }

        if (visualTarget == null)
        {
            if (!_missingVisualWarningLogged)
            {
                Debug.LogWarning("SpeakerZoneFeedback could not find a SpriteRenderer to shake.", this);
                _missingVisualWarningLogged = true;
            }
            return null;
        }

        _baselineLocalPosition = visualTarget.localPosition;
        _isPrepared = true;
        return visualTarget;
    }

    public void Play()
    {
        if (!isActiveAndEnabled || PrepareVisual() == null)
        {
            return;
        }

        ResetFeedback();
        _routine = StartCoroutine(ShakeRoutine());
    }

    public void ResetFeedback()
    {
        if (_routine != null)
        {
            StopCoroutine(_routine);
            _routine = null;
        }

        if (_isPrepared && visualTarget != null)
        {
            visualTarget.localPosition = _baselineLocalPosition;
        }
    }

    private Transform CreateVisualProxy()
    {
        SpriteRenderer sourceRenderer = GetComponent<SpriteRenderer>();
        if (sourceRenderer == null)
        {
            return null;
        }

        GameObject visualObject = new GameObject(RuntimeVisualName, typeof(SpriteRenderer));
        visualObject.layer = gameObject.layer;
        Transform target = visualObject.transform;
        target.SetParent(transform, false);
        target.localPosition = Vector3.zero;
        target.localRotation = Quaternion.identity;
        target.localScale = Vector3.one;

        SpriteRenderer proxyRenderer = visualObject.GetComponent<SpriteRenderer>();
        proxyRenderer.sprite = sourceRenderer.sprite;
        proxyRenderer.sharedMaterial = sourceRenderer.sharedMaterial;
        proxyRenderer.color = sourceRenderer.color;
        proxyRenderer.flipX = sourceRenderer.flipX;
        proxyRenderer.flipY = sourceRenderer.flipY;
        proxyRenderer.drawMode = sourceRenderer.drawMode;
        proxyRenderer.size = sourceRenderer.size;
        proxyRenderer.tileMode = sourceRenderer.tileMode;
        proxyRenderer.maskInteraction = sourceRenderer.maskInteraction;
        proxyRenderer.sortingLayerID = sourceRenderer.sortingLayerID;
        proxyRenderer.sortingOrder = sourceRenderer.sortingOrder;
        proxyRenderer.spriteSortPoint = sourceRenderer.spriteSortPoint;
        proxyRenderer.enabled = sourceRenderer.enabled;

        sourceRenderer.enabled = false;
        return target;
    }

    private IEnumerator ShakeRoutine()
    {
        if (duration <= 0f)
        {
            ResetFeedback();
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float decay = 1f - Mathf.Clamp01(elapsed / duration);
            float x = (float)(_random.NextDouble() * 2.0 - 1.0);
            float y = (float)(_random.NextDouble() * 2.0 - 1.0);
            Vector2 offset = new Vector2(x, y).normalized * (strength * decay);
            visualTarget.localPosition = _baselineLocalPosition + new Vector3(offset.x, offset.y, 0f);
            yield return null;
        }

        visualTarget.localPosition = _baselineLocalPosition;
        _routine = null;
    }

    private void OnDisable()
    {
        ResetFeedback();
    }
}
