using UnityEngine;
using UnityEngine.EventSystems;

// UI 拖动手柄：在示波器控件内垂直拖动以设定目标值，手柄跟随目标位置
public class SpringTargetHandle : MonoBehaviour, IDragHandler, IPointerDownHandler
{
    public DamperScope2D Scope;
    public SpringDamperSample2D Sample;

    private RectTransform _rect;

    private void Awake()
    {
        _rect = GetComponent<RectTransform>();
    }

    private void Update()
    {
        // 手柄跟随模型目标值，保证与数值同步
        if (Sample != null)
            SyncHandlePosition(Sample.Target);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        ApplyPointer(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        ApplyPointer(eventData);
    }

    private void ApplyPointer(PointerEventData eventData)
    {
        if (Scope == null || Sample == null)
            return;

        RectTransform scopeRect = Scope.rectTransform;
        Vector2 localPoint;
        bool ok = RectTransformUtility.ScreenPointToLocalPointInRectangle(
            scopeRect, eventData.position, eventData.pressEventCamera, out localPoint);
        if (!ok)
            return;

        float value = LocalYToValue(localPoint.y);
        Sample.SetTarget(value);
        SyncHandlePosition(value);
    }

    // 控件本地 Y 反映射为值域数值
    private float LocalYToValue(float localY)
    {
        Rect rect = Scope.rectTransform.rect;
        float height = Mathf.Max(rect.height, 1e-4f);
        float t = (localY - rect.yMin) / height;
        float value = (Mathf.Clamp01(t) * 2f - 1f) * Scope.ValueRange;
        return value;
    }

    // 把手柄放到对应值的高度（手柄与示波器同一父级坐标系）
    private void SyncHandlePosition(float value)
    {
        if (_rect == null)
            return;

        float y = Scope.ValueToLocalY(value);
        Vector2 pos = _rect.anchoredPosition;
        pos.y = y;
        _rect.anchoredPosition = pos;
    }
}
