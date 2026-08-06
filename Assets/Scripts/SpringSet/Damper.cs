using UnityEngine;
namespace SpringSet
{
    public static class Damper
    {
        public static float Lerp(float x, float y, float a)
        {
            return (1.0f - a) * x + a * y;
        }

        public static float DamperBase(float start, float target, float factor)
        {
            return Lerp(start, target, factor);
        }

        /// <summary>
        /// 指数阻尼器，以固定的帧时间 ft 计算阻尼结果，摆脱了阻尼结果对帧时间 dt 的依赖，保证了阻尼结果的稳定性
        /// </summary>
        /// <param name="x">当前值</param>
        /// <param name="g">目标值</param>
        /// <param name="damping">阻尼参数</param>
        /// <param name="dt">实际引擎帧时间</param>
        /// <param name="ft">预期固定阻尼帧时间</param>
        /// <returns>阻尼结果值</returns>
        public static float DamperExponential(
            float x,
            float g,
            float damping,
            float dt,
            float ft = 1.0f / 60.0f)
        {
            return Lerp(x, g, 1.0f - Mathf.Pow(1.0f / (1.0f - ft * damping), -dt / ft));

            // 推导式
            //     x[t + n] = lerp(x[t], g, 1 - (1 / y)^(-n))
            //     n = dt / ft, y = 1 - damping * ft
            // 推导式中的 y 是怎么来的
            // 考虑阻尼衰减时，真正在衰减的不是位置，而是位置到目标的距离，因此定义误差
            //     e[t] = x[t] - g
            // x[t] 的递推式为
            //     x[t+1] = lerp(x[t], g, damping * ft)
            //            = x[t] + damping * ft * (g - x[t])
            // 递推式两边同时减去 g
            //     x[t+1] - g = (x[t] - g) + damping * ft * (g - x[t])
            // 注意到 (g - x[t]) = -(x[t] - g) = -e[t]，代入
            //     e[t+1] = e[t] - damping * ft * e[t]
            //            = (1 - damping * ft) * e[t]
            // y = 1 - damping * ft 就此得出，y 将作为 a 参与 Lerp 计算
            // 为什么？
            // 因为 a 是 lerp 中朝目标覆盖的比例，而在误差的递推式中，e[t+n] = y^n * e[t]。因此在每帧插值后，离目标的距离会缩小到原来的 y^n
            // 而我们想用一次 lerp 就跳到 n 帧（固定帧时间 ft）后的结果，因此 a = 1 - y^n
            // x[t+n] = g + e[t+n]
            //     = g + y^n * e[t]
            //     = g + y^n * (x[t] - g)
            //     = lerp(x[t], g, 1 - y^n) -- 这一步回归到 lerp 形式
        }

        /// <summary>
        /// 精确阻尼器，以半衰期 halfLife 计算阻尼结果，摆脱了阻尼结果对帧时间 dt 的依赖，保证了阻尼结果的稳定性
        /// </summary>
        /// <param name="x">当前值</param>
        /// <param name="g">目标值</param>
        /// <param name="halfLife">半衰期</param>
        /// <param name="dt">实际引擎帧时间</param>
        /// <param name="eps"></param>
        /// <returns></returns>
        public static float DamperExact(
            float x,
            float g,
            float halfLife,
            float dt,
            float eps = 1e-5f)
        {
            // 效率更高的公式，用换底公式 2^k = e^(k * ln(2))
            // ln(2) = 0.69314718056
            return Lerp(x, g, 1.0f - Mathf.Exp(-(0.69314718056f * dt) / (halfLife + eps)));

            // 简单等价公式（作参考）
            // 将 y = 1 - damping * ft 固定为 0.5，表示每半衰期后误差减半
            // return Lerp(x, g, 1.0f - Mathf.Pow(2, -dt / (halfLife + eps)));
        }
    }
}
