using MULTI_Bet_playing_Demo.Services;

namespace MULTI_Bet_playing_Demo.Pages;

public partial class WebViewPage : ContentPage
{
    private readonly string _homeUrl;
    private readonly string _title;
    private bool _desktopUa;
    private bool _isNavigating;

    public WebViewPage(string url, string title = "Site")
    {
        InitializeComponent();
        _title = title;

        if (!UrlValidator.TryNormalize(url, out var safe, out var err))
        {
            TitleLabel.Text = "URL bloqueada";
            Title = "Erro";
            _homeUrl = string.Empty;
            MainWebView.Source = null;
            AddressEntry.Text = string.Empty;
            _ = DisplayAlertAsync("Bloqueado", err, "OK");
            return;
        }

        _homeUrl = safe;
        TitleLabel.Text = title;
        Title = title;
        SetCurrentUrl(safe);
        MainWebView.Source = safe;
    }

    protected override bool OnBackButtonPressed()
    {
        try
        {
            if (MainWebView.CanGoBack)
            {
                MainWebView.GoBack();
                return true;
            }
        }
        catch { }
        return base.OnBackButtonPressed();
    }

    private void OnBack(object? sender, EventArgs e)
    {
        try { if (MainWebView.CanGoBack) MainWebView.GoBack(); } catch { }
    }

    private void OnForward(object? sender, EventArgs e)
    {
        try { if (MainWebView.CanGoForward) MainWebView.GoForward(); } catch { }
    }

    private void OnReload(object? sender, EventArgs e)
    {
        try { MainWebView.Reload(); } catch { }
    }

    private void OnHome(object? sender, EventArgs e)
    {
        if (!string.IsNullOrEmpty(_homeUrl))
            NavigateTo(_homeUrl);
    }

    private async void OnClose(object? sender, EventArgs e)
    {
        try { await Navigation.PopAsync(); } catch { }
    }

    private void OnAddressCompleted(object? sender, EventArgs e) => NavigateFromAddressBar();

    private void OnGo(object? sender, EventArgs e) => NavigateFromAddressBar();

    private void NavigateFromAddressBar()
    {
        try
        {
            if (!UrlValidator.TryNormalize(AddressEntry.Text, out var safe, out var err))
            {
                _ = DisplayAlertAsync("URL bloqueada", err, "OK");
                return;
            }

            NavigateTo(safe);
            AddressEntry.Unfocus();
        }
        catch { }
    }

    private void NavigateTo(string url)
    {
        try
        {
            if (!UrlValidator.TryNormalize(url, out var safe, out _))
                return;

            SetCurrentUrl(safe);
            MainWebView.Source = safe;
        }
        catch { }
    }

    private void OnNavigating(object? sender, WebNavigatingEventArgs e)
    {
        try
        {
            _isNavigating = true;
            LoadingProgress.Progress = 0.15;
            LoadingProgress.IsVisible = true;
            if (!string.IsNullOrEmpty(e.Url))
                SetCurrentUrl(e.Url);
        }
        catch { }
    }

    private void OnNavigated(object? sender, WebNavigatedEventArgs e)
    {
        try
        {
            _isNavigating = false;
            LoadingProgress.Progress = 1;
            if (!string.IsNullOrEmpty(e.Url))
                SetCurrentUrl(e.Url);

            Dispatcher.StartTimer(TimeSpan.FromMilliseconds(180), () =>
            {
                LoadingProgress.IsVisible = false;
                LoadingProgress.Progress = 0;
                return false;
            });
        }
        catch { }
    }

    private void SetCurrentUrl(string url)
    {
        UrlLabel.Text = url;
        AddressEntry.Text = url;
    }

    private async void OnMenu(object? sender, EventArgs e)
    {
        try
        {
            var choice = await DisplayActionSheetAsync(
                "Navegador", "Cancelar", null,
                _isNavigating ? "Parar carregamento" : "Recarregar",
                "Abrir no Chrome / navegador do sistema",
                _desktopUa ? "Modo mobile (UA)" : "Modo desktop (UA)",
                "Copiar URL", "Compartilhar URL", "Limpar cookies deste app");

            var url = AddressEntry.Text ?? _homeUrl;

            switch (choice)
            {
                case "Parar carregamento":
                    try { MainWebView.Source = null; } catch { }
                    _isNavigating = false;
                    LoadingProgress.IsVisible = false;
                    break;
                case "Recarregar":
                    OnReload(sender, e);
                    break;
                case "Abrir no Chrome / navegador do sistema":
                    if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
                        await Launcher.Default.OpenAsync(uri);
                    break;
                case "Modo desktop (UA)":
                case "Modo mobile (UA)":
                    _desktopUa = !_desktopUa;
                    ApplyUserAgent(_desktopUa);
                    MainWebView.Reload();
                    break;
                case "Copiar URL":
                    await Clipboard.Default.SetTextAsync(url);
                    await DisplayAlertAsync("OK", "URL copiada.", "OK");
                    break;
                case "Compartilhar URL":
                    await Share.Default.RequestAsync(new ShareTextRequest { Text = url, Title = _title });
                    break;
                case "Limpar cookies deste app":
                    WebViewSecurity.ClearCookiesAndCache();
                    await DisplayAlertAsync("OK", "Cookies limpos. Recarregue e tente o login de novo.", "OK");
                    break;
            }
        }
        catch (Exception ex)
        {
            try { await DisplayAlertAsync("Erro", ex.Message, "OK"); } catch { }
        }
    }

    private void ApplyUserAgent(bool desktop)
    {
#if ANDROID
        try
        {
            if (MainWebView.Handler?.PlatformView is Android.Webkit.WebView aw)
            {
                aw.Settings.UserAgentString = desktop
                    ? WebViewSecurity.ChromeDesktopUa
                    : WebViewSecurity.ChromeMobileUa;
            }
        }
        catch { }
#endif
    }
}
