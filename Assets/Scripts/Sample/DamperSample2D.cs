using UnityEngine;
using DefaultNamespace;

// 纯数值阻尼模型：Current 以阻尼方式趋近 Target，并把采样喂给 2D 示波器
public class DamperSample2D : MonoBehaviour
{
    public DamperScope2D Scope;
    public float Factor = 5f;
    public float Interval = 0.0167f;
    public float Current;
    public float Target;

    private float _timer;

    private void Update()
    {
        _timer += Time.deltaTime;
        Interval = Mathf.Max(0.0167f, Interval);
        while (_timer >= Interval)
        {
            _timer -= Interval;
            Step();
        }
    }

    // 设定目标值，供拖动手柄回调
    public void SetTarget(float target)
    {
        Target = target;
    }

    private void Step()
    {
        Current = SpringSet.DamperCalc(Current, Target, Factor * Interval);
        if (Scope != null)
            Scope.PushSample(Current, Interval);
    }
}
