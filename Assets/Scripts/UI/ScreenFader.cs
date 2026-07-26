using System.Collections;
using UnityEngine;

/// <summary>
/// 全屏黑色渐变遮罩，用于场景/剧情切换时的渐黑、渐显过渡。
/// 根物体挂 CanvasGroup + 一张铺满全屏的黑色 Image。
/// </summary>
public class ScreenFader : BasePanel
{
    private CanvasGroup canvasGroup;

    public bool IsOpaque => canvasGroup != null && canvasGroup.alpha >= 0.999f;

    protected override void Awake()
    {
        base.Awake();
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            Debug.LogError("ScreenFader requires a CanvasGroup component.");
            return;
        }

        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
    }

    /// <summary>渐黑：不透明度从当前值渐变到 1（完全黑屏，挡住输入）</summary>
    public IEnumerator FadeOut(float duration)
    {
        if (canvasGroup == null) yield break;

        canvasGroup.blocksRaycasts = true;
        if (IsOpaque || duration <= 0f)
        {
            canvasGroup.alpha = 1f;
            yield break;
        }

        float start = canvasGroup.alpha;
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(start, 1f, t / duration);
            yield return null;
        }
        canvasGroup.alpha = 1f;
    }

    /// <summary>渐显：不透明度从当前值渐变到 0（完全透明，不挡输入）</summary>
    public IEnumerator FadeIn(float duration)
    {
        if (canvasGroup == null) yield break;

        if (canvasGroup.alpha <= 0.001f || duration <= 0f)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            yield break;
        }

        float start = canvasGroup.alpha;
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(start, 0f, t / duration);
            yield return null;
        }
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
    }
}
