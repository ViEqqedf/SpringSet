using UnityEngine;
using DefaultNamespace;

public class DamperSample : MonoBehaviour
{
    public Transform Target;
    public Vector3 Axis = Vector3.right;
    public float Factor = 5f;
    public float AxisLength = 10f;

    private Vector3 _origin;
    private Vector3 _normalizedAxis;
    private Camera _mainCamera;
    private bool _dragging;
    private float _dragGrabOffset;

    private void Start()
    {
        _origin = transform.position;
        _normalizedAxis = Axis.sqrMagnitude > 0f ? Axis.normalized : Vector3.right;
        _mainCamera = Camera.main;

        if (Target != null)
        {
            float initCoord = ProjectToAxis(Target.position);
            Target.position = _origin + _normalizedAxis * initCoord;
        }
    }

    private void Update()
    {
        HandleDrag();
        ApplyDamper();
    }

    private void HandleDrag()
    {
        if (Target == null)
            return;
        if (_mainCamera == null)
            return;

        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
            Collider targetCollider = Target.GetComponent<Collider>();
            if (targetCollider != null && targetCollider.Raycast(ray, out RaycastHit hit, 1000f))
            {
                _dragging = true;
                float targetCoord = ProjectToAxis(Target.position);
                float pickedCoord = ClosestAxisCoord(ray);
                _dragGrabOffset = targetCoord - pickedCoord;
            }
        }

        if (Input.GetMouseButtonUp(0))
            _dragging = false;

        if (_dragging)
        {
            Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
            float coord = ClosestAxisCoord(ray) + _dragGrabOffset;
            coord = Mathf.Clamp(coord, -AxisLength, AxisLength);
            Target.position = _origin + _normalizedAxis * coord;
        }
    }

    private void ApplyDamper()
    {
        if (Target == null)
            return;

        float currentCoord = ProjectToAxis(transform.position);
        float targetCoord = ProjectToAxis(Target.position);
        float nextCoord = SpringSet.DamperCalc(currentCoord, targetCoord, Factor * Time.deltaTime);

        Vector3 perpendicular = transform.position - (_origin + _normalizedAxis * currentCoord);
        transform.position = _origin + _normalizedAxis * nextCoord + perpendicular;
    }

    private float ProjectToAxis(Vector3 worldPos)
    {
        return Vector3.Dot(worldPos - _origin, _normalizedAxis);
    }

    private float ClosestAxisCoord(Ray ray)
    {
        Vector3 w0 = _origin - ray.origin;
        float a = Vector3.Dot(_normalizedAxis, _normalizedAxis);
        float b = Vector3.Dot(_normalizedAxis, ray.direction);
        float c = Vector3.Dot(ray.direction, ray.direction);
        float d = Vector3.Dot(_normalizedAxis, w0);
        float e = Vector3.Dot(ray.direction, w0);
        float denom = a * c - b * b;
        if (Mathf.Abs(denom) < 1e-6f)
            return ProjectToAxis(ray.origin);
        return (b * e - c * d) / denom;
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 origin = Application.isPlaying ? _origin : transform.position;
        Vector3 axisDir = Axis.sqrMagnitude > 0f ? Axis.normalized : Vector3.right;
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(origin - axisDir * AxisLength, origin + axisDir * AxisLength);
    }
}
