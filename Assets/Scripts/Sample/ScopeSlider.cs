using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// 参数类型：决定滑条驱动 DamperSample2D 的哪个字段
public enum EScopeParam
{
    Factor,
    Interval,
}

// 自绘滑条：拖动设定 [MinValue, MaxValue] 之间的值，并写回 DamperSample2D
public class ScopeSlider : Graphic, IDragHandler, IPointerDownHandler
{
    public DamperSample2D Sample;
    public EScopeParam Param = EScopeParam.Factor;
    public float MinValue = 0f;
    public float MaxValue = 1f;
    public Color TrackColor = new Color(0.75f, 0.75f, 0.75f, 1f);
    public Color FillColor = new Color(0.5f, 0.8f, 1f, 1f);
    public Text ValueLabel;

    private float _value;

    protected override void Start()
    {
        base.Start();
        _value = ReadParam();
        SetVerticesDirty();
        RefreshLabel();
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
        Vector2 localPoint;
        bool ok = RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rectTransform, eventData.position, eventData.pressEventCamera, out localPoint);
        if (!ok)
            return;

        Rect rect = rectTransform.rect;
        float width = Mathf.Max(rect.width, 1e-4f);
        float t = Mathf.Clamp01((localPoint.x - rect.xMin) / width);
        _value = Mathf.Lerp(MinValue, MaxValue, t);
        WriteParam(_value);
        SetVerticesDirty();
        RefreshLabel();
    }

    private float ReadParam()
    {
        if (Sample == null)
            return MinValue;
        if (Param == EScopeParam.Factor)
            return Sample.Factor;
        return Sample.Interval;
    }

    private void WriteParam(float value)
    {
        if (Sample == null)
            return;
        if (Param == EScopeParam.Factor)
        {
            Sample.Factor = value;
            return;
        }
        Sample.Interval = value;
    }

    private void RefreshLabel()
    {
        if (ValueLabel == null)
            return;
        ValueLabel.text = _value.ToString("0.000");
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        Rect rect = rectTransform.rect;
        float range = Mathf.Max(MaxValue - MinValue, 1e-4f);
        float t = Mathf.Clamp01((_value - MinValue) / range);
        float fillX = Mathf.Lerp(rect.xMin, rect.xMax, t);

        // 轨道背景
        AddQuad(vh, rect.xMin, rect.xMax, rect.yMin, rect.yMax, TrackColor);
        // 已填充部分
        AddQuad(vh, rect.xMin, fillX, rect.yMin, rect.yMax, FillColor);
    }

    private void AddQuad(VertexHelper vh, float xMin, float xMax, float yMin, float yMax, Color color)
    {
        if (xMax - xMin < 1e-4f)
            return;

        int index = vh.currentVertCount;
        UIVertex vertex = UIVertex.simpleVert;
        vertex.color = color;

        vertex.position = new Vector2(xMin, yMin);
        vh.AddVert(vertex);
        vertex.position = new Vector2(xMin, yMax);
        vh.AddVert(vertex);
        vertex.position = new Vector2(xMax, yMax);
        vh.AddVert(vertex);
        vertex.position = new Vector2(xMax, yMin);
        vh.AddVert(vertex);

        vh.AddTriangle(index, index + 1, index + 2);
        vh.AddTriangle(index + 2, index + 3, index);
    }
}
