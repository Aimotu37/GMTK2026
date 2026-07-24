using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class InteractionController : SingletonMono<InteractionController>
{
    [SerializeField] private Transform speakerZoneTransform;   // 发声槽的位置（用于磁吸）
    [SerializeField] private Collider2D speakerZoneCollider;   // 发声槽的碰撞体（用于检测）
    [SerializeField] private LayerMask speakerZoneLayerMask;   // 或者用 Layer 检测
    public Vector3 SpeakerZonePosition => speakerZoneTransform.position;

    public TypewriterEffect typewriter;

    [Header("调试信息")]
    [SerializeField] private Item currentItem;

    public UnityAction<string, Item> OnItemDroppedToZone;

    private Vector3 offset;
    private bool isDragging = false;

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

    public void OnMouseDownOnItem()
    {
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(InputManager.Instance.GetMousePosition());
        mousePos.z = 0f;

        RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero);
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
        currentItem.OnDragUpdate(targetPos);
    }

    public void OnMouseUpOnItem()
    {
        if (!isDragging || currentItem == null) return;
        bool isOverDropZone = CheckOverlapWithDropZone(currentItem);

        if (isOverDropZone)
        {
            string itemID = currentItem.ItemID;
            GameManager.Instance.CheckWord(itemID);
            AcceptCurrentDrop();
        }
        else
        {
            RejectCurrentDrop();
        }
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