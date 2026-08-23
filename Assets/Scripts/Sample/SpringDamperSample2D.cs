using UnityEngine;

// 可选的弹簧阻尼函数
public enum ESpringFunc
{
    // 显式欧拉：Stiffness / Damping，dt 稍大会震荡甚至爆炸
    Bad,
    // 欠阻尼解析：Stiffness / Damping，仅覆盖欠阻尼情形
    ExactUnderDamped,
    // 完整解析：Stiffness / Damping，欠阻尼 / 临界 / 过阻尼三分支
    Exact,
    // 完整解析：以 Frequency / HalfLife 参数化
    ExactHalfLife,
    // 完整解析：以 DampingRatio / HalfLife 参数化
    ExactRatio,
    // 临界解析：固定临界阻尼，只需 HalfLife
    Critical,
    // 简化解析：目标速度恒为 0，只需 HalfLife
    Simple,
    // 衰减解析：目标位置与速度均为 0，把偏移平滑衰减到零
    Decay,
}

// 弹簧阻尼模型：二阶系统，Current（位置）在弹簧力与阻尼力作用下趋近 Target，
// 会产生过冲与回弹，并把采样喂给 2D 示波器
public class SpringDamperSample2D : MonoBehaviour
{
    public DamperScope2D Scope;
    // 当前使用的弹簧阻尼函数
    public ESpringFunc Func = ESpringFunc.Bad;
    // 刚度：弹簧力系数，越大回弹越快、震荡频率越高
    public float Stiffness = 60f;
    // 阻尼：速度衰减系数，越大越快收敛、过冲越小
    public float Damping = 8f;
    // 频率：每秒振荡次数，比直接给刚度更直观
    public float Frequency = 1.2f;
    // 半衰期（秒）：振幅衰减一半所需时间，越小收敛越快
    public float HalfLife = 0.3f;
    // 阻尼比：无量纲的弹性程度，<1 欠阻尼、=1 临界、>1 过阻尼
    public float DampingRatio = 0.5f;
    // 目标速度：期望到达目标时的速度，一般为 0
    public float TargetVelocity = 0f;
    // 极小值：防止除零
    public float Eps = 1e-5f;
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
        Damp(ref x, ref v, Interval);
        Current = x;
        Velocity = v;
        if (Scope != null)
            Scope.PushSample(Current, Interval);
    }

    // 按当前选择的弹簧阻尼函数推进一步，dt 为本次步进的时间间隔
    private void Damp(ref float x, ref float v, float dt)
    {
        switch (Func)
        {
            case ESpringFunc.ExactUnderDamped:
                SpringSet.SpringDamper.SpringDamperExactUnderDamped(ref x, ref v, Target, TargetVelocity, Stiffness, Damping, dt, Eps);
                break;
            case ESpringFunc.Exact:
                SpringSet.SpringDamper.SpringDamperExact(ref x, ref v, Target, TargetVelocity, Stiffness, Damping, dt, Eps);
                break;
            case ESpringFunc.ExactHalfLife:
                SpringSet.SpringDamper.SpringDamperExactHalfLife(ref x, ref v, Target, TargetVelocity, Frequency, HalfLife, dt, Eps);
                break;
            case ESpringFunc.ExactRatio:
                SpringSet.SpringDamper.SpringDamperExactRatio(ref x, ref v, Target, TargetVelocity, DampingRatio, HalfLife, dt, Eps);
                break;
            case ESpringFunc.Critical:
                SpringSet.SpringDamper.CriticalSpringDamperExact(ref x, ref v, Target, TargetVelocity, HalfLife, dt);
                break;
            case ESpringFunc.Simple:
                SpringSet.SpringDamper.SimpleSpringDamperExact(ref x, ref v, Target, HalfLife, dt);
                break;
            case ESpringFunc.Decay:
                SpringSet.SpringDamper.DecaySpringDamperExact(ref x, ref v, HalfLife, dt);
                break;
            default:
                SpringSet.SpringDamper.SpringDamperBad(ref x, ref v, Target, TargetVelocity, Stiffness, Damping, dt);
                break;
        }
    }
}
