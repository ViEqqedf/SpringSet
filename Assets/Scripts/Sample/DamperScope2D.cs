using UnityEngine;
using UnityEngine.UI;

// 2D 示波器：继承 UGUI Graphic，在 Canvas 上自绘阻尼趋近曲线
// 不依赖世界空间，避免 LineRenderer 的视角/透视偏差
[RequireComponent(typeof(CanvasRenderer))]
public class DamperScope2D : Graphic
{
    public int SampleCount = 256;
    // 值域：曲线在控件内映射的范围，[-ValueRange, +ValueRange] 对应控件上下边缘
    public float ValueRange = 1f;
    public float LineThickness = 2f;
    public Color LineColor = Color.green;
    public Color TargetLineColor = new Color(1f, 0.6f, 0.2f, 1f);
    public Color BaselineColor = new Color(1f, 1f, 1f, 0.2f);

    private float[] _buffer;
    private int _head;
    private float _target;

    protected override void Awake()
    {
        base.Awake();
        EnsureBuffer();
    }

    // 写入一个新采样点，触发重绘
    public void PushSample(float value)
    {
        EnsureBuffer();
        _buffer[_head] = value;
        _head = (_head + 1) % _buffer.Length;
        SetVerticesDirty();
    }

    // 设定目标值，用于绘制目标横线
    public void SetTarget(float target)
    {
        _target = target;
        SetVerticesDirty();
    }

    // 把值域映射到控件本地 Y 坐标，供曲线与手柄共用
    public float ValueToLocalY(float value)
    {
        Rect rect = rectTransform.rect;
        float range = Mathf.Max(ValueRange, 1e-4f);
        float t = Mathf.Clamp(value / range, -1f, 1f) * 0.5f + 0.5f;
        return Mathf.Lerp(rect.yMin, rect.yMax, t);
    }

    private void EnsureBuffer()
    {
        int count = Mathf.Max(SampleCount, 2);
        if (_buffer == null || _buffer.Length != count)
        {
            _buffer = new float[count];
            _head = 0;
        }
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        EnsureBuffer();

        Rect rect = rectTransform.rect;
        int count = _buffer.Length;

        // 基准线（value = 0）
        DrawHorizontalLine(vh, ValueToLocalY(0f), rect, BaselineColor);
        // 目标横线
        DrawHorizontalLine(vh, ValueToLocalY(_target), rect, TargetLineColor);

        // 曲线：最新样本在右侧，向左滚动
        float spacing = rect.width / (count - 1);
        Vector2 prev = Vector2.zero;
        bool hasPrev = false;
        for (int i = 0; i < count; i++)
        {
            int bufferIndex = ((_head - 1 - i) % count + count) % count;
            float x = rect.xMax - i * spacing;
            float y = ValueToLocalY(_buffer[bufferIndex]);
            Vector2 point = new Vector2(x, y);
            if (hasPrev)
                AddSegment(vh, prev, point, LineColor);
            prev = point;
            hasPrev = true;
        }
    }

    private void DrawHorizontalLine(VertexHelper vh, float y, Rect rect, Color color)
    {
        Vector2 left = new Vector2(rect.xMin, y);
        Vector2 right = new Vector2(rect.xMax, y);
        AddSegment(vh, left, right, color);
    }

    // 用一个带厚度的四边形绘制一段线
    private void AddSegment(VertexHelper vh, Vector2 a, Vector2 b, Color color)
    {
        Vector2 dir = b - a;
        if (dir.sqrMagnitude < 1e-8f)
            return;

        dir.Normalize();
        Vector2 normal = new Vector2(-dir.y, dir.x) * (LineThickness * 0.5f);

        int index = vh.currentVertCount;
        UIVertex vertex = UIVertex.simpleVert;
        vertex.color = color;

        vertex.position = a - normal;
        vh.AddVert(vertex);
        vertex.position = a + normal;
        vh.AddVert(vertex);
        vertex.position = b + normal;
        vh.AddVert(vertex);
        vertex.position = b - normal;
        vh.AddVert(vertex);

        vh.AddTriangle(index, index + 1, index + 2);
        vh.AddTriangle(index + 2, index + 3, index);
    }
}
