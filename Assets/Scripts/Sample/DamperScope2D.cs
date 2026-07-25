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
    // 滚动速度（像素/秒）：点在屏幕上向左移动的物理速度，与 dt 无关
    public float ScrollSpeed = 400f;
    public float LineThickness = 2f;
    public Color LineColor = new Color(0.2f, 0.5f, 1f, 1f);
    public Color PointColor = new Color(0.2f, 0.5f, 1f, 1f);
    public float PointSize = 6f;
    public Color EndpointColor = new Color(0.1f, 0.35f, 0.85f, 1f);
    public float EndpointSize = 16f;
    public Color BaselineColor = new Color(1f, 1f, 1f, 0.2f);

    private float[] _buffer;
    // 与 _buffer 平行：记录每个采样点当时的 dt，使历史点间距不受后续 dt 变化影响
    private float[] _intervals;
    private int _head;

    protected override void Awake()
    {
        base.Awake();
        EnsureBuffer();
    }

    // 写入一个新采样点，触发重绘。interval 为本次采样的时间间隔（dt）
    public void PushSample(float value, float interval)
    {
        EnsureBuffer();
        _buffer[_head] = value;
        _intervals[_head] = Mathf.Max(interval, 1e-4f);
        _head = (_head + 1) % _buffer.Length;
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
            _intervals = new float[count];
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

        // 从右边缘向左累加：每个点用它自己当时的 dt 决定与前一点的间距，
        // 使已采样的历史点间距不受后续 dt 变化影响
        // 曲线：最新样本在右侧，向左滚动
        float x = rect.xMax;
        Vector2 prev = Vector2.zero;
        bool hasPrev = false;
        for (int i = 0; i < count; i++)
        {
            int bufferIndex = ((_head - 1 - i) % count + count) % count;
            if (i > 0)
                x -= ScrollSpeed * _intervals[bufferIndex];
            float y = ValueToLocalY(_buffer[bufferIndex]);
            Vector2 point = new Vector2(x, y);
            if (hasPrev)
                AddSegment(vh, prev, point, LineColor);
            prev = point;
            hasPrev = true;
            if (x < rect.xMin)
                break;
        }

        // 每个采样点上画一个菱形点
        x = rect.xMax;
        for (int i = 0; i < count; i++)
        {
            int bufferIndex = ((_head - 1 - i) % count + count) % count;
            if (i > 0)
                x -= ScrollSpeed * _intervals[bufferIndex];
            float y = ValueToLocalY(_buffer[bufferIndex]);
            AddDiamond(vh, new Vector2(x, y), PointSize, PointColor);
            if (x < rect.xMin)
                break;
        }

        // 曲线末端（最新采样）画一个大圆点表示当前值
        int latestIndex = ((_head - 1) % count + count) % count;
        Vector2 endpoint = new Vector2(rect.xMax, ValueToLocalY(_buffer[latestIndex]));
        AddCircle(vh, endpoint, EndpointSize * 0.5f, EndpointColor);
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

    // 在采样位置画一个菱形点
    private void AddDiamond(VertexHelper vh, Vector2 center, float size, Color color)
    {
        float half = size * 0.5f;
        int index = vh.currentVertCount;
        UIVertex vertex = UIVertex.simpleVert;
        vertex.color = color;

        vertex.position = center + new Vector2(0f, half);
        vh.AddVert(vertex);
        vertex.position = center + new Vector2(half, 0f);
        vh.AddVert(vertex);
        vertex.position = center + new Vector2(0f, -half);
        vh.AddVert(vertex);
        vertex.position = center + new Vector2(-half, 0f);
        vh.AddVert(vertex);

        vh.AddTriangle(index, index + 1, index + 2);
        vh.AddTriangle(index + 2, index + 3, index);
    }

    // 在指定位置画一个圆（用扇形三角面近似）
    private void AddCircle(VertexHelper vh, Vector2 center, float radius, Color color)
    {
        const int SEGMENTS = 24;
        int centerIndex = vh.currentVertCount;
        UIVertex vertex = UIVertex.simpleVert;
        vertex.color = color;

        vertex.position = center;
        vh.AddVert(vertex);

        for (int i = 0; i <= SEGMENTS; i++)
        {
            float angle = (float)i / SEGMENTS * Mathf.PI * 2f;
            Vector2 offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
            vertex.position = center + offset;
            vh.AddVert(vertex);
        }

        for (int i = 0; i < SEGMENTS; i++)
            vh.AddTriangle(centerIndex, centerIndex + i + 1, centerIndex + i + 2);
    }
}
