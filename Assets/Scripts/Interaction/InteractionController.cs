using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class InteractionController : MonoBehaviour
{
    private static InteractionController _instance;
    public static InteractionController Instance
    {
        get
        {
            if (_instance == null)
            {
                //尝试获取现有实例
                _instance = FindFirstObjectByType<InteractionController>();
                if (_instance == null)
                {
                    GameObject singleton = new GameObject(typeof(InteractionController).Name);
                    _instance = singleton.AddComponent<InteractionController>();
                }
            }
            return _instance;
        }
    }

    [Header("交互区域引用")]
    [SerializeField] private Transform dropZoneTransform;   // 发声槽的位置（用于磁吸）
    [SerializeField] private Collider2D dropZoneCollider;   // 发声槽的碰撞体（用于检测）
    [SerializeField] private LayerMask dropZoneLayerMask;   // 或者用 Layer 检测

    public TypewriterEffect typewriter;

    [Header("调试信息")]
    [SerializeField] private Item currentItem;
    public Vector3 DropZonePosition => dropZoneTransform != null ? dropZoneTransform.position : Vector3.zero;

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
            if (GameManager.Instance.CheckWord(currentItem.ItemID))
            {
                AcceptCurrentDrop();
            }
            else
            {
                RejectCurrentDrop();
            }
        }
        else
        {
            currentItem.OnDragEnd(false);
            currentItem = null;
            isDragging = false;
        }
    }

    private bool CheckOverlapWithDropZone(Item item)
    {
        if (dropZoneCollider == null || item == null) return false;

        Collider2D itemCol = item.GetCollider();
        if (itemCol == null) return false;

        Bounds itemBounds = itemCol.bounds;
        Bounds zoneBounds = dropZoneCollider.bounds;

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