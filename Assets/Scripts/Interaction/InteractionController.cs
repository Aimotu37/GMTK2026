using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;

public class InteractionController : SingletonMono<InteractionController>
{
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

        if (UnityEngine.EventSystems.EventSystem.current != null &&
            UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
            return;

        if (InputManager.Instance.GetMouseDown(0))
        {
            OnMouseDownOnItem();
        }

        if (InputManager.Instance.GetMouse(0))
        {
            OnMouseDragOnItem();
        }

        if (InputManager.Instance.GetMouseUp(0))
        {
            OnMouseUpOnItem();
        }
    }

    //初始化发声槽
    public void SetSpeakerZone(SpeakerZone zone)
    {
        speakerZoneTransform = zone.speakerZoneTransform;
        speakerZoneCollider = zone.speakerZoneCollider;
        speakerZoneLayerMask = zone.speakerZoneLayerMask;
    }

    public void SetDragBounds(Collider2D boundsCollider)
    {
        dragBoundsCollider = boundsCollider;
    }

    public void OnMouseDownOnItem()
    {
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(InputManager.Instance.GetMousePosition());
        mousePos.z = 0f;

        RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero, Mathf.Infinity, itemRaycastLayerMask);
        if (hit.collider != null)
        {
            var item = hit.collider.GetComponent<IInteractive>();
            if (item != null && item.IsInteractable)
            {
                currentItem = item as Item;
                isDragging = true;
                offset = currentItem.transform.position - mousePos;
                currentItem.OnDragStart(mousePos);
            }
        }
    }

    public void OnMouseDragOnItem()
    {
        if (!isDragging || currentItem == null) return;
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(InputManager.Instance.GetMousePosition());
        mousePos.z = 0f;
        Vector3 targetPos = mousePos + offset;
        targetPos = ClampPositionToBounds(targetPos);
        currentItem.OnDragUpdate(targetPos);
    }

    public void OnMouseUpOnItem()
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
        currentItem.OnDragEnd(true);
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