﻿using CefSharp;
using CefSharp.Wpf;
using Squirrel;
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
            try
            {
                using (var mgr = UpdateManager.GitHubUpdateManager("https://github.com/Henry-YSLin/QuickDictionary").Result)
                {
                    // Note, in most of these scenarios, the app exits after this method
                    // completes!
                    SquirrelAwareApp.HandleEvents(
                      onInitialInstall: v => mgr.CreateShortcutForThisExe(),
                      onAppUpdate: v => mgr.CreateShortcutForThisExe(),
                      onAppUninstall: v =>
                      {
                          mgr.RemoveShortcutForThisExe();
                          mgr.RemoveUninstallerRegistryEntry();
                      },
                      onFirstRun: () => { });
                }
            }
            catch (Exception)
            {

            }

            var settings = new CefSettings();

            settings.CachePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "QuickDictionary\\cache");
            settings.PersistUserPreferences = true;
            settings.PersistSessionCookies = true;
            settings.UserAgent = "Mozilla/5.0 (Linux; Android 10) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/87.0.4280.101 Mobile Safari/537.36";

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
                LogException((Exception)e.ExceptionObject, "AppDomain.CurrentDomain.UnhandledException");

            DispatcherUnhandledException += (s, e) =>
            {
                LogException(e.Exception, "Application.Current.DispatcherUnhandledException");
                e.Handled = true;
            };

            TaskScheduler.UnobservedTaskException += (s, e) =>
            {
                LogException(e.Exception, "TaskScheduler.UnobservedTaskException");
                e.SetObserved();
            };
        }

        public static void LogException(Exception exception, string source)
        {
            StringBuilder message = new StringBuilder();
            message.AppendLine($"[{DateTime.Now:R}] Exception ({source})");
            try
            {
                System.Reflection.AssemblyName assemblyName = System.Reflection.Assembly.GetExecutingAssembly().GetName();
                message.AppendLine($" in {assemblyName.Name} v{assemblyName.Version}");
                message.AppendLine(exception.Message);
                message.AppendLine(exception.StackTrace);
                message.AppendLine();
                message.AppendLine();
                File.AppendAllText(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "QuickDictionary\\log.txt"), message.ToString());
                if (exception.InnerException!= null)
                {
                    LogException(exception.InnerException, source + ".InnerException");
                }
            }
            catch (Exception)
            {
            }
        }

        private void Application_Exit(object sender, ExitEventArgs e)
        {
            WordListManager.CommitDeletions();
        }
    }
}
