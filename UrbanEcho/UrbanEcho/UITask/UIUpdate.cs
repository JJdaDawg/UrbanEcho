using Avalonia.Threading;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UrbanEcho.Events.UI;

namespace UrbanEcho.UI
{
    public static class UIUpdate
    {
        public static CancellationTokenSource Cts = new CancellationTokenSource();
        public static Task? UITask;

        public static int SleepTime = 16;//update every 16ms

        public static void Run()
        {
            while (Cts.IsCancellationRequested == false)
            {
                if (!EventQueueForUI.Instance.IsEmpty())
                {
                    if (!EventQueueForUI.Instance.IsEmpty())
                        Dispatcher.UIThread.Post(() =>
                        {
                            while (!EventQueueForUI.Instance.IsEmpty())
                            {
                                EventQueueForUI.Instance.Read()?.Run();
                            }
                        });
                }

                Thread.Sleep(SleepTime);//update every 16ms
            }
        }
    }
}