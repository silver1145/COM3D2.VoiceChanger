using COM3D2.VoiceChanger.Plugin.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading;
using static EventDelegate;
using static OVRLipSync;

namespace COM3D2.VoiceChanger.Plugin.Preloader
{
    public enum PreloaderType
    {
        NonePreloader,
        LinerPreloader,
        // GraphDBPreloader,
        // KagPreloader,
    }

    internal delegate List<Base_Data> PredictFunc();
    internal delegate void PreloadResultCallback(List<Base_Data> data);

    internal abstract class BasePreloader : IDisposable
    {
        public PreloaderType preloaderType { get; protected set; } = PreloaderType.NonePreloader;
        protected CacheHashSet<string> voiceWait;
        protected CacheHashSet<string> preloadHistory;
        protected CacheHashSet<Base_Data> preloadData;
        private HashSet<InferThread> inferThreads;
        protected PreloadResultCallback sendCallback;
        private readonly ReaderWriterLockSlim lockSlim;
        private Timer workTimer;
        private int cleanCountTimer;
        protected bool disposedValue;

        protected BasePreloader(int cacheSize, CacheHashSet<string> wait, PreloadResultCallback callback)
        {
            voiceWait = wait;
            preloadHistory = new CacheHashSet<string>(cacheSize);
            preloadData = new CacheHashSet<Base_Data>();
            inferThreads = new HashSet<InferThread>();
            sendCallback = callback;
            lockSlim = new ReaderWriterLockSlim();
            cleanCountTimer = 0;
            // can't use multi System.Threading.Timer
            workTimer = new Timer(state =>
            {
                
                SendData();
                cleanCountTimer++;
                if (cleanCountTimer >= 25)
                {
                    cleanCountTimer = 0;
                    CleanThreads();
                }
                
            }, null, 0, 200);
        }

        ~BasePreloader()
        {
            Dispose(false);
        }

        private void SendData()
        {
            if (preloadData.Any())
            {
                List<Base_Data> temp = new List<Base_Data>();
                foreach (Base_Data data in preloadData)
                {
                    temp.Add(data);
                    if ((data is Command_Data commandd) && commandd.name == "cancel")
                    {
                        preloadHistory.Clear();
                    }
                    else if (data is Voice_Data voiced)
                    {
                        preloadHistory.Add(voiced.name);
                        voiceWait.Add(voiced.name);
                    }
                }
                preloadData.Clear();
                sendCallback?.Invoke(temp);
            }
        }

        private bool CleanThreads(bool requireStop = false)
        {
            if (inferThreads.Any())
            {
                List<InferThread> threadsToClean = new List<InferThread>();
                foreach (InferThread thread in inferThreads)
                {
                    if (thread == null)
                    {
                        threadsToClean.Add(thread);
                        continue;
                    }
                    if (requireStop)
                    {
                        thread.Stop();
                    }
                    if (thread.Stopped)
                    {
                        threadsToClean.Add(thread);
                    }
                }
                foreach (InferThread thread in threadsToClean)
                {
                    inferThreads.Remove(thread);
                }
                if (inferThreads.Any())
                {
                    return false;
                }
            }
            return true;
        }

        public void Preload(string oggFilename)
        {
            Func<List<Base_Data>> predictf = () => Predict(oggFilename);
            InferThread ithread = new InferThread(() => predictf.Invoke(), AddData);
            inferThreads.Add(ithread);
            ithread.Start();
        }

        private void AddData(List<Base_Data> data)
        {
            List<Base_Data> temp = new List<Base_Data>();
            bool cancel = false;
            foreach (Base_Data d in data)
            {
                if ((d is Command_Data cd) && cd.name == "cancel")
                {
                    temp.Clear();
                    cancel = true;
                }
                temp.Add(d);
            }
            if (cancel)
            {
                // Ensure order of cancel
                lockSlim.EnterWriteLock();
                try
                {
                    preloadData.Clear();
                    foreach (Base_Data d in temp)
                    {
                        preloadData.Add(d);
                    }
                }
                finally
                {
                    lockSlim.ExitWriteLock();
                }
            }
            else
            {
                lockSlim.EnterReadLock();
                try
                {
                    foreach (Base_Data d in temp)
                    {
                        preloadData.Add(d);
                    }
                }
                finally
                {
                    lockSlim.ExitReadLock();
                }
            }
        }

        protected abstract List<Base_Data> Predict(string oggFilename);

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                while (!CleanThreads(true))
                {
                    Thread.Sleep(50);
                }
                if (disposing)
                {
                    lockSlim.Dispose();
                    workTimer.Dispose();
                }
                disposedValue = true;
            }
        }

        public abstract void Dispose();
    }

    internal class InferThread
    {
        static bool cancel;
        private Thread thread;
        private PredictFunc predictFunc;
        private PreloadResultCallback resultCallback;
        public bool Stopped => thread.ThreadState == ThreadState.Stopped;

        public InferThread(PredictFunc predict, PreloadResultCallback callback)
        {
            cancel = false;
            predictFunc = predict;
            resultCallback = callback;
        }

        public void Run()
        {
            List<Base_Data>? data = predictFunc?.Invoke();
            if (data != null && data.Any() && !cancel)
            {
                resultCallback?.Invoke(data);
            }
        }

        public void Start()
        {
            thread = new Thread(Run);
            thread.Start();
        }

        public void Stop()
        {
            cancel = true;
        }
    }
}
