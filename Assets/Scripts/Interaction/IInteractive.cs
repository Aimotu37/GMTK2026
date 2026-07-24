using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IInteractive
{
    // 唯一标识
    int ItemID { get; }

    // 交互状态
    bool IsInteractable { get; }
    bool IsDragging { get; set; }

    void OnDragStart(Vector3 mouseWorldPos);
    void OnDragUpdate(Vector3 mouseWorldPos);
    void OnDragEnd(bool isAccepted);

    Transform transform { get; }
    GameObject gameObject { get; }
    Collider2D GetCollider();
}
