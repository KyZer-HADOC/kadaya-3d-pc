using System;

namespace Kadaya.Tests
{
    static class Program
    {
        static void Main()
        {
            float yawDelta = CameraMotion.MouseYawDelta(100, 1);
            if (Math.Abs(yawDelta + .15f) > .0001f) throw new Exception("Mouse yaw sensitivity changed unexpectedly.");

            float before = 3.13f;
            float after = CameraMotion.ApproachYaw(before, -3.13f, .016f, 2.4f);
            if (Math.Abs(CameraMotion.WrapAngle(-3.13f - after)) >= Math.Abs(CameraMotion.WrapAngle(-3.13f - before)))
                throw new Exception("Automatic camera follow did not take the shortest path across the angle boundary.");

            if (CameraMotion.ApproachYaw(before, -3.13f, 0, 2.4f) != before)
                throw new Exception("Camera follow must remain stable when no frame time has elapsed.");

            Console.WriteLine("Camera motion checks passed.");
        }
    }
}

