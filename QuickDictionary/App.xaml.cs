﻿using CefSharp;
using CefSharp.Wpf;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace QuickDictionary
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            ////Add Custom assembly resolver
            //AppDomain.CurrentDomain.AssemblyResolve += Resolver;

            ////Any CefSharp references have to be in another method with NonInlining
            //// attribute so the assembly rolver has time to do it's thing.
            //InitializeCefSharp();
            var settings = new CefSettings();

            settings.CachePath = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "data\\cache");
            settings.PersistUserPreferences = true;
            settings.PersistSessionCookies = true;

            Cef.Initialize(settings);
        }

        Mutex myMutex;
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            bool aIsNewInstance = false;
            myMutex = new Mutex(true, "QuickDictionary", out aIsNewInstance);
            if (!aIsNewInstance)
            {
                Current.Shutdown();
            }

            SetupExceptionHandling();
        }

        private void SetupExceptionHandling()
        {
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
                LogUnhandledException((Exception)e.ExceptionObject, "AppDomain.CurrentDomain.UnhandledException");

            DispatcherUnhandledException += (s, e) =>
            {
                LogUnhandledException(e.Exception, "Application.Current.DispatcherUnhandledException");
                e.Handled = true;
            };

            TaskScheduler.UnobservedTaskException += (s, e) =>
            {
                LogUnhandledException(e.Exception, "TaskScheduler.UnobservedTaskException");
                e.SetObserved();
            };
        }

        public static void LogUnhandledException(Exception exception, string source)
        {
            StringBuilder message = new StringBuilder();
            message.AppendLine($"[{DateTime.Now:R}] Unhandled exception ({source})");
            try
            {
                System.Reflection.AssemblyName assemblyName = System.Reflection.Assembly.GetExecutingAssembly().GetName();
                message.AppendLine($" in {assemblyName.Name} v{assemblyName.Version}");
            }
            catch (Exception ex)
            {
            }
            message.AppendLine(exception.Message);
            message.AppendLine(exception.StackTrace);
            message.AppendLine();
            message.AppendLine();
            File.AppendAllText(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "QuickDictionary\\log.txt"), message.ToString());
        }
    }
}
