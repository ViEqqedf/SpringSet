using UnityEngine;

// 可选的阻尼函数
public enum EDamperFunc
{
    // 线性插值：Damper_Base，Factor 为插值速率
    Base,
    // 指数阻尼：Damper_Exponential，Damping 为阻尼系数
    Exponential,
    // 精确阻尼：Damper_Exact，HalfLife 为半衰期（秒）
    Exact,
}

// 纯数值阻尼模型：Current 以阻尼方式趋近 Target，并把采样喂给 2D 示波器
public class DamperSample2D : MonoBehaviour
{
    public DamperScope2D Scope;
    // 当前使用的阻尼函数
    public EDamperFunc Func = EDamperFunc.Base;
    // Damper_Base 的插值速率
    public float Factor = 5f;
    // Damper_Exponential 的阻尼系数
    public float Damping = 10f;
    // Damper_Exponential 的预期固定阻尼帧时间
    public float Ft = 1.0f / 60.0f;
    // Damper_Exact 的半衰期（秒）
    public float HalfLife = 0.1f;
    // Damper_Exact 的极小值，防止除零
    public float Eps = 1e-5f;
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
        Current = Damp(Current, Target, Interval);
        if (Scope != null)
            Scope.PushSample(Current, Interval);
    }

    // 按当前选择的阻尼函数计算下一帧的值，dt 为本次步进的时间间隔
    private float Damp(float current, float target, float dt)
    {
        switch (Func)
        {
            case EDamperFunc.Exponential:
                return SpringSet.Damper.DamperExponential(current, target, Damping, dt, Ft);
            case EDamperFunc.Exact:
                return SpringSet.Damper.DamperExact(current, target, HalfLife, dt, Eps);
            default:
                return SpringSet.Damper.DamperBase(current, target, Factor * dt);
        }
    }
}
