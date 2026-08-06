using UnityEngine;

// 弹簧阻尼模型：二阶系统，Current（位置）在弹簧力与阻尼力作用下趋近 Target，
// 会产生过冲与回弹，并把采样喂给 2D 示波器
public class SpringDamperSample2D : MonoBehaviour
{
    public DamperScope2D Scope;
    // 刚度：弹簧力系数，越大回弹越快、震荡频率越高
    public float Stiffness = 100f;
    // 阻尼：速度衰减系数，越大越快收敛、过冲越小
    public float Damping = 10f;
    // 目标速度：期望到达目标时的速度，一般为 0
    public float TargetVelocity = 0f;
    public float Interval = 0.0167f;
    public float Current;
    // 当前速度：二阶系统的速度状态，逐帧累加
    public float Velocity;
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
        float x = Current;
        float v = Velocity;
        SpringSet.SpringDamper.SpringDamperBad(ref x, ref v, Target, TargetVelocity, Stiffness, Damping, Interval);
        Current = x;
        Velocity = v;
        if (Scope != null)
            Scope.PushSample(Current, Interval);
    }
}
