using System;

namespace Kadaya
{
    static class CameraMotion
    {
        const float Tau = MathF.PI * 2;

        public static float MouseYawDelta(float dx, float scale) => -dx * scale * .0015f;
        public static float MousePitchDelta(float dy, float scale) => dy * scale * .00125f;

        public static float ApproachYaw(float current, float target, float dt, float rate)
        {
            float amount = 1 - MathF.Exp(-Math.Max(0, dt) * rate);
            return current + WrapAngle(target - current) * amount;
        }

        public static float WrapAngle(float angle) => MathF.IEEERemainder(angle, Tau);
    }
}

