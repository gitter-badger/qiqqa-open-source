﻿using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using Utilities.Shutdownable;

namespace Utilities.Maintainable
{
    public class MaintainableManager
    {
        public static MaintainableManager Instance = new MaintainableManager();
        public delegate void DoMaintenanceDelegate(Daemon daemon);

        class DoMaintenanceDelegateWrapper
        {
            internal string maintainable_description;
            internal WeakReference target;
            internal MethodInfo method_info;

            internal int delay_before_start_milliseconds;
            internal Daemon daemon;
        }

        List<DoMaintenanceDelegateWrapper> do_maintenance_delegate_wrappers = new List<DoMaintenanceDelegateWrapper>();

        MaintainableManager()
        {
            Logging.Info("Creating MaintainableManager");
            ShutdownableManager.Instance.Register(Shutdown);
        }

        public void Shutdown()
        {
            Logging.Info("Stopping MaintainableManager");

            foreach (DoMaintenanceDelegateWrapper do_maintenance_delegate_wrapper in do_maintenance_delegate_wrappers)
            {
                do_maintenance_delegate_wrapper.daemon.Stop();
            }

            foreach (DoMaintenanceDelegateWrapper do_maintenance_delegate_wrapper in do_maintenance_delegate_wrappers)
            {
                while (!do_maintenance_delegate_wrapper.daemon.Join(1000))
                {
                    Logging.Info("Waiting for Maintainable {0} to terminate.", do_maintenance_delegate_wrapper.maintainable_description);
                }
            }
        }

        public Daemon Register(DoMaintenanceDelegate do_maintenance_delegate, int delay_before_start_milliseconds, ThreadPriority thread_priority)
        {
            lock (do_maintenance_delegate_wrappers)
            {
                // Set up the wrapper
                DoMaintenanceDelegateWrapper do_maintenance_delegate_wrapper = new DoMaintenanceDelegateWrapper();
                do_maintenance_delegate_wrapper.maintainable_description = String.Format("{0}:{1}", do_maintenance_delegate.Target, do_maintenance_delegate.Method.Name);
                do_maintenance_delegate_wrapper.target = new WeakReference(do_maintenance_delegate.Target);
                do_maintenance_delegate_wrapper.method_info = do_maintenance_delegate.Method;
                do_maintenance_delegate_wrapper.delay_before_start_milliseconds = delay_before_start_milliseconds;
                do_maintenance_delegate_wrapper.daemon = new Daemon("Maintainable:" + do_maintenance_delegate.Target.GetType().Name + "." + do_maintenance_delegate.Method.Name);
                
                // Add it to our list of trackers
                do_maintenance_delegate_wrappers.Add(do_maintenance_delegate_wrapper);

                // Start the thread
                do_maintenance_delegate_wrapper.daemon.Start(DaemonThreadEntryPoint, do_maintenance_delegate_wrapper);
                do_maintenance_delegate_wrapper.daemon.Priority = thread_priority;

                return do_maintenance_delegate_wrapper.daemon;
            }
        }

        void DaemonThreadEntryPoint(object wrapper)
        {
            DoMaintenanceDelegateWrapper do_maintenance_delegate_wrapper = (DoMaintenanceDelegateWrapper)wrapper;

            if (0 != do_maintenance_delegate_wrapper.delay_before_start_milliseconds)
            {
                Logging.Info("+MaintainableManager is waiting some startup time for {0}", do_maintenance_delegate_wrapper.maintainable_description);
                do_maintenance_delegate_wrapper.daemon.Sleep(do_maintenance_delegate_wrapper.delay_before_start_milliseconds);
                Logging.Info("-MaintainableManager is waiting some startup time for {0}", do_maintenance_delegate_wrapper.maintainable_description);
            }

            while (do_maintenance_delegate_wrapper.daemon.StillRunning)
            {
                try
                {
                    object target = do_maintenance_delegate_wrapper.target.Target;
                    if (null != target)
                    {
                        do_maintenance_delegate_wrapper.method_info.Invoke(target, new object[] { do_maintenance_delegate_wrapper.daemon } );
                        target = null;
                    }
                    else
                    {
                        Logging.Info("Target maintainable ({0}) has been garbage collected, so closing down Maintainable thread.", do_maintenance_delegate_wrapper.maintainable_description);
                        do_maintenance_delegate_wrapper.daemon.Stop();
                    }
                }

                catch (Exception ex)
                {
                    Logging.Error(ex, "Maintainable {0} has thrown an unhandled exception.", do_maintenance_delegate_wrapper.maintainable_description);
                }

                if (do_maintenance_delegate_wrapper.daemon.StillRunning)
                {
                    do_maintenance_delegate_wrapper.daemon.Sleep();
                }
            }
        }
    }
}
