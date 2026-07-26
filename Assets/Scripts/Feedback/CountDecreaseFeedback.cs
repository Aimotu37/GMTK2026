using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CountDecreaseFeedback : MonoBehaviour
{
    [Header("Scale")]
    [SerializeField] private float enlargedScale = 1.25f;
    [SerializeField] private float growDuration = 0.10f;
    [SerializeField] private float returnDuration = 0.10f;

    [Header("Impact Shake")]
    [SerializeField] private float shakeDuration = 0.25f;
    [SerializeField] private float shakeStrength = 10f;

    [Header("UI Burst")]
    [SerializeField, Min(1)] private int particleCount = 8;
    [SerializeField] private float particleLifetime = 0.24f;
    [SerializeField] private float particleDistance = 42f;
    [SerializeField] private float particleSize = 8f;
    [SerializeField] private Color particleColor = new Color(1f, 0.18f, 0.08f, 1f);

    private sealed class UiParticle
    {
        public RectTransform Rect;
        public Image Image;
        public Vector2 Direction;
    }

    private readonly List<UiParticle> _particles = new List<UiParticle>();
    private readonly System.Random _random = new System.Random();

    private TMP_Text _countText;
    private RectTransform _target;
    private RectTransform _particleContainer;
    private Vector2 _baselinePosition;
    private Vector3 _baselineScale = Vector3.one;
    private Coroutine _routine;

    public void Initialize(TMP_Text countText)
    {
        if (countText == null)
        {
            Debug.LogWarning("CountDecreaseFeedback requires a count text component.", this);
            return;
        }

        ResetFeedback();
        _countText = countText;
        _target = countText.rectTransform;
        _baselinePosition = _target.anchoredPosition;
        _baselineScale = _target.localScale;
        EnsureParticlePool();
    }

    public void SetImmediate(int value)
    {
        ResetFeedback();
        if (_countText != null)
        {
            _countText.text = value.ToString();
        }
    }

    public void PlayDecrease(int oldValue, int newValue)
    {
        if (_countText == null || _target == null)
        {
            return;
        }

        if (newValue >= oldValue)
        {
            SetImmediate(newValue);
            return;
        }

        ResetFeedback();
        _countText.text = oldValue.ToString();
        _routine = StartCoroutine(PlayDecreaseRoutine(newValue));
    }

    public void ResetFeedback()
    {
        if (_routine != null)
        {
            StopCoroutine(_routine);
            _routine = null;
        }

        ResetTransformOnly();
        HideParticles();
    }

    private IEnumerator PlayDecreaseRoutine(int newValue)
    {
        yield return AnimateScale(_baselineScale, _baselineScale * enlargedScale, growDuration);
        yield return AnimateScale(_target.localScale, _baselineScale, returnDuration);

        _countText.text = newValue.ToString();
        BeginParticleBurst();

        float elapsed = 0f;
        float impactDuration = Mathf.Max(shakeDuration, particleLifetime);
        while (elapsed < impactDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            UpdateShake(elapsed);
            UpdateParticles(elapsed);
            yield return null;
        }

        ResetTransformOnly();
        HideParticles();
        _routine = null;
    }

    private IEnumerator AnimateScale(Vector3 from, Vector3 to, float duration)
    {
        if (duration <= 0f)
        {
            _target.localScale = to;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            _target.localScale = Vector3.LerpUnclamped(from, to, t);
            yield return null;
        }

        _target.localScale = to;
    }

    private void UpdateShake(float elapsed)
    {
        if (_target == null || shakeDuration <= 0f || elapsed >= shakeDuration)
        {
            if (_target != null)
            {
                _target.anchoredPosition = _baselinePosition;
            }
            return;
        }

        float decay = 1f - Mathf.Clamp01(elapsed / shakeDuration);
        float x = (float)(_random.NextDouble() * 2.0 - 1.0);
        float y = (float)(_random.NextDouble() * 2.0 - 1.0);
        Vector2 offset = new Vector2(x, y).normalized * (shakeStrength * decay);
        _target.anchoredPosition = _baselinePosition + offset;
    }

    private void EnsureParticlePool()
    {
        if (_target == null || _particles.Count == particleCount)
        {
            return;
        }

        if (_particleContainer != null)
        {
            Destroy(_particleContainer.gameObject);
        }

        _particles.Clear();
        GameObject containerObject = new GameObject("CountImpactParticles", typeof(RectTransform));
        _particleContainer = containerObject.GetComponent<RectTransform>();
        _particleContainer.SetParent(_target, false);
        _particleContainer.anchorMin = new Vector2(0.5f, 0.5f);
        _particleContainer.anchorMax = new Vector2(0.5f, 0.5f);
        _particleContainer.pivot = new Vector2(0.5f, 0.5f);
        _particleContainer.anchoredPosition = Vector2.zero;
        _particleContainer.sizeDelta = Vector2.zero;
        _particleContainer.SetAsLastSibling();

        int count = Mathf.Max(1, particleCount);
        for (int i = 0; i < count; i++)
        {
            GameObject particleObject = new GameObject(
                $"Particle_{i}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            RectTransform rect = particleObject.GetComponent<RectTransform>();
            rect.SetParent(_particleContainer, false);
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = Vector2.one * particleSize;
            rect.localRotation = Quaternion.Euler(0f, 0f, 45f);

            Image image = particleObject.GetComponent<Image>();
            image.color = particleColor;
            image.raycastTarget = false;
            particleObject.SetActive(false);

            _particles.Add(new UiParticle { Rect = rect, Image = image });
        }
    }

    private void BeginParticleBurst()
    {
        EnsureParticlePool();
        int count = _particles.Count;
        for (int i = 0; i < count; i++)
        {
            float angle = (Mathf.PI * 2f * i / count) + (float)(_random.NextDouble() - 0.5) * 0.25f;
            UiParticle particle = _particles[i];
            particle.Direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            particle.Rect.anchoredPosition = Vector2.zero;
            particle.Rect.localScale = Vector3.one;
            particle.Image.color = particleColor;
            particle.Rect.gameObject.SetActive(true);
        }
    }

    private void UpdateParticles(float elapsed)
    {
        float t = particleLifetime <= 0f ? 1f : Mathf.Clamp01(elapsed / particleLifetime);
        float distanceT = 1f - (1f - t) * (1f - t);
        for (int i = 0; i < _particles.Count; i++)
        {
            UiParticle particle = _particles[i];
            if (!particle.Rect.gameObject.activeSelf)
            {
                continue;
            }

            particle.Rect.anchoredPosition = particle.Direction * (particleDistance * distanceT);
            particle.Rect.localScale = Vector3.one * Mathf.Lerp(1f, 0.35f, t);
            Color color = particleColor;
            color.a *= 1f - t;
            particle.Image.color = color;

            if (t >= 1f)
            {
                particle.Rect.gameObject.SetActive(false);
            }
        }
    }

    private void ResetTransformOnly()
    {
        if (_target == null)
        {
            return;
        }

        _target.anchoredPosition = _baselinePosition;
        _target.localScale = _baselineScale;
    }

    private void HideParticles()
    {
        for (int i = 0; i < _particles.Count; i++)
        {
            if (_particles[i].Rect != null)
            {
                _particles[i].Rect.gameObject.SetActive(false);
            }
        }
    }

    private void OnDisable()
    {
        ResetFeedback();
    }
}
