namespace SpringSet
{
    public class SpringDamper
    {
        public static void SpringDamperBad(
            ref float x,
            ref float v,
            float g,
            float q,
            float stiffness,
            float damping,
            float dt)
        {
            // 在 Damper 的基础上
            // 将设置速度调整为累加速度来实现平滑效果，因此引入 stiffness * (g - x[t])
            // 此时发现需要有阻尼让推导式收敛，否则会出现震荡，于是引入 damping * (q - v[t])
            // 会发现这个多项式演变成了加速度到速度再到位置的二阶系统：
            // a[t] = stiffness * (g - x[t]) + damping * (q - v[t])
            // v[t + dt] = v[t] + a[t] * dt
            // x[t + dt] = x[t] + v[t + dt] * dt

            v += dt * stiffness * (g - x) + dt * damping * (q - v);
            x += dt * v;
        }
    }
}
