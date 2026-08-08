using UnityEngine;
using UnityEngine.UI;

public class TutorialGuidePanel : BasePanel, ICanvasRaycastFilter
{
    private Collider2D _targetCollider;
    private RectTransform _settingButtonRect;

    public void Configure(Collider2D targetCollider, RectTransform settingButtonRect)
    {
        _targetCollider = targetCollider;
        _settingButtonRect = settingButtonRect;
    }

    public bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
    {
        if (_settingButtonRect != null &&
            RectTransformUtility.RectangleContainsScreenPoint(
                _settingButtonRect,
                screenPoint,
                eventCamera))
        {
            return false;
        }

        if (_targetCollider == null)
        {
            return true;
        }

        Camera worldCamera = Camera.main;
        if (worldCamera == null)
        {
            return true;
        }

        Ray pointerRay = worldCamera.ScreenPointToRay(screenPoint);
        Plane targetPlane = new Plane(Vector3.forward, _targetCollider.transform.position);
        if (!targetPlane.Raycast(pointerRay, out float distance))
        {
            return true;
        }

        Vector3 worldPoint = pointerRay.GetPoint(distance);
        return !_targetCollider.OverlapPoint(worldPoint);
    }

    protected override void OnClose()
    {
        _targetCollider = null;
        _settingButtonRect = null;
    }
}
