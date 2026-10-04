Imports System
Imports System.Collections.Generic
Imports System.Collections.ObjectModel
Imports System.ComponentModel
Imports System.Globalization
Imports System.Linq
Imports System.Threading.Tasks
Imports System.Windows
Imports System.Windows.Controls
Imports System.Windows.Input
Imports System.Windows.Media
Imports System.Windows.Media.Animation
Imports System.Windows.Threading
Imports Microsoft.Web.WebView2.Core
Imports Microsoft.Web.WebView2.Wpf

Namespace NovaBrowser
    Partial Public Class MainWindow
        Inherits Window

        Private ReadOnly _state As AppState
        Private ReadOnly _tabs As New ObservableCollection(Of BrowserTab)()
        Private ReadOnly _closedTabs As New Stack(Of SessionEntry)()
        Private ReadOnly _downloads As New Dictionary(Of CoreWebView2DownloadOperation, BrowserTab)()
        Private ReadOnly _clock As New DispatcherTimer With {.Interval = TimeSpan.FromSeconds(20)}
        Private ReadOnly _saveTimer As New DispatcherTimer With {.Interval = TimeSpan.FromMilliseconds(650)}
        Private ReadOnly _toastTimer As New DispatcherTimer With {.Interval = TimeSpan.FromSeconds(5)}
        Private _environment As Task(Of CoreWebView2Environment)
        Private _privateProfile As String = "Private-" & Guid.NewGuid().ToString("N")
        Private _active As BrowserTab
        Private _restoring As Boolean = True
        Private _settingsReady As Boolean
        Private _isClosing As Boolean
        Private _allowClose As Boolean
        Private _fatalShutdown As Boolean
        Private _sidebarVisible As Boolean = True
        Private _drawerMode As String = "settings"
        Private _confirmCompletion As TaskCompletionSource(Of Boolean)

        Public Sub New()
            _state = StateStore.Load()
            ThemeManager.Apply(Application.Current.Resources, _state.Settings)
            InitializeComponent()
            TabList.ItemsSource = _tabs
            InitializePreferences()
            InitializeWorkbench()
            AddHandler _clock.Tick, Sub(s, e) UpdateClock()
            AddHandler _saveTimer.Tick,
                Sub(s, e)
                    _saveTimer.Stop()
                    PersistState()
                End Sub
            AddHandler _toastTimer.Tick,
                Sub(s, e)
                    _toastTimer.Stop()
                    ToastCard.Visibility = Visibility.Collapsed
                End Sub
            AddHandler Me.IsVisibleChanged, Sub(s, e) ConfigureAmbientMotion()
        End Sub

        Private Sub Window_Loaded(sender As Object, e As RoutedEventArgs)
            UpdateHome()
            UpdateClock()
            _clock.Start()
            If _state.Settings.StartMaximized Then WindowState = WindowState.Maximized
            UpdateWorkbenchLayout()
            If _state.Settings.SetupCompleted Then
                StartBrowsingSession()
            Else
                ShowSetup()
            End If
            If StateStore.LoadWarning.Length > 0 Then ShowToast(StateStore.LoadWarning)
        End Sub

        Private Sub StartBrowsingSession()
            If _sessionInitialized Then Return
            _sessionInitialized = True
            Dim restore = _state.Settings.RestoreSession
            If restore AndAlso Not _state.LastExitClean AndAlso _state.Session.Count > 0 Then
                restore = MessageBox.Show(Me, "NOVA ble ikke avsluttet normalt. Gjenopprette de vanlige fanene? Velg Nei for en ren start. Private faner gjenopprettes aldri.", "Gjenopprett faner", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) = MessageBoxResult.Yes
            End If
            If restore Then
                For Each saved In _state.Session.ToArray()
                    Dim browserTab = CreateTab(saved.Url, False, False)
                    If browserTab IsNot Nothing Then
                        browserTab.Title = UrlPolicy.CleanTitle(saved.Title)
                        browserTab.IsPinned = saved.IsPinned
                        browserTab.IsMuted = saved.IsMuted
                        browserTab.Zoom = saved.Zoom
                    End If
                Next
            End If
            If _tabs.Count = 0 Then CreateTab(UrlPolicy.HomeUrl, False, False)
            _state.LastExitClean = False
            _restoring = False
            TabList.SelectedIndex = Math.Clamp(_state.ActiveSessionIndex, 0, _tabs.Count - 1)
            UpdateHome()
            UpdateClock()
            PersistState()
        End Sub

        Private Function CreateTab(Optional address As String = UrlPolicy.HomeUrl, Optional isPrivate As Boolean = False, Optional selectTab As Boolean = True) As BrowserTab
            If _isClosing Then Return Nothing
            If _tabs.Count >= 50 Then
                ShowToast("Du har 50 faner åpne. Lukk en fane før du åpner flere.")
                Return Nothing
            End If
            Dim browserTab As New BrowserTab With {
                .Address = address, .IsHome = address = UrlPolicy.HomeUrl,
                .IsPrivate = isPrivate, .Zoom = _state.Settings.DefaultZoom,
                .Title = If(address = UrlPolicy.HomeUrl, "Ny fane", UrlPolicy.DisplayHost(address))}
            _tabs.Add(browserTab)
            browserTab.NotifyLocation()
            TabCountText.Text = _tabs.Count.ToString("00")
            If selectTab Then TabList.SelectedItem = browserTab
            ScheduleSave()
            Return browserTab
        End Function

        Private Async Sub TabList_SelectionChanged(sender As Object, e As SelectionChangedEventArgs)
            If _restoring OrElse _isClosing Then Return
            CloseAddressSuggestions()
            If _active IsNot Nothing Then _active.LastActiveUtc = DateTimeOffset.UtcNow
            _active = TryCast(TabList.SelectedItem, BrowserTab)
            WakeTab(_active)
            UpdateViewVisibility()
            UpdateChrome(True)
            If _active IsNot Nothing Then
                TabList.ScrollIntoView(_active)
                If _active.IsHome Then AnimateEntrance(HomeContent, 10)
            End If
            ScheduleSave()
            Dim browserTab = _active
            If browserTab IsNot Nothing AndAlso Not browserTab.IsHome AndAlso browserTab.View Is Nothing Then
                Await NavigateAsync(browserTab, browserTab.Address)
            End If
        End Sub

        Private Sub UpdateViewVisibility()
            For Each browserTab In _tabs
                If browserTab.View IsNot Nothing Then
                    browserTab.View.Visibility = If(browserTab Is _active AndAlso Not browserTab.IsHome AndAlso browserTab.ErrorMessage.Length = 0, Visibility.Visible, Visibility.Hidden)
                End If
            Next
            HomeScreen.Visibility = If(_active Is Nothing OrElse _active.IsHome, Visibility.Visible, Visibility.Collapsed)
            ErrorPanel.Visibility = If(_active IsNot Nothing AndAlso _active.ErrorMessage.Length > 0, Visibility.Visible, Visibility.Collapsed)
            If _active IsNot Nothing Then ErrorText.Text = _active.ErrorMessage
            If HomeScreen.Visibility = Visibility.Visible Then
                GreetingText.Text = If(_active IsNot Nothing AndAlso _active.IsPrivate, "Et privat pusterom.", If(_state.Settings.DisplayName.Length = 0, "Hei der.", "Hei, " & _state.Settings.DisplayName & "."))
            End If
            ConfigureAmbientMotion()
        End Sub

        Private Sub UpdateChrome(Optional replaceAddress As Boolean = False)
            If _active Is Nothing Then Return
            Dim core = _active.View?.CoreWebView2
            BackButton.IsEnabled = core IsNot Nothing AndAlso core.CanGoBack
            ForwardButton.IsEnabled = core IsNot Nothing AndAlso core.CanGoForward
            ReloadIcon.Kind = If(_active.IsLoading, "Stop", "Refresh")
            LoadingBar.IsIndeterminate = MotionEnabled
            LoadingBar.Value = 100
            LoadingBar.Visibility = If(_active.IsLoading, Visibility.Visible, Visibility.Collapsed)
            If replaceAddress OrElse Not AddressBox.IsKeyboardFocusWithin Then
                _updatingAddress = True
                AddressBox.Text = _active.Address
                _updatingAddress = False
            End If
            Dim isHttps = _active.Address.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
            ' HTTPS only describes transport; it is not a verdict that a website is safe.
            ConnectionIcon.Kind = If(isHttps, "Lock", If(_active.IsHome, "Home", "Globe"))
            ConnectionButton.ToolTip = If(_active.IsHome, "NOVA-startside", If(isHttps, "HTTPS-adresse. Klikk for forklaring.", "HTTP-adresse: ikke kryptert."))
            BookmarkIcon.Kind = If(_state.Bookmarks.Any(Function(p) p.Url = _active.Address), "StarFilled", "Star")
            ZoomButton.Content = CInt(_active.Zoom * 100).ToString() & " %"
            WindowCaption.Text = If(_active.IsPrivate, "PRIVAT ØKT / Besøkslisten er av", "Din arbeidsflate. Ditt fokus.") & "  /  NOVA"
            Me.Title = If(_active.IsPrivate, "Privat fane", _active.Title) & " - NOVA"
            StatusText.Text = If(_active.IsPrivate, "PRIVAT / ikke anonym på nettet", If(_active.IsHome, "NOVA / startside", If(isHttps, "HTTPS-adresse", "HTTP / ikke kryptert")))
        End Sub

        Private Async Function NavigateAsync(browserTab As BrowserTab, input As String) As Task
            If browserTab Is Nothing OrElse browserTab.Closed OrElse _isClosing Then Return
            CloseAddressSuggestions()
            WakeTab(browserTab)
            Try
                Dim address = UrlPolicy.Resolve(input, _state.Settings.SearchEngine)
                If address = UrlPolicy.HomeUrl Then
                    ShowHome(browserTab)
                    Return
                End If
                browserTab.NavigationVersion += 1
                Dim version = browserTab.NavigationVersion
                browserTab.IsHome = False
                browserTab.Address = address
                browserTab.ErrorMessage = ""
                browserTab.IsLoading = True
                browserTab.NotifyLocation()
                If browserTab Is _active Then
                    UpdateViewVisibility()
                    UpdateChrome(True)
                    Keyboard.ClearFocus()
                End If
                If Not Await EnsureViewAsync(browserTab) Then Return
                If browserTab.Closed OrElse _isClosing OrElse version <> browserTab.NavigationVersion Then Return
                browserTab.View.CoreWebView2.Navigate(address)
                If browserTab Is _active Then FocusActivePage()
                ScheduleSave()
            Catch ex As ArgumentException
                ShowToast(ex.Message)
                If browserTab Is _active Then UpdateChrome(True)
            Catch ex As Exception
                ReportTabError(browserTab, ex)
            End Try
        End Function

        Private Sub ShowHome(browserTab As BrowserTab)
            If browserTab Is Nothing OrElse browserTab.Closed Then Return
            CloseAddressSuggestions()
            WakeTab(browserTab)
            browserTab.NavigationVersion += 1
            browserTab.IsHome = True
            browserTab.Title = "Ny fane"
            browserTab.Address = UrlPolicy.HomeUrl
            browserTab.IsLoading = False
            browserTab.ErrorMessage = ""
            browserTab.NotifyLocation()
            If browserTab.View?.CoreWebView2 IsNot Nothing Then
                Try
                    browserTab.HomeBlankPending = True
                    browserTab.View.CoreWebView2.Stop()
                    browserTab.View.CoreWebView2.Navigate("about:blank")
                Catch ex As Exception
                    StateStore.LogError(ex)
                    ReleaseTabView(browserTab)
                End Try
            End If
            If browserTab Is _active Then
                HomeSearchBox.Clear()
                UpdateViewVisibility()
                UpdateChrome(True)
                AnimateEntrance(HomeContent, 12)
            End If
            ScheduleSave()
        End Sub

        Private Function EnsureViewAsync(browserTab As BrowserTab) As Task(Of Boolean)
            If browserTab.Initialization Is Nothing Then browserTab.Initialization = InitializeViewAsync(browserTab)
            Return browserTab.Initialization
        End Function

        Private Async Function InitializeViewAsync(browserTab As BrowserTab) As Task(Of Boolean)
            Dim epoch = browserTab.ViewEpoch
            Dim lifetime As New System.Threading.CancellationTokenSource()
            browserTab.InitializationCancellation = lifetime
            Try
                If _environment Is Nothing Then
                    _environment = CoreWebView2Environment.CreateAsync(userDataFolder:=StateStore.WebDataFolder)
                End If
                Dim env = Await _environment.WaitAsync(TimeSpan.FromSeconds(30), lifetime.Token)
                If browserTab.Closed OrElse _isClosing OrElse epoch <> browserTab.ViewEpoch Then Return False
                Dim view As New WebView2CompositionControl With {.ZoomFactor = browserTab.Zoom, .AllowExternalDrop = False, .Visibility = Visibility.Hidden}
                browserTab.View = view
                BrowserHost.Children.Add(view)
                Dim options = env.CreateCoreWebView2ControllerOptions()
                options.ProfileName = If(browserTab.IsPrivate, _privateProfile, "Default")
                options.IsInPrivateModeEnabled = browserTab.IsPrivate
                Await view.EnsureCoreWebView2Async(env, options).WaitAsync(TimeSpan.FromSeconds(30), lifetime.Token)
                If browserTab.Closed OrElse _isClosing OrElse epoch <> browserTab.ViewEpoch OrElse browserTab.View IsNot view Then
                    If browserTab.View Is view Then ReleaseTabView(browserTab)
                    Return False
                End If
                ConfigureWebView(browserTab)
                AttachWebEvents(browserTab)
                UpdateViewVisibility()
                Return True
            Catch ex As OperationCanceledException When lifetime.IsCancellationRequested
                Return False
            Catch ex As Exception
                If epoch <> browserTab.ViewEpoch Then Return False
                If _environment IsNot Nothing AndAlso _environment.IsFaulted Then _environment = Nothing
                ReleaseTabView(browserTab)
                ReportTabError(browserTab, ex)
                Return False
            Finally
                If browserTab.InitializationCancellation Is lifetime Then browserTab.InitializationCancellation = Nothing
                lifetime.Dispose()
            End Try
        End Function

        Private Sub ConfigureWebView(browserTab As BrowserTab)
            Dim core = browserTab.View.CoreWebView2
            core.IsMuted = browserTab.IsMuted
            core.Settings.AreHostObjectsAllowed = False
            core.Settings.IsWebMessageEnabled = False
            core.Settings.IsStatusBarEnabled = False
            core.Settings.AreDefaultContextMenusEnabled = True
            core.Settings.AreDevToolsEnabled = True
            core.Profile.IsPasswordAutosaveEnabled = False
            core.Profile.IsGeneralAutofillEnabled = False
            core.Profile.PreferredColorScheme = If(_state.Settings.Theme = "Dawn", CoreWebView2PreferredColorScheme.Light, CoreWebView2PreferredColorScheme.Dark)
        End Sub

        Private Function IsCurrentView(target As BrowserTab, view As WebView2CompositionControl) As Boolean
            Return Not _isClosing AndAlso Not _fatalShutdown AndAlso Not target.Closed AndAlso target.View Is view
        End Function
        Private Sub AttachWebEvents(browserTab As BrowserTab)
            Dim view = browserTab.View
            Dim core = view.CoreWebView2
            AddHandler core.NavigationStarting,
                Sub(sender, args)
                    If IsCurrentView(browserTab, view) Then
                        OnNavigationStarting(browserTab, args)
                    Else
                        args.Cancel = True
                    End If
                End Sub
            AddHandler core.NavigationCompleted,
                Sub(sender, args)
                    If IsCurrentView(browserTab, view) Then OnNavigationCompleted(browserTab, args)
                End Sub
            AddHandler core.SourceChanged,
                Sub(sender, args)
                    If Not IsCurrentView(browserTab, view) OrElse browserTab.IsHome Then Return
                    If UrlPolicy.IsWebUrl(core.Source) Then browserTab.Address = UrlPolicy.Resolve(core.Source, _state.Settings.SearchEngine)
                    browserTab.NotifyLocation()
                    If browserTab Is _active Then UpdateChrome()
                    ScheduleSave()
                End Sub
            AddHandler core.HistoryChanged,
                Sub(sender, args)
                    If IsCurrentView(browserTab, view) AndAlso browserTab Is _active Then UpdateChrome()
                End Sub
            AddHandler core.DocumentTitleChanged,
                Sub(sender, args)
                    If Not IsCurrentView(browserTab, view) OrElse browserTab.IsHome Then Return
                    Dim title = UrlPolicy.CleanTitle(core.DocumentTitle)
                    browserTab.Title = If(title.Length > 0, title, UrlPolicy.DisplayHost(browserTab.Address))
                    If Not browserTab.IsPrivate Then
                        Dim entry = _state.History.FirstOrDefault(Function(item) item.Url = browserTab.Address)
                        If entry IsNot Nothing Then entry.Title = browserTab.Title
                    End If
                    If browserTab Is _active Then UpdateChrome()
                    ScheduleSave()
                End Sub
            AddHandler core.IsDocumentPlayingAudioChanged,
                Sub(sender, args)
                    If IsCurrentView(browserTab, view) Then browserTab.IsPlayingAudio = core.IsDocumentPlayingAudio
                End Sub
            AddHandler core.IsMutedChanged,
                Sub(sender, args)
                    If IsCurrentView(browserTab, view) Then browserTab.IsMuted = core.IsMuted
                End Sub
            AddHandler core.NewWindowRequested,
                Async Sub(sender, args)
                    If IsCurrentView(browserTab, view) Then
                        Await HandleNewWindowAsync(browserTab, args)
                    Else
                        args.Handled = True
                    End If
                End Sub
            AddHandler core.PermissionRequested,
                Async Sub(sender, args)
                    If IsCurrentView(browserTab, view) Then
                        Await HandlePermissionAsync(browserTab, args)
                    Else
                        args.State = CoreWebView2PermissionState.Deny
                    End If
                End Sub
            AddHandler core.LaunchingExternalUriScheme, Sub(sender, args) args.Cancel = True
            AddHandler core.WindowCloseRequested,
                Sub(sender, args)
                    If IsCurrentView(browserTab, view) Then Dispatcher.BeginInvoke(New Action(Async Sub() Await CloseTabAsync(browserTab)))
                End Sub
            AddHandler core.ProcessFailed,
                Sub(sender, args)
                    If Not IsCurrentView(browserTab, view) Then Return
                    Dim affected = If(args.ProcessFailedKind = CoreWebView2ProcessFailedKind.BrowserProcessExited, _tabs.ToArray(), New BrowserTab() {browserTab}).
                        Select(Function(target) New With {.Target = target, .Epoch = target.ViewEpoch}).ToArray()
                    _environment = Nothing
                    Dispatcher.BeginInvoke(New Action(
                        Sub()
                            If _isClosing OrElse _fatalShutdown Then Return
                            For Each item In affected
                                Dim target = item.Target
                                If target.Closed OrElse target.ViewEpoch <> item.Epoch Then Continue For
                                ForgetTabDownloads(target)
                                ReleaseTabView(target)
                                target.IsLoading = False
                                If Not target.IsHome Then target.ErrorMessage = "Nettmotoren stoppet. Velg Prøv igjen for å laste siden på nytt."
                            Next
                            UpdateViewVisibility()
                            UpdateChrome()
                        End Sub))
                End Sub
            AddHandler core.DownloadStarting,
                Sub(sender, args)
                    If IsCurrentView(browserTab, view) Then
                        BeginDownload(browserTab, args)
                    Else
                        args.Cancel = True
                    End If
                End Sub
        End Sub

        Private Sub OnNavigationStarting(browserTab As BrowserTab, e As CoreWebView2NavigationStartingEventArgs)
            If browserTab.Closed OrElse _isClosing Then
                e.Cancel = True
                Return
            End If
            If Not UrlPolicy.IsAllowedNavigation(e.Uri) Then
                e.Cancel = True
                browserTab.IsLoading = False
                If browserTab Is _active Then
                    ShowToast("Denne adressetypen er blokkert. Bruk en http- eller https-adresse.")
                    UpdateChrome()
                End If
                Return
            End If
            If e.Uri = "about:blank" AndAlso browserTab.HomeBlankPending Then
                browserTab.HomeBlankPending = False
                e.Cancel = Not browserTab.IsHome
                Return
            End If
            If browserTab.IsHome Then
                e.Cancel = e.Uri <> "about:blank"
                Return
            End If
            browserTab.CurrentNavigationId = e.NavigationId
            browserTab.DocumentVersion += 1
            browserTab.IsSleeping = False
            If Not e.IsRedirected Then browserTab.HasSensitivePermission = False
            browserTab.IsHome = False
            browserTab.ErrorMessage = ""
            browserTab.IsLoading = True
            browserTab.Address = If(UrlPolicy.IsWebUrl(e.Uri), UrlPolicy.Resolve(e.Uri, _state.Settings.SearchEngine), e.Uri)
            browserTab.NotifyLocation()
            If browserTab Is _active Then
                UpdateViewVisibility()
                UpdateChrome()
            End If
        End Sub

        Private Sub OnNavigationCompleted(browserTab As BrowserTab, e As CoreWebView2NavigationCompletedEventArgs)
            If browserTab.Closed OrElse _isClosing OrElse browserTab.IsHome OrElse e.NavigationId <> browserTab.CurrentNavigationId Then Return
            browserTab.IsLoading = False
            If e.IsSuccess Then
                Dim core = browserTab.View.CoreWebView2
                browserTab.Address = If(UrlPolicy.IsWebUrl(core.Source), UrlPolicy.Resolve(core.Source, _state.Settings.SearchEngine), core.Source)
                Dim title = UrlPolicy.CleanTitle(core.DocumentTitle)
                browserTab.Title = If(title.Length > 0, title, UrlPolicy.DisplayHost(browserTab.Address))
                AddHistory(browserTab)
            ElseIf e.WebErrorStatus <> CoreWebView2WebErrorStatus.OperationCanceled Then
                ' WebView2 retains its own error/certificate UI; do not bypass TLS checks.
                If browserTab Is _active Then ShowToast("Siden kunne ikke lastes: " & e.WebErrorStatus.ToString() & ".")
            End If
            browserTab.NotifyLocation()
            If browserTab Is _active Then UpdateChrome()
            ScheduleSave()
        End Sub

        Private Sub ReportTabError(browserTab As BrowserTab, ex As Exception)
            StateStore.LogError(ex)
            If browserTab.Closed OrElse _isClosing Then Return
            browserTab.IsLoading = False
            browserTab.ErrorMessage = If(TypeOf ex Is WebView2RuntimeNotFoundException,
                "WebView2 Runtime mangler. Installer Microsoft Edge WebView2 Evergreen Runtime, og start NOVA på nytt. Se START-HER.txt.",
                "Nettmotoren kunne ikke starte denne fanen. Prøv igjen. Kontroller internett og WebView2 Runtime. Feiltype: " & ex.GetType().Name)
            If browserTab Is _active Then
                UpdateViewVisibility()
                UpdateChrome()
            End If
        End Sub

        Private Async Function CloseTabAsync(browserTab As BrowserTab) As Task
            If browserTab Is Nothing OrElse browserTab.Closed OrElse _isClosing Then Return
            If browserTab.IsPinned Then
                If Not Await AskAsync("Lukk festet fane?", "Denne fanen er festet. Vil du likevel lukke den?", "Lukk fane") Then Return
            End If
            If _downloads.Values.Any(Function(t) t Is browserTab) Then
                If Not Await AskAsync("Lukke fanen?", "Fanen har en pågående nedlasting som kan bli avbrutt.", "Lukk fane") Then Return
            End If
            RemoveTab(browserTab)
        End Function

        Private Sub RemoveTab(browserTab As BrowserTab)
            If browserTab.Closed Then Return
            If Not browserTab.IsPrivate Then
                _closedTabs.Push(New SessionEntry With {.Title = browserTab.Title, .Url = browserTab.Address, .IsPinned = browserTab.IsPinned, .IsMuted = browserTab.IsMuted, .Zoom = browserTab.Zoom})
                If _closedTabs.Count > 20 Then
                    Dim recent = _closedTabs.Take(20).Reverse().ToArray()
                    _closedTabs.Clear()
                    For Each entry In recent
                        _closedTabs.Push(entry)
                    Next
                End If
            End If
            Dim index = _tabs.IndexOf(browserTab)
            browserTab.Closed = True
            browserTab.NavigationVersion += 1
            ForgetTabDownloads(browserTab)
            ReleaseTabView(browserTab)
            For Each operation In _downloads.Where(Function(kv) kv.Value Is browserTab).Select(Function(kv) kv.Key).ToArray()
                _downloads.Remove(operation)
            Next
            _tabs.Remove(browserTab)
            If Not _tabs.Any(Function(t) t.IsPrivate) Then _privateProfile = "Private-" & Guid.NewGuid().ToString("N")
            If _tabs.Count = 0 Then CreateTab()
            If TabList.SelectedItem Is Nothing Then TabList.SelectedIndex = Math.Clamp(index - 1, 0, _tabs.Count - 1)
            TabCountText.Text = _tabs.Count.ToString("00")
            ScheduleSave()
        End Sub

        Private Sub ReopenTab()
            If _closedTabs.Count = 0 Then
                ShowToast("Ingen vanlig fane å åpne igjen.")
                Return
            End If
            Dim saved = _closedTabs.Pop()
            Dim target = CreateTab(saved.Url, False, False)
            If target Is Nothing Then
                _closedTabs.Push(saved)
                Return
            End If
            target.IsPinned = saved.IsPinned
            target.IsMuted = saved.IsMuted
            target.Zoom = saved.Zoom
            If target.IsPinned Then _tabs.Move(_tabs.IndexOf(target), _tabs.Where(Function(item) item.IsPinned).Count() - 1)
            TabList.SelectedItem = target
        End Sub

        Private Sub ScheduleSave()
            If _restoring OrElse _isClosing OrElse _fatalShutdown Then Return
            _saveTimer.Stop()
            _saveTimer.Start()
        End Sub

        Private Function PersistState() As Boolean
            If _fatalShutdown OrElse Not _sessionInitialized Then Return False
            Dim normal = _tabs.Where(Function(t) Not t.IsPrivate AndAlso (t.IsHome OrElse UrlPolicy.IsWebUrl(t.Address))).ToList()
            _state.Session = If(_state.Settings.RestoreSession,
                normal.Select(Function(t) New SessionEntry With {.Title = t.Title, .Url = If(t.IsHome, UrlPolicy.HomeUrl, t.Address), .IsPinned = t.IsPinned, .IsMuted = t.IsMuted, .Zoom = t.Zoom}).ToList(), New List(Of SessionEntry)())
            _state.ActiveSessionIndex = Math.Max(0, normal.IndexOf(_active))
            Dim saved = StateStore.Save(_state)
            If Not saved AndAlso Not _isClosing Then ShowToast("Kunne ikke lagre innstillingene. Kontroller plass og tilgang til datamappen.")
            Return saved
        End Function

        Private Async Sub Window_Closing(sender As Object, e As CancelEventArgs)
            If _isClosing Then Return
            If _downloads.Count > 0 AndAlso Not _allowClose Then
                e.Cancel = True
                If Await AskAsync("Avslutte NOVA?", "Det pågår nedlastinger. De kan bli avbrutt når nettleseren lukkes.", "Avslutt") Then
                    _allowClose = True
                    Close()
                End If
                Return
            End If
            If Not _fatalShutdown Then
                _state.LastExitClean = True
                PersistState()
            End If
            _isClosing = True
            StopWorkbench()
            _clock.Stop()
            _saveTimer.Stop()
            _toastTimer.Stop()
            _confirmCompletion?.TrySetResult(False)
            For Each browserTab In _tabs
                browserTab.Closed = True
                ForgetTabDownloads(browserTab)
                ReleaseTabView(browserTab)
            Next
        End Sub

        Friend Sub PrepareForFatalShutdown()
            ' Do not overwrite the saved session with partial crash-time state.
            _fatalShutdown = True
            StopWorkbench()
            _allowClose = True
            _clock.Stop()
            _saveTimer.Stop()
            _toastTimer.Stop()
            _confirmCompletion?.TrySetResult(False)
        End Sub

        Private Sub ReleaseTabView(browserTab As BrowserTab)
            If browserTab Is Nothing Then Return
            Dim view As WebView2CompositionControl = browserTab.View
            browserTab.ViewEpoch += 1
            Dim lifetime = browserTab.InitializationCancellation
            browserTab.InitializationCancellation = Nothing
            lifetime?.Cancel()
            browserTab.Initialization = Nothing
            browserTab.IsSleeping = False
            browserTab.IsPlayingAudio = False
            browserTab.View = Nothing
            If view Is Nothing Then Return
            Try
                BrowserHost.Children.Remove(view)
            Catch ex As Exception
                StateStore.LogError(ex)
            End Try
            Try
                view.Dispose()
            Catch ex As Exception
                ' The composition control can also fail in Dispose after failed init.
                ' Log it, but still allow the tab/window to close.
                StateStore.LogError(ex)
            End Try
        End Sub

        Private Sub Viewport_SizeChanged(sender As Object, e As SizeChangedEventArgs)
            Viewport.Clip = New RectangleGeometry(New Rect(0, 0, Math.Max(0, e.NewSize.Width), Math.Max(0, e.NewSize.Height)), 19, 19)
        End Sub
    End Class
End Namespace
