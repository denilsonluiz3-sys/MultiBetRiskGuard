namespace MULTI_Bet_playing_Demo.Services;

/// <summary>
/// Hardened WebView configuration for the in-app browser.
/// Security is defense-in-depth; remote sites remain untrusted content.
/// </summary>
public static class WebViewSecurity
{
    public const string ChromeMobileUa =
        "Mozilla/5.0 (Linux; Android 13; Pixel 7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/121.0.0.0 Mobile Safari/537.36";
    public const string ChromeDesktopUa =
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/121.0.0.0 Safari/537.36";

    public static void ConfigureHandlers()
    {
#if ANDROID
        Microsoft.Maui.Handlers.WebViewHandler.Mapper.AppendToMapping("MultiBetBrowser", (handler, view) =>
        {
            try
            {
                var wv = handler.PlatformView;
                if (wv?.Settings == null) return;
                var s = wv.Settings;

                s.JavaScriptEnabled = true;
                s.DomStorageEnabled = true;
                s.DatabaseEnabled = false;
                s.LoadsImagesAutomatically = true;
                s.MediaPlaybackRequiresUserGesture = true;

                s.SetSupportMultipleWindows(false);
                s.JavaScriptCanOpenWindowsAutomatically = false;
                s.SetSupportZoom(true);
                s.BuiltInZoomControls = true;
                s.DisplayZoomControls = false;
                s.UseWideViewPort = true;
                s.LoadWithOverviewMode = true;

                // Never allow local file/content origins to reach arbitrary files.
                s.AllowFileAccess = false;
                s.AllowContentAccess = false;
                s.AllowFileAccessFromFileURLs = false;
                s.AllowUniversalAccessFromFileURLs = false;

                if (string.IsNullOrEmpty(s.UserAgentString) || s.UserAgentString.Contains("; wv)"))
                    s.UserAgentString = ChromeMobileUa;

                var cm = Android.Webkit.CookieManager.Instance;
                cm?.SetAcceptCookie(true);
                cm?.SetAcceptThirdPartyCookies(wv, false);

                wv.SetWebChromeClient(new MultiBetChromeClient());
                wv.SetDownloadListener(new SafeDownloadListener());
            }
            catch { }
        });
#endif
    }

    public static void ClearCookiesAndCache()
    {
#if ANDROID
        try
        {
            Android.Webkit.CookieManager.Instance?.RemoveAllCookies(null);
            Android.Webkit.CookieManager.Instance?.Flush();
            Android.Webkit.WebStorage.Instance?.DeleteAllData();
        }
        catch { }
#endif
    }

#if ANDROID
    sealed class MultiBetChromeClient : Android.Webkit.WebChromeClient
    {
        public override bool OnCreateWindow(Android.Webkit.WebView? view, bool isDialog, bool isUserGesture, Android.OS.Message? resultMsg)
        {
            try
            {
                var extra = view?.GetHitTestResult()?.Extra;
                if (!string.IsNullOrEmpty(extra) && UrlValidator.TryNormalize(extra, out var safe, out _))
                {
                    var intent = new Android.Content.Intent(Android.Content.Intent.ActionView, Android.Net.Uri.Parse(safe));
                    intent.AddFlags(Android.Content.ActivityFlags.NewTask);
                    Android.App.Application.Context.StartActivity(intent);
                    return true;
                }
            }
            catch { }
            return false;
        }

        public override void OnPermissionRequest(Android.Webkit.PermissionRequest? request)
        {
            try { request?.Deny(); } catch { }
        }

        public override bool OnGeolocationPermissionsShowPrompt(string? origin, Android.Webkit.GeolocationPermissions.ICallback? callback)
        {
            try { callback?.Invoke(origin, false, false); } catch { }
            return true;
        }
    }

    sealed class SafeDownloadListener : Java.Lang.Object, Android.Webkit.IDownloadListener
    {
        public void OnDownloadStart(string? url, string? userAgent, string? contentDisposition, string? mimetype, long contentLength)
        {
            // Downloads are deliberately not delegated automatically. This avoids
            // silently handing untrusted URLs to external download handlers.
            // A future download flow can add explicit user confirmation and type checks.
        }
    }
#endif
}
