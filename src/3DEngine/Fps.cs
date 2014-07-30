using System;

namespace _3DEngine.Window
{
    public class Fps
    {
        private DateTime previousDate;
        private TimeSpan ellapsedTime;
        private double accumulatedFps;

        private int tickCounter;

        public double GetFps()
        {
            var now = DateTime.Now;
            var currentFps = 1000.0 / (now - previousDate).TotalMilliseconds;
            accumulatedFps += currentFps;
            ellapsedTime += now - previousDate;
            previousDate = now;
            tickCounter++;

            if (ellapsedTime > TimeSpan.FromSeconds(1))
            {
                m_fps = accumulatedFps/tickCounter;
                ellapsedTime = TimeSpan.Zero;
                accumulatedFps = 0;
                tickCounter = 0;
            }

            return m_fps;
        }

        public override string ToString()
        {
            return string.Format("{0:0.00} fps", GetFps());
        }

        private double m_fps;
    }
}