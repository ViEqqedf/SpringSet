using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class DamperScope : MonoBehaviour
{
    public DamperSample Source;
    public int SampleCount = 256;
    public float Length = 8f;
    public float ValueScale = 1f;

    private LineRenderer _line;
    private float[] _buffer;
    private int _head;
    private Vector3 _timeDir;
    private Vector3 _valueDir;

    private void Start()
    {
        _line = GetComponent<LineRenderer>();
        _line.useWorldSpace = true;
        _line.positionCount = SampleCount;
        _buffer = new float[SampleCount];
        _head = 0;

        UpdateDirections();

        float initCoord = Source != null ? Source.CurrentCoord : 0f;
        for (int i = 0; i < SampleCount; i++)
            _buffer[i] = initCoord;
        RefreshLine();
    }

    private void FixedUpdate()
    {

    }

    public void UpdateScope()
    {
        if (Source == null)
            return;

        UpdateDirections();
        _buffer[_head] = Source.CurrentCoord;
        _head = (_head + 1) % SampleCount;
        RefreshLine();
    }

    private void UpdateDirections()
    {
        _valueDir = Source != null ? Source.AxisDir : Vector3.right;
        Vector3 timeDir = Vector3.Cross(_valueDir, Vector3.up);
        if (timeDir.sqrMagnitude < 1e-6f)
            timeDir = Vector3.Cross(_valueDir, Vector3.forward);
        _timeDir = timeDir.normalized;
    }

    private void RefreshLine()
    {
        float spacing = Length / Mathf.Max(SampleCount - 1, 1) * (Source.Interval / 0.0167f);
        Vector3 origin = transform.position;
        for (int i = 0; i < SampleCount; i++)
        {
            int bufferIndex = ((_head - 1 - i) % SampleCount + SampleCount) % SampleCount;
            float value = _buffer[bufferIndex] * ValueScale;
            Vector3 pos = origin + _timeDir * (i * spacing) + _valueDir * value;
            _line.SetPosition(i, pos);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (Source == null)
            return;

        Vector3 timeDir = Vector3.Cross(Source.AxisDir, Vector3.up);
        if (timeDir.sqrMagnitude < 1e-6f)
            timeDir = Vector3.Cross(Source.AxisDir, Vector3.forward);
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, transform.position + timeDir.normalized * Length);
    }
}
