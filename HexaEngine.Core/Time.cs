﻿namespace HexaEngine.Core
{
    using HexaEngine.Core.Debugging;
    using System;
    using System.Diagnostics;

    public static class Time
    {
        private static long last;
        private static float fixedTime;
        private static float cumulativeFrameTime;

        // Properties
        public static float Delta { get; private set; }

        public static float CumulativeFrameTime { get => cumulativeFrameTime; }

        public static int FixedUpdateRate { get; set; } = 3;

        public static float FixedUpdatePerSecond => FixedUpdateRate / 1000F;

        public static event EventHandler? FixedUpdate;

        // Public Methods
        public static void Initialize()
        {
            last = Stopwatch.GetTimestamp();
            fixedTime = 0;
            cumulativeFrameTime = 0;
        }

        public static void FrameUpdate()
        {
            long now = Stopwatch.GetTimestamp();
            double deltaTime = (double)(now - last) / Stopwatch.Frequency;
            last = now;

            Delta = (float)deltaTime;
            cumulativeFrameTime += Delta;
            fixedTime += Delta;

            if (deltaTime == 0 || deltaTime < 0)
            {
                throw new InvalidOperationException();
            }

            while (fixedTime > FixedUpdatePerSecond)
            {
                fixedTime -= FixedUpdatePerSecond;
                FixedUpdate?.Invoke(null, EventArgs.Empty);
            }
        }
    }
}