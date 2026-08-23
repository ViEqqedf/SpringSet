using UnityEngine;
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
            // 此时发现需要有阻尼让速度收敛，否则会出现震荡，于是引入 damping * (q - v[t])
            // 会发现这个多项式演变成了加速度到速度再到位置的二阶系统：
            // a[t] = stiffness * (g - x[t]) + damping * (q - v[t])
            // v[t + dt] = v[t] + a[t] * dt
            // x[t + dt] = x[t] + v[t + dt] * dt
            // q 为目标速度，即速度要趋近的值，当 q = 0 时，意味着停下时将刚好抵达目标位置

            //     为什么？
            //     考虑 v == 0 && a == 0 的情形
            //     a = stiffness * (g - x) + damping * (0 - v)
            //       = stiffness * (g - x) - damping * 0
            //       = stiffness * (g - x) = 0
            //     此时可得 x = g

            v += dt * stiffness * (g - x) + dt * damping * (q - v);
            x += dt * v;
        }

        public static void SpringDamperExact(
            ref float x,
            ref float v,
            float xGoal,
            float vGoal,
            float stiffness,
            float damping,
            float dt,
            float eps = 1e-5f)
        {
            float g = xGoal;
            float q = vGoal;
            float s = stiffness;
            float d = damping;
            float c = g + (d * q) / (s + eps);
            float y = d / 2f;
            float w = Mathf.Sqrt(s - Mathf.Pow(d, 2) / 4f);
            float j = Mathf.Sqrt(Mathf.Pow(v + y * (x - c), 2) / (Mathf.Pow(w, 2) + eps) + Mathf.Pow(x - c, 2));
            float p = Mathf.Atan((v + (x - c) * y) / (-(x - c) * w + eps));

            j = (x - c) > 0 ? j : -j;

            float eydt = Mathf.Exp(y * dt);

            x = j * eydt * Mathf.Cos(w * dt + p) + c;
            v = -y * j * eydt * Mathf.Cos(w * dt + p) - w * j * eydt * Mathf.Sin(w * dt + p);
        }
    }
}
