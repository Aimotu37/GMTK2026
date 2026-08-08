using UnityEngine;
using System.Collections;

public class Item : MonoBehaviour, IInteractive
{
    [Header("物品信息数据")]
    [SerializeField] private int itemID;
    public int ItemID => itemID;

    [Header("交互手感参数")]
    [SerializeField] private float dragScale = 1.1f;
    [SerializeField] private float snapDuration = 0.15f;

    [Header("Highlight Visual")]
    [SerializeField] private Sprite highlightSprite;

    // 实现接口属性
    public bool IsInteractable { get; private set; } = true;
    public bool IsDragging { get; set; } = false;

    // 内部组件缓存
    private Collider2D col2d;
    private SpriteRenderer spriteRenderer;
    private Sprite originalSprite;
    private Vector3 startScale;
    private Vector3 startPosition;
    private int defaultSortingOrder;
    private bool tutorialHighlighted;
    private bool dropAccepted;

    private void Awake()
    {
        col2d = GetComponent<Collider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        startScale = transform.localScale;
        if (spriteRenderer != null)
        {
            originalSprite = spriteRenderer.sprite;
            defaultSortingOrder = spriteRenderer.sortingOrder;
        }
    }

    public void OnDragStart(Vector3 mouseWorldPos)
    {
        if (!IsInteractable) return;

        IsDragging = true;
        startPosition = transform.position;
        RefreshSprite();

        transform.localScale = startScale * dragScale;
        if (spriteRenderer != null) spriteRenderer.sortingOrder = 100;

        StopAllCoroutines();
    }

    public void OnDragUpdate(Vector3 mouseWorldPos)
    {
        //TODO:让物品平滑跟随鼠标（直接在Controller里计算位置，这里只做位置更新）
        transform.position = mouseWorldPos;
    }

    public void OnDragEnd(bool isAccepted)
    {
        IsDragging = false;
        dropAccepted = isAccepted;
        RefreshSprite();
        if (isAccepted)
        {
            StartCoroutine(SnapAndDestroy(InteractionController.Instance.SpeakerZonePosition));
        }
        else
        {
            StartCoroutine(SnapBack(startPosition));
        }
    }

    public Collider2D GetCollider() => col2d;

    public void SetTutorialHighlighted(bool highlighted)
    {
        tutorialHighlighted = highlighted;
        RefreshSprite();
    }

    private IEnumerator SnapAndDestroy(Vector3 target)
    {
        col2d.enabled = false;
        yield return StartCoroutine(SmoothMoveTo(target, snapDuration));

        float elapsed = 0f;
        float duration = 0.2f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            transform.localScale = startScale * (1 - t);
            yield return null;
        }
        Destroy(gameObject);
    }

    private IEnumerator SnapBack(Vector3 target)
    {
        yield return StartCoroutine(SmoothMoveTo(target, snapDuration));
        transform.localScale = startScale;
        if (spriteRenderer != null) spriteRenderer.sortingOrder = defaultSortingOrder;
        col2d.enabled = true;
    }

    private IEnumerator SmoothMoveTo(Vector3 target, float duration)
    {
        Vector3 startPos = transform.position;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0, 1, elapsed / duration);
            transform.position = Vector3.Lerp(startPos, target, t);
            yield return null;
        }
        transform.position = target;
    }

    public void SetInteractable(bool value)
    {
        IsInteractable = value;
        col2d.enabled = value;
    }

    private void RefreshSprite()
    {
        if (spriteRenderer == null)
        {
            return;
        }

        bool shouldHighlight = tutorialHighlighted || IsDragging || dropAccepted;
        spriteRenderer.sprite = shouldHighlight && highlightSprite != null
            ? highlightSprite
            : originalSprite;
    }
}
