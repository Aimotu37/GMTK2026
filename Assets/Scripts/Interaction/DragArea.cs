using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DragArea : MonoBehaviour
{
    private BoxCollider2D col;

    void Start()
    {
        col = GetComponent<BoxCollider2D>();
        InteractionController.Instance.SetDragBounds(col);
    }
}
