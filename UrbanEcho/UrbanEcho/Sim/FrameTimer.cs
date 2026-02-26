using System;
using System.Diagnostics;
using System.Linq;

namespace UrbanEcho.Sim
{
    public class FrameTimer
    {
        private double timeToSleep = 0;

        private Stopwatch fpsTimer;

        private double targetMs = 16.6667f;
        private double actualMs = 0.0f;

        private const double second = 1000;
        private const double targetFrameTime = 16.6666667f;

        private int frames = 0;
        private string timeToSend = "";
        private Stopwatch stopwatch;
        private bool addText = false;

        private bool showingFPS = false;

        public double ElaspedSecondsSinceLastFrame = 0;

        public FrameTimer(bool showingFPS)
        {
            fpsTimer = new Stopwatch();
            fpsTimer.Start();
            stopwatch = new Stopwatch();
            stopwatch.Start();

            this.showingFPS = showingFPS;
        }

        //Called each frame
        public void Update()
        {
            frames++;

            if (stopwatch.ElapsedMilliseconds >= second)
            {
                if (showingFPS)
                {
                    addText = true;
                    timeToSend = $"last sleep time {timeToSleep.ToString("F2")} and {frames} frames in last second";
                }
                frames = 0;
                stopwatch.Restart();
            }

            actualMs += fpsTimer.ElapsedTicks * second / Stopwatch.Frequency;
            ElaspedSecondsSinceLastFrame = (double)fpsTimer.ElapsedTicks / (double)Stopwatch.Frequency;
            targetMs += targetFrameTime;
            //So Computer doesn't use 100% CPU and we update 60 times a second
            //16.77ms is 1/(60Hz) so if time since last scan is less than that sleep for bit
            timeToSleep = targetMs - actualMs;

            //Prevent catching up over many frames

            if (timeToSleep < -second * 0.25f)
            {
                actualMs = targetMs;
                targetMs += targetFrameTime;
            }
            fpsTimer.Restart();
        }

        public int GetTimeToSleep()
        {
            return (int)timeToSleep;
        }

        public bool ShouldShowText()
        {
            return addText;
        }

        public void ResetShowText()
        {
            addText = false;
        }

        public string TimeToShow()
        {
            return timeToSend;
        }
    }
}