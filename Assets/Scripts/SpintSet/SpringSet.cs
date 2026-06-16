namespace DefaultNamespace
{
    public static class SpringSet
    {
        public static float Lerp(float x, float y, float a)
        {
            return (1.0f - a) * x + a * y;
        }

        public static float DamperCalc(float start, float target, float factor)
        {
            return Lerp(start, target, factor);
        }
    }
}
