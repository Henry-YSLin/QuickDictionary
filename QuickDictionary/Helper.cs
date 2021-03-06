﻿using CefSharp;
using CefSharp.Wpf;
using MoreLinq;
using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace QuickDictionary
{
    public static class Helper
    {
        public static void HideBoundingBox(object root)
        {
            Control control = root as Control;
            if (control != null)
                control.FocusVisualStyle = null;

            if (root is DependencyObject)
            {
                foreach (object child in LogicalTreeHelper.GetChildren((DependencyObject)root))
                {
                    HideBoundingBox(child);
                }
            }
        }

        public static string ToJSLiteral(this string input)
        {
            using (var writer = new StringWriter())
            {
                using (var provider = CodeDomProvider.CreateProvider("JScript"))
                {
                    provider.GenerateCodeFromExpression(new CodePrimitiveExpression(input), writer, null);
                    string tmp = writer.ToString();
                    return writer.ToString();
                }
            }
        }

        public static async Task<string> GetInnerTextByXPath(this ChromiumWebBrowser browser, params string[] xpath)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(@"(function() { return ");
            for (int i = 0; i < xpath.Length; i++)
            {
                sb.Append(@"document.evaluate(" + xpath[i].ToJSLiteral() + @", document, null, XPathResult.FIRST_ORDERED_NODE_TYPE, null).singleNodeValue.innerText");
                if (i != xpath.Length - 1)
                {
                    sb.Append(@" + '\r\n' + ");
                }
            }
            sb.Append(@";})();");
            var result = await browser.EvaluateScriptAsync(sb.ToString());
            if (result.Success)
            {
                return result.Result as string;
            }
            else
            {
                return null;
            }
        }

        public static void Show(this UIElement element)
        {
            element.Visibility = Visibility.Visible;
        }

        public static void Hide(this UIElement element, bool collapse = true)
        {
            element.Visibility = collapse ? Visibility.Collapsed : Visibility.Hidden;
        }

        /// <summary>
        /// Blocks while condition is true or timeout occurs.
        /// </summary>
        /// <param name="condition">The condition that will perpetuate the block.</param>
        /// <param name="frequency">The frequency at which the condition will be check, in milliseconds.</param>
        /// <param name="timeout">Timeout in milliseconds.</param>
        /// <exception cref="TimeoutException"></exception>
        /// <returns></returns>
        public static async Task WaitWhile(Func<bool> condition, int frequency = 25, int timeout = -1)
        {
            var waitTask = Task.Run(async () =>
            {
                while (condition()) await Task.Delay(frequency);
            });

            if (waitTask != await Task.WhenAny(waitTask, Task.Delay(timeout)))
                return;
        }

        /// <summary>
        /// Blocks until condition is true or timeout occurs.
        /// </summary>
        /// <param name="condition">The break condition.</param>
        /// <param name="frequency">The frequency at which the condition will be checked.</param>
        /// <param name="timeout">The timeout in milliseconds.</param>
        /// <returns></returns>
        public static async Task WaitUntil(Func<bool> condition, int frequency = 25, int timeout = -1)
        {
            var waitTask = Task.Run(async () =>
            {
                while (!condition()) await Task.Delay(frequency);
            });

            if (waitTask != await Task.WhenAny(waitTask,
                    Task.Delay(timeout)))
                return;
        }
        public static Bitmap cropAtRect(this Bitmap b, Rectangle r)
        {
            Bitmap nb = new Bitmap(r.Width, r.Height);
            using (Graphics g = Graphics.FromImage(nb))
            {
                g.DrawImage(b, -r.X, -r.Y);
                return nb;
            }
        }

        public static System.Windows.Point RealPixelsToWpf(Window w, System.Windows.Point p)
        {
            var t = PresentationSource.FromVisual(w).CompositionTarget.TransformFromDevice;
            return t.Transform(p);
        }

        public static System.Windows.Point WpfToRealPixels(Window w, System.Windows.Point p)
        {
            var t = PresentationSource.FromVisual(w).CompositionTarget.TransformToDevice;
            return t.Transform(p);
        }

        public static double DistanceToPoint(this RectangleF rect, double x, double y)
        {
            return Math.Sqrt(Math.Pow(Math.Max(0, Math.Abs(rect.X + rect.Width / 2 - x) - rect.Width / 2), 2) + Math.Pow(Math.Max(0, Math.Abs(rect.Y + rect.Height / 2 - y) - rect.Height / 2), 2));
        }

        public static async Task<string> GetFinalRedirectAsync(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return url;

            int maxRedirCount = 8;  // prevent infinite loops
            string newUrl = url;
            do
            {
                HttpWebRequest req = null;
                HttpWebResponse resp = null;
                try
                {
                    req = (HttpWebRequest)HttpWebRequest.Create(url);
                    req.Method = "HEAD";
                    req.AllowAutoRedirect = false;
                    resp = (HttpWebResponse)await req.GetResponseAsync();
                    switch (resp.StatusCode)
                    {
                        case HttpStatusCode.OK:
                            return newUrl;
                        case HttpStatusCode.Redirect:
                        case HttpStatusCode.MovedPermanently:
                        case HttpStatusCode.RedirectKeepVerb:
                        case HttpStatusCode.RedirectMethod:
                            newUrl = resp.Headers["Location"];
                            if (newUrl == null)
                                return url;

                            if (newUrl.IndexOf("://", System.StringComparison.Ordinal) == -1)
                            {
                                // Doesn't have a URL Schema, meaning it's a relative or absolute URL
                                Uri u = new Uri(new Uri(url), newUrl);
                                newUrl = u.ToString();
                            }
                            break;
                        default:
                            return newUrl;
                    }
                    url = newUrl;
                }
                catch (WebException)
                {
                    // Return the last known good URL
                    return newUrl;
                }
                catch (Exception)
                {
                    return null;
                }
                finally
                {
                    if (resp != null)
                        resp.Close();
                }
            } while (maxRedirCount-- > 0);

            return newUrl;
        }

        public static async Task<HttpStatusCode> GetFinalStatusCodeAsync(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return HttpStatusCode.NotFound;

            int maxRedirCount = 8;  // prevent infinite loops
            string newUrl = url;
            HttpStatusCode statusCode = HttpStatusCode.NotFound;
            do
            {
                HttpWebRequest req = null;
                HttpWebResponse resp = null;
                try
                {
                    req = (HttpWebRequest)HttpWebRequest.Create(url);
                    req.Method = "HEAD";
                    req.AllowAutoRedirect = false;
                    resp = (HttpWebResponse)await req.GetResponseAsync();
                    statusCode = resp.StatusCode;
                    switch (resp.StatusCode)
                    {
                        case HttpStatusCode.OK:
                            return statusCode;
                        case HttpStatusCode.Redirect:
                        case HttpStatusCode.MovedPermanently:
                        case HttpStatusCode.RedirectKeepVerb:
                        case HttpStatusCode.RedirectMethod:
                            newUrl = resp.Headers["Location"];
                            if (newUrl == null)
                                return statusCode;

                            if (newUrl.IndexOf("://", System.StringComparison.Ordinal) == -1)
                            {
                                // Doesn't have a URL Schema, meaning it's a relative or absolute URL
                                Uri u = new Uri(new Uri(url), newUrl);
                                newUrl = u.ToString();
                            }
                            break;
                        default:
                            return statusCode;
                    }
                    url = newUrl;
                }
                catch (WebException)
                {
                    // Return the last known good URL
                    return statusCode;
                }
                catch (Exception)
                {
                    return statusCode;
                }
                finally
                {
                    if (resp != null)
                        resp.Close();
                }
            } while (maxRedirCount-- > 0);

            return statusCode;
        }
    }

    public class NotifyPropertyChanged : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void NotifyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected void NotifyChanged(IEnumerable<string> propertyName)
        {
            propertyName.ForEach(x => NotifyChanged(x));
        }

        protected void SetAndNotify<T>(ref T variable, T value, IEnumerable<string> calculatedProperties = null, [CallerMemberName] string memberName = null)
        {
            variable = value;
            NotifyChanged(memberName);
            if (calculatedProperties != null) NotifyChanged(calculatedProperties);
        }
    }
}
