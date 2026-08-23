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
            // 在 SpringDamperBad 的基础上
            // SpringDamperBad 采用显式欧拉数值积分（逐步累加 a -> v -> x），dt 稍大就会震荡甚至爆炸
            // SpringDamperExact 改用解析解，对任意 dt 都精确且稳定

            // 推导起点：把运动方程整理成标准二阶线性微分方程
            //     a = s * (g - x) + d * (q - v)
            //     展开、移项、取负后（其中 a = x''，v = x'）得到：
            //     x'' + d * x' + s * x = s * g + d * q

            // 观察图像时发现震荡的周期性图形类似于三角函数，因此猜一个衰减振荡形式的解（此实现仅覆盖欠阻尼情形 s - d^2 / 4 > 0）：
            //     x[t] = j * e^(-y * t) * cos(w * t + p) + c
            // 代入方程后，cos、sin、常数三者线性无关，要对所有 t 恒成立，各自系数必须为零
            // 由此逐一解出下面 5 个参数：
            //     c：平衡位置，令 x'' = x' = 0 得 s * c = s * g + d * q，故 c = g + d * q / s
            //     y：衰减率，由特征方程 r^2 + d * r + s = 0 得 y = d / 2
            //     w：振动频率，w = sqrt(s - d^2 / 4)
            //     j：振幅，由初始 x、v 与平衡位置 c 决定
            //     p：相位，由初始 x、v 与平衡位置 c 决定
            // 最终 x 即解析式本身，v 为该式对 t 求导的结果

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

            // 注意是负指数 e^(-y * dt)，振幅随时间衰减，系统才会收敛
            float eydt = Mathf.Exp(-y * dt);

            x = j * eydt * Mathf.Cos(w * dt + p) + c;
            v = -y * j * eydt * Mathf.Cos(w * dt + p) - w * j * eydt * Mathf.Sin(w * dt + p);
        }
    }
}
