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

        /// 精确弹簧阻尼的欠阻尼问题处理版本，会发现 s - d^2 / 4 的三种临界情形未考虑周全
        public static void SpringDamperExactUnderDamped(
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
            // 相比 SpringDamperExactUnderDamped 只覆盖欠阻尼一种情形，
            // SpringDamperExact 补全了临界阻尼与过阻尼两个分支，对任意 stiffness、damping 都成立

            // 推导起点：把运动方程整理成标准二阶线性微分方程（其中 a = x''，v = x'）
            //     a = s * (g - x) + d * (q - v)
            //     => x'' + d * x' + s * x = s * g + d * q

            // 为什么要解特征方程：微分方程的未知是一整个函数，导数把 x 锁死无法直接解；
            // 猜 e^(r*t) 后导数全变成 r 的乘方，方程塌缩成会解的二次方程，即把“函数问题”翻译成“数字问题”
            // 特征方程从哪来：先解齐次部分 x'' + d * x' + s * x = 0
            // 为什么猜指数：只有指数函数求导后形状不变（各阶导保持固定比例），代入才能提出公因子约去，
            //     换多项式、三角、对数等都做不到。因此猜解 x = e^(r * t)，代入得：
            //     r^2 * e^(r*t) + d * r * e^(r*t) + s * e^(r*t) = 0
            //     e^(r*t) 恒不为零，两边约去后得到特征方程：r^2 + d * r + s = 0
            //     规律：几阶导数就换成 r 的几次方（x'' -> r^2，x' -> r，x -> 1）
            // 求根 r = -d / 2 ± sqrt(d^2 / 4 - s)，解出根即读出整条运动曲线的形状

            // 根 r 为什么能决定系统性格：把复根 r = -y ± i * w 代入 e^(r*t)，用 e^(a+b)=e^a*e^b 拆成两个相乘的因子：
            //     e^(r*t) = e^(-y*t) * e^(i*w*t)
            //     实部那块 e^(-y*t)：实数，因 y > 0 单调衰减到 0，故实部（-y）管“衰减多快”
            //     虚部那块 e^(i*w*t)：由欧拉公式 = cos(w*t) + i*sin(w*t)，是纯振荡，故虚部（w）管“是否振荡及频率”
            //     两者相乘即“振幅衰减的振荡”；若虚部 w = 0 则振荡因子退化成 1，振荡消失 -> 临界/过阻尼不振荡
            // 为什么 y = d / 2：y 就是特征根实部的绝对值，来自求根公式的 -b/(2a) = -d/2（两个根平摊 d）；
            //     三种阻尼状态实部都是 -d / 2，故 y = d / 2 恒成立，只有根号那部分决定要不要振荡

            // 为什么通解是“两个解的组合”：
            //     二阶系统有位置和速度两个自由度，需要两个自由常数才能匹配任意初始 x、v
            //     线性齐次方程满足叠加原理：若 A、B 各是解，则 j0 * A + j1 * B 也是解（因为求导是线性的）
            //     故取两个线性无关的基础解、用两个常数组合，即可表示所有可能的运动

            // 判别式 s - d^2 / 4 的符号决定特征根类型，进而决定“该猜什么形式的解”，分三种阻尼状态：
            //     > 0 欠阻尼：一对复根 -y ± i * w，实数解为 j * e^(-y * t) * cos(w * t + p) + c，会振荡
            //     < 0 过阻尼：两个不同实根，解为 j0 * e^(-y0 * t) + j1 * e^(-y1 * t) + c，缓慢趋近
            //         注意此时特征方程根号内 d^2 / 4 - s 为正（与频率 w = sqrt(s - d^2 / 4) 的根号符号相反），故为实根不振荡
            //     = 0 临界阻尼：重根 r = -y 只给出一个解 e^(-y * t)，但二阶需两个独立解，第二个用 t * e^(-y * t) 补足
            //         这个 t 的来历：取过阻尼两解的差商 (e^(r0*t) - e^(r1*t)) / (r0 - r1)，令 r0 -> r1
            //         其极限恰为对 r 求导 d/dr[e^(r*t)] = t * e^(r*t)（t 视作常数、r 为变量），解为 (j0 + j1 * t) * e^(-y * t) + c

            // 三种情形共用两个参数：
            //     c：平衡位置（最终停在哪），令 x'' = x' = 0 得 c = g + d * q / s
            //     y：衰减率，y = d / 2（即特征根的实部）

            float g = xGoal;
            float q = vGoal;
            float s = stiffness;
            float d = damping;
            float c = g + d * q / (s + eps);
            float y = d / 2f;

            if (Mathf.Abs(s - d * d / 4f) < eps)
            {
                // 临界阻尼：重根 r = -y，缺失的第二个独立解用 t * e^(-y * t) 补足
                // 由初始条件定：j0 = x - c（初始偏移），j1 = v + j0 * y（匹配初始速度）
                // 下面 x、v 即 x = (j0 + j1 * t) * e^(-y * t) + c 及其对 t 的导数
                float j0 = x - c;
                float j1 = v + j0 * y;
                float eydt = Mathf.Exp(-y * dt);

                x = j0 * eydt + dt * j1 * eydt + c;
                v = -y * j0 * eydt - y * dt * j1 * eydt + j1 * eydt;
            }
            else if (s - d * d / 4f > 0f)
            {
                // 欠阻尼：复根 -y ± i * w，虚部 w 成为 cos / sin 的振荡频率
                // w 振动频率、j 振幅、p 相位；j、p 由初始 x、v 与 c 决定
                // p 用单参 Atan 值域只有半圈，故用 j 的正负号补足另外半圈
                float w = Mathf.Sqrt(s - d * d / 4f);
                float j = Mathf.Sqrt((v + y * (x - c)) * (v + y * (x - c)) / (w * w + eps) + (x - c) * (x - c));
                float p = Mathf.Atan((v + (x - c) * y) / (-(x - c) * w + eps));

                j = (x - c) > 0f ? j : -j;

                float eydt = Mathf.Exp(-y * dt);

                x = j * eydt * Mathf.Cos(w * dt + p) + c;
                v = -y * j * eydt * Mathf.Cos(w * dt + p) - w * j * eydt * Mathf.Sin(w * dt + p);
            }
            else
            {
                // 过阻尼：两个不同的正实衰减率 y0 > y1，解为两个衰减指数叠加，无振荡
                // j0、j1 由初始 x、v 与 c 决定；衰减较慢的 y1 项主导后期趋近速度
                float y0 = (d + Mathf.Sqrt(d * d - 4f * s)) / 2f;
                float y1 = (d - Mathf.Sqrt(d * d - 4f * s)) / 2f;
                float j1 = (c * y0 - x * y0 - v) / (y1 - y0);
                float j0 = x - j1 - c;

                float ey0dt = Mathf.Exp(-y0 * dt);
                float ey1dt = Mathf.Exp(-y1 * dt);

                x = j0 * ey0dt + j1 * ey1dt + c;
                v = -y0 * j0 * ey0dt - y1 * j1 * ey1dt;
            }
        }

        // 频率转刚度：无阻尼弹簧 x'' + s * x = 0 的角频率为 sqrt(s)，而角频率 = 2 * PI * frequency，
        // 故 s = (2 * PI * frequency)^2。用 frequency（每秒振荡多少次）比直接给 stiffness 更直观
        public static float FrequencyToStiffness(float frequency)
        {
            float angular = 2f * Mathf.PI * frequency;
            return angular * angular;
        }

        // 刚度转频率：上式的逆运算 frequency = sqrt(s) / (2 * PI)
        public static float StiffnessToFrequency(float stiffness)
        {
            return Mathf.Sqrt(stiffness) / (2f * Mathf.PI);
        }

        // 半衰期转阻尼：由振幅衰减一半 e^(-(d / 2) * halfLife) = 1 / 2 得 d = 2 * ln(2) / halfLife，
        // 额外再乘 2（即 4 * ln(2)）让此处 halfLife 的手感与基础阻尼器的 halfLife 更一致
        public static float HalfLifeToDamping(float halfLife, float eps = 1e-5f)
        {
            return (4f * 0.69314718056f) / (halfLife + eps);
        }

        // 阻尼转半衰期：上式的逆运算，形式完全对称
        public static float DampingToHalfLife(float damping, float eps = 1e-5f)
        {
            return (4f * 0.69314718056f) / (damping + eps);
        }

        // 给定 frequency，求恰好达到临界阻尼所需的 halfLife
        // 临界条件 s = d^2 / 4，即 d = 2 * sqrt(s) = sqrt(4 * s)，再由 d 换算成 halfLife
        public static float CriticalHalfLife(float frequency)
        {
            return DampingToHalfLife(Mathf.Sqrt(FrequencyToStiffness(frequency) * 4f));
        }

        // 给定 halfLife，求恰好达到临界阻尼所需的 frequency
        // 临界条件 s = d^2 / 4，先由 halfLife 得 d，再算 s = d^2 / 4，最后换算成 frequency
        public static float CriticalFrequency(float halfLife)
        {
            float d = HalfLifeToDamping(halfLife);
            return StiffnessToFrequency(d * d / 4f);
        }

        public static void SpringDamperExactHalfLife(
            ref float x,
            ref float v,
            float xGoal,
            float vGoal,
            float frequency,
            float halfLife,
            float dt,
            float eps = 1e-5f)
        {
            // 与 SpringDamperExact 的数学主体完全相同，唯一区别是入参更直观：
            // 用 frequency（振荡快慢）与 halfLife（收敛快慢）代替抽象的 stiffness 和 damping，
            // 进入函数后立即换算回 s、d，后续三分支逻辑一字不差（详见 SpringDamperExact 注释）
            //     s = FrequencyToStiffness(frequency) = (2 * PI * frequency)^2
            //     d = HalfLifeToDamping(halfLife)     = 4 * ln(2) / halfLife
            // 想让系统恰好临界阻尼，可用 CriticalHalfLife / CriticalFrequency 由一个参数反推另一个

            float g = xGoal;
            float q = vGoal;
            float s = FrequencyToStiffness(frequency);
            float d = HalfLifeToDamping(halfLife);
            float c = g + d * q / (s + eps);
            float y = d / 2f;

            if (Mathf.Abs(s - d * d / 4f) < eps)
            {
                // 临界阻尼
                float j0 = x - c;
                float j1 = v + j0 * y;
                float eydt = Mathf.Exp(-y * dt);

                x = j0 * eydt + dt * j1 * eydt + c;
                v = -y * j0 * eydt - y * dt * j1 * eydt + j1 * eydt;
            }
            else if (s - d * d / 4f > 0f)
            {
                // 欠阻尼
                float w = Mathf.Sqrt(s - d * d / 4f);
                float j = Mathf.Sqrt((v + y * (x - c)) * (v + y * (x - c)) / (w * w + eps) + (x - c) * (x - c));
                float p = Mathf.Atan((v + (x - c) * y) / (-(x - c) * w + eps));

                j = (x - c) > 0f ? j : -j;

                float eydt = Mathf.Exp(-y * dt);

                x = j * eydt * Mathf.Cos(w * dt + p) + c;
                v = -y * j * eydt * Mathf.Cos(w * dt + p) - w * j * eydt * Mathf.Sin(w * dt + p);
            }
            else
            {
                // 过阻尼
                float y0 = (d + Mathf.Sqrt(d * d - 4f * s)) / 2f;
                float y1 = (d - Mathf.Sqrt(d * d - 4f * s)) / 2f;
                float j1 = (c * y0 - x * y0 - v) / (y1 - y0);
                float j0 = x - j1 - c;

                float ey0dt = Mathf.Exp(-y0 * dt);
                float ey1dt = Mathf.Exp(-y1 * dt);

                x = j0 * ey0dt + j1 * ey1dt + c;
                v = -y0 * j0 * ey0dt - y1 * j1 * ey1dt;
            }
        }

        // 阻尼比转刚度：由定义 r = d / (2 * sqrt(s)) 变形，sqrt(s) = d / (2 * r)，故 s = (d / (2 * r))^2
        public static float DampingRatioToStiffness(float ratio, float damping)
        {
            float root = damping / (ratio * 2f);
            return root * root;
        }

        // 阻尼比转阻尼：由定义 r = d / (2 * sqrt(s)) 变形，d = 2 * r * sqrt(s)
        public static float DampingRatioToDamping(float ratio, float stiffness)
        {
            return ratio * 2f * Mathf.Sqrt(stiffness);
        }

        public static void SpringDamperExactRatio(
            ref float x,
            ref float v,
            float xGoal,
            float vGoal,
            float dampingRatio,
            float halfLife,
            float dt,
            float eps = 1e-5f)
        {
            // 与 SpringDamperExact 数学主体完全相同，区别在于用阻尼比 dampingRatio 参数化弹性
            // 阻尼比 r = d / (2 * sqrt(s)) 是个无量纲量，直接描述“弹性程度”，比 stiffness、damping 更贴近直觉：
            //     r < 1 欠阻尼（弹，会振荡）
            //     r = 1 临界阻尼（最快收敛且不振荡）
            //     r > 1 过阻尼（黏，缓慢趋近）
            // 用户只需在一条“从不弹到很弹”的刻度上滑动 r 即可
            // 本版先由 halfLife 得 d，再由 r 与 d 反推 s（与 SpringDamperExactHalfLife 的顺序相反）
            //     d = HalfLifeToDamping(halfLife)
            //     s = DampingRatioToStiffness(dampingRatio, d) = (d / (2 * r))^2
            // 换算回 s、d 后，后续三分支逻辑一字不差（详见 SpringDamperExact 注释）

            float g = xGoal;
            float q = vGoal;
            float d = HalfLifeToDamping(halfLife);
            float s = DampingRatioToStiffness(dampingRatio, d);
            float c = g + d * q / (s + eps);
            float y = d / 2f;

            if (Mathf.Abs(s - d * d / 4f) < eps)
            {
                // 临界阻尼
                float j0 = x - c;
                float j1 = v + j0 * y;
                float eydt = Mathf.Exp(-y * dt);

                x = j0 * eydt + dt * j1 * eydt + c;
                v = -y * j0 * eydt - y * dt * j1 * eydt + j1 * eydt;
            }
            else if (s - d * d / 4f > 0f)
            {
                // 欠阻尼
                float w = Mathf.Sqrt(s - d * d / 4f);
                float j = Mathf.Sqrt((v + y * (x - c)) * (v + y * (x - c)) / (w * w + eps) + (x - c) * (x - c));
                float p = Mathf.Atan((v + (x - c) * y) / (-(x - c) * w + eps));

                j = (x - c) > 0f ? j : -j;

                float eydt = Mathf.Exp(-y * dt);

                x = j * eydt * Mathf.Cos(w * dt + p) + c;
                v = -y * j * eydt * Mathf.Cos(w * dt + p) - w * j * eydt * Mathf.Sin(w * dt + p);
            }
            else
            {
                // 过阻尼
                float y0 = (d + Mathf.Sqrt(d * d - 4f * s)) / 2f;
                float y1 = (d - Mathf.Sqrt(d * d - 4f * s)) / 2f;
                float j1 = (c * y0 - x * y0 - v) / (y1 - y0);
                float j0 = x - j1 - c;

                float ey0dt = Mathf.Exp(-y0 * dt);
                float ey1dt = Mathf.Exp(-y1 * dt);

                x = j0 * ey0dt + j1 * ey1dt + c;
                v = -y0 * j0 * ey0dt - y1 * j1 * ey1dt;
            }
        }

        // 临界弹簧阻尼器（最常用）：固定在临界阻尼情形，s = d^2 / 4 恒成立
        // 因此不必再判断欠阻尼/临界/过阻尼三种情况，直接套临界公式，可编译成极快的代码
        // 也因为临界约束 d^2 / 4 = s 让 s、d 不再独立，只需 halfLife 一个参数即可确定整个系统，
        // 表现为无额外振荡地尽快趋向目标
        public static void CriticalSpringDamperExact(
            ref float x,
            ref float v,
            float xGoal,
            float vGoal,
            float halfLife,
            float dt)
        {
            // c 的分母 s 用临界关系 s = d^2 / 4 代入：c = g + (d * q) / (d^2 / 4)
            // 速度公式做了化简：由 j1 = v + j0 * y 知 j1 - y * j0 = v，故 v = eydt * (v - j1 * y * dt)
            float g = xGoal;
            float q = vGoal;
            float d = HalfLifeToDamping(halfLife);
            float c = g + (d * q) / (d * d / 4f);
            float y = d / 2f;
            float j0 = x - c;
            float j1 = v + j0 * y;
            float eydt = Mathf.Exp(-y * dt);

            x = eydt * (j0 + j1 * dt) + c;
            v = eydt * (v - j1 * y * dt);
        }

        // 简化版：目标速度 vGoal 恒为 0（最常见的“平滑趋近并停下”场景）
        // 此时 c = g（因 q = 0），省去 c 的计算与 vGoal 参数
        public static void SimpleSpringDamperExact(
            ref float x,
            ref float v,
            float xGoal,
            float halfLife,
            float dt)
        {
            float y = HalfLifeToDamping(halfLife) / 2f;
            float j0 = x - xGoal;
            float j1 = v + j0 * y;
            float eydt = Mathf.Exp(-y * dt);

            x = eydt * (j0 + j1 * dt) + xGoal;
            v = eydt * (v - j1 * y * dt);
        }

        // 衰减版：目标位置与目标速度均为 0，让 x、v 平滑衰减到零
        // 此时 c = 0、j0 = x，连 xGoal 都省去，常用于惯性化等“把偏移量衰减掉”的场景
        public static void DecaySpringDamperExact(
            ref float x,
            ref float v,
            float halfLife,
            float dt)
        {
            float y = HalfLifeToDamping(halfLife) / 2f;
            float j1 = v + x * y;
            float eydt = Mathf.Exp(-y * dt);

            x = eydt * (x + j1 * dt);
            v = eydt * (v - j1 * y * dt);
        }
    }
}
