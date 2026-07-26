using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InteractionController : SingletonMono<InteractionController>
{
    private SpeakerZone speakerZone;

    [SerializeField] private Transform speakerZoneTransform;   // 发声槽的位置（用于磁吸）
    [SerializeField] private Collider2D speakerZoneCollider;   // 发声槽的碰撞体（用于检测）
    [SerializeField] private LayerMask speakerZoneLayerMask;   // 或者用 Layer 检测
    [Header("鼠标拾取")]
    [SerializeField] private LayerMask itemRaycastLayerMask; // 只让指定层级的物品参与点击检测
    [Header("拖拽限制")]
    [SerializeField] private Collider2D dragBoundsCollider;    // 指定拖拽范围的碰撞体
    public Vector3 SpeakerZonePosition => speakerZoneTransform.position;

    public TypewriterEffect typewriter;

    [Header("调试信息")]
    [SerializeField] private Item currentItem;
    public Item CurrentItem => currentItem;

    public UnityAction<string, Item> OnItemDroppedToZone;

    private Vector3 offset;
    private bool isDragging = false;

    private void OnEnable()
    {
        itemRaycastLayerMask = LayerMask.GetMask("Interactive");
    }

    public void Update()
    {
        // 已经开始的拖拽优先处理，避免 UI 拦截导致拖拽更新或松手事件丢失
        if (isDragging && currentItem != null)
        {
            Vector3 mousePos = Camera.main.ScreenToWorldPoint(InputManager.Instance.GetMousePosition());
            mousePos.z = 0f;

            if (!IsPointerInsideDragBounds(mousePos))
            {
                RejectCurrentDrop();
                return;
            }

            if (InputManager.Instance.GetMouse(0))
            {
                OnMouseDragOnObject(mousePos);
            }

            if (InputManager.Instance.GetMouseUp(0))
            {
                OnMouseUpOnObject();
            }

            return;
        }

        // 未处于拖拽状态时，UI 只阻止开始新的游戏对象交互
        EventSystem eventSystem = EventSystem.current;
        if (eventSystem != null && eventSystem.IsPointerOverGameObject())
        {
            if (InputManager.Instance.GetMouseDown(0) && !IsPointerOverButton(eventSystem))
            {
                UIEmptyClick();
            }
            return;  // ← UI 上的点击，直接结束，不处理游戏对象
        }

        if (InputManager.Instance.GetMouseDown(0))
        {
            OnMouseDownOnObject();
        }
    }

    private bool IsPointerOverButton(EventSystem eventSystem)
    {
        PointerEventData pointerData = new PointerEventData(eventSystem)
        {
            position = InputManager.Instance.GetMousePosition()
        };
        List<RaycastResult> raycastResults = new List<RaycastResult>();
        eventSystem.RaycastAll(pointerData, raycastResults);

        foreach (RaycastResult result in raycastResults)
        {
            if (result.gameObject != null && result.gameObject.GetComponentInParent<Button>() != null)
            {
                return true;
            }
        }

        return false;
    }

    //初始化发声槽
    public void SetSpeakerZone(SpeakerZone zone)
    {
        speakerZone = zone;
        speakerZoneTransform = zone.speakerZoneTransform;
        speakerZoneCollider = zone.speakerZoneCollider;
        speakerZoneLayerMask = zone.speakerZoneLayerMask;
    }

    public void SetDragBounds(Collider2D boundsCollider)
    {
        dragBoundsCollider = boundsCollider;
    }

    public void OnMouseDownOnObject()
    {
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(InputManager.Instance.GetMousePosition());
        mousePos.z = 0f;

        RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero, Mathf.Infinity, itemRaycastLayerMask);
        if (hit.collider != null)
        {
            var item = hit.collider.GetComponent<IInteractive>();
            if (item != null && item.IsInteractable)
            {
                AudioManager.Instance.StartPlaySound("sfx_03_08", false);
                currentItem = item as Item;
                isDragging = true;
                offset = currentItem.transform.position - mousePos;
                currentItem.OnDragStart(mousePos);
            }
        }
    }

    public void UIEmptyClick()
    {
        if (FlowController.Instance.IsWaitForClickEmpty)
        {
            if (FlowController.Instance.CurrentState == GameFlowState.Success)
            {
                AudioManager.Instance.StartPlaySound("sfx_10_12", false);
                FlowController.Instance.ShowCaseTruth();
            }
            else if (FlowController.Instance.CurrentState == GameFlowState.CaseFail)
            {
                AudioManager.Instance.StartPlaySound("sfx_13", false);
                bool retryOwnsPanelDismissal = GameManager.Instance.RetryCurrentCase();
                if (!retryOwnsPanelDismissal)
                {
                    UIManager.Instance.HidePanel("death_panel");
                }
            }
        }
    }

    public void OnMouseDragOnObject()
    {
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(InputManager.Instance.GetMousePosition());
        mousePos.z = 0f;
        OnMouseDragOnObject(mousePos);
    }

    private void OnMouseDragOnObject(Vector3 mousePos)
    {
        if (!isDragging || currentItem == null) return;
        Vector3 targetPos = mousePos + offset;
        targetPos = ClampPositionToBounds(targetPos);
        currentItem.OnDragUpdate(targetPos);
    }

    private bool IsPointerInsideDragBounds(Vector3 mouseWorldPos)
    {
        return dragBoundsCollider == null || dragBoundsCollider.OverlapPoint(mouseWorldPos);
    }

    public void OnMouseUpOnObject()
    {
        if (!isDragging || currentItem == null) return;
        bool isOverDropZone = CheckOverlapWithDropZone(currentItem);

        if (isOverDropZone)
        {
            int itemID = currentItem.ItemID;
            GameManager.Instance.CheckWord(itemID);
            AcceptCurrentDrop();
        }
        else
        {
            RejectCurrentDrop();
        }
    }

    private Vector3 ClampPositionToBounds(Vector3 targetPos)
    {
        if (dragBoundsCollider == null || currentItem == null) return targetPos;

        Collider2D itemCol = currentItem.GetCollider();
        if (itemCol == null) return targetPos;

        Bounds bounds = dragBoundsCollider.bounds;
        Bounds itemBounds = itemCol.bounds;

        float minX = bounds.min.x + itemBounds.extents.x;
        float maxX = bounds.max.x - itemBounds.extents.x;
        float minY = bounds.min.y + itemBounds.extents.y;
        float maxY = bounds.max.y - itemBounds.extents.y;

        targetPos.x = Mathf.Clamp(targetPos.x, minX, maxX);
        targetPos.y = Mathf.Clamp(targetPos.y, minY, maxY);
        return targetPos;
    }

    private bool CheckOverlapWithDropZone(Item item)
    {
        if (speakerZoneCollider == null || item == null) return false;

        Collider2D itemCol = item.GetCollider();
        if (itemCol == null) return false;

        Bounds itemBounds = itemCol.bounds;
        Bounds zoneBounds = speakerZoneCollider.bounds;

        bool overlap = itemBounds.min.x < zoneBounds.max.x &&
                       itemBounds.max.x > zoneBounds.min.x &&
                       itemBounds.min.y < zoneBounds.max.y &&
                       itemBounds.max.y > zoneBounds.min.y;

        return overlap;
    }

    public void AcceptCurrentDrop()
    {
        if (currentItem == null) return;
        AudioManager.Instance.StartPlaySound("sfx_04_05_06", false);
        currentItem.OnDragEnd(true);
        speakerZone?.PlayAcceptedDropFeedback();
        currentItem = null;
        isDragging = false;
    }

    public void RejectCurrentDrop()
    {
        if (currentItem == null) return;
        currentItem.OnDragEnd(false);
        currentItem = null;
        isDragging = false;
    }

    public void ResetController()
    {
        if (currentItem != null && currentItem.IsDragging)
        {
            currentItem.OnDragEnd(false);
            currentItem = null;
        }
        isDragging = false;
    }
}
