Imports System
Imports System.Collections.Generic
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

Namespace NovaBrowser
    Public Class CommandItem
        Public Property Label As String = ""
        Public Property Hint As String = ""
        Public Property Execute As Action
    End Class

    Partial Public Class MainWindow
        Private Sub InitializePreferences()
            _settingsReady = False
            ThemeSetting.ItemsSource = ThemeManager.Themes
            AccentSetting.ItemsSource = ThemeManager.Accents
            WallpaperSetting.ItemsSource = New String() {"Aurora", "Orbit", "Ingen"}
            SearchSetting.ItemsSource = SearchProviders.Names
            ZoomSetting.ItemsSource = New String() {"75 %", "90 %", "100 %", "110 %", "125 %", "150 %"}
            Dim s = _state.Settings
            NameSetting.Text = s.DisplayName
            ThemeSetting.SelectedItem = s.Theme
            AccentSetting.SelectedItem = s.Accent
            WallpaperSetting.SelectedItem = s.Wallpaper
            If WallpaperSetting.SelectedIndex < 0 Then WallpaperSetting.SelectedIndex = 0
            SearchSetting.SelectedItem = s.SearchEngine
            If SearchSetting.SelectedIndex < 0 Then SearchSetting.SelectedIndex = 0
            ZoomSetting.SelectedItem = CInt(s.DefaultZoom * 100).ToString() & " %"
            If ZoomSetting.SelectedIndex < 0 Then ZoomSetting.SelectedIndex = 2
            AnimationsSetting.IsChecked = s.Animations
            ClockSetting.IsChecked = s.ShowClock
            CompactSetting.IsChecked = s.CompactTabs
            RestoreSetting.IsChecked = s.RestoreSession
            HistorySetting.IsChecked = s.RecordHistory
            _settingsReady = True
            ApplyDensity()
        End Sub

        Private Sub Preferences_Changed(sender As Object, e As RoutedEventArgs)
            If Not _settingsReady OrElse _isClosing Then Return
            Dim s = _state.Settings
            s.DisplayName = UrlPolicy.CleanTitle(NameSetting.Text)
            s.Theme = CStr(ThemeSetting.SelectedItem)
            s.Accent = CStr(AccentSetting.SelectedItem)
            s.Wallpaper = CStr(WallpaperSetting.SelectedItem)
            s.SearchEngine = CStr(SearchSetting.SelectedItem)
            s.Animations = AnimationsSetting.IsChecked.GetValueOrDefault()
            s.ShowClock = ClockSetting.IsChecked.GetValueOrDefault()
            s.CompactTabs = CompactSetting.IsChecked.GetValueOrDefault()
            s.RestoreSession = RestoreSetting.IsChecked.GetValueOrDefault()
            s.RecordHistory = HistorySetting.IsChecked.GetValueOrDefault()
            s.DefaultZoom = Integer.Parse(CStr(ZoomSetting.SelectedItem).Replace(" %", ""), CultureInfo.InvariantCulture) / 100.0
            ThemeManager.Apply(Application.Current.Resources, s)
            For Each browserTab In _tabs
                If browserTab.View?.CoreWebView2 IsNot Nothing Then ConfigureWebView(browserTab)
            Next
            ApplyDensity()
            UpdateHome()
            UpdateClock()
            ScheduleSave()
        End Sub

        Private Sub ApplyDensity()
            Dim style As New Style(GetType(ListBoxItem), CType(FindResource("TabRow"), Style))
            style.Setters.Add(New Setter(Control.PaddingProperty, New Thickness(10, If(_state.Settings.CompactTabs, 6, 12), 10, If(_state.Settings.CompactTabs, 6, 12))))
            TabList.ItemContainerStyle = style
        End Sub

        Private Sub UpdateHome()
            HomeSearchHint.Text = "Søk med " & _state.Settings.SearchEngine & ", eller skriv en adresse"
            AddressBox.ToolTip = "Skriv en nettadresse eller søk med " & _state.Settings.SearchEngine & " (Ctrl+L)"
            ShortcutItems.ItemsSource = _state.Bookmarks.Take(6).ToList()
            Dim name = _state.Settings.DisplayName
            AvatarText.Text = If(name.Length > 0, name.Substring(0, 1).ToUpperInvariant(), "N")
            ClockText.Visibility = If(_state.Settings.ShowClock, Visibility.Visible, Visibility.Collapsed)
            AmbientLayer.Visibility = If(_state.Settings.Wallpaper = "Ingen", Visibility.Collapsed, Visibility.Visible)
            OrbitArt.Visibility = If(_state.Settings.Wallpaper = "Orbit", Visibility.Visible, Visibility.Collapsed)
            UpdateViewVisibility()
        End Sub

        Private Sub UpdateClock()
            ClockText.Text = DateTime.Now.ToString("HH:mm")
            DateText.Text = DateTime.Now.ToString("dddd d. MMMM", CultureInfo.GetCultureInfo("nb-NO")).ToUpper(CultureInfo.GetCultureInfo("nb-NO"))
        End Sub

        Private ReadOnly Property MotionEnabled As Boolean
            Get
                Dim animations As Boolean = _state.Settings.Animations
                If _setupSession IsNot Nothing Then animations = _setupSession.Draft.Animations
                Return animations AndAlso SystemParameters.ClientAreaAnimation
            End Get
        End Property

        Private Sub ConfigureAmbientMotion()
            If HaloOne Is Nothing Then Return
            Dim one = CType(HaloOne.RenderTransform, TranslateTransform)
            Dim two = CType(HaloTwo.RenderTransform, TranslateTransform)
            one.BeginAnimation(TranslateTransform.XProperty, Nothing)
            two.BeginAnimation(TranslateTransform.XProperty, Nothing)
            If Not MotionEnabled OrElse Not IsVisible OrElse WindowState = WindowState.Minimized OrElse HomeScreen.Visibility <> Visibility.Visible OrElse AmbientLayer.Visibility <> Visibility.Visible Then Return
            Dim drift As New DoubleAnimation(-18, 20, TimeSpan.FromSeconds(14)) With {.AutoReverse = True, .RepeatBehavior = RepeatBehavior.Forever, .EasingFunction = New SineEase With {.EasingMode = EasingMode.EaseInOut}}
            one.BeginAnimation(TranslateTransform.XProperty, drift)
            two.BeginAnimation(TranslateTransform.XProperty, New DoubleAnimation(12, -26, TimeSpan.FromSeconds(18)) With {.AutoReverse = True, .RepeatBehavior = RepeatBehavior.Forever})
        End Sub

        Private Sub AnimateEntrance(element As FrameworkElement, offset As Double)
            element.BeginAnimation(UIElement.OpacityProperty, Nothing)
            element.Opacity = 1
            Dim move As New TranslateTransform()
            element.RenderTransform = move
            If Not MotionEnabled Then Return
            Dim duration = TimeSpan.FromMilliseconds(220)
            element.BeginAnimation(UIElement.OpacityProperty, New DoubleAnimation(0.35, 1, duration))
            move.BeginAnimation(TranslateTransform.YProperty, New DoubleAnimation(offset, 0, duration) With {.EasingFunction = New CubicEase With {.EasingMode = EasingMode.EaseOut}})
        End Sub

        Private Sub ShowToast(text As String)
            If _isClosing Then Return
            ToastText.Text = text
            ToastCard.Visibility = Visibility.Visible
            AnimateEntrance(ToastCard, 8)
            _toastTimer.Stop()
            _toastTimer.Start()
        End Sub

        Private Sub ShowDrawer(mode As String)
            CommandLayer.Visibility = Visibility.Collapsed
            _drawerMode = mode
            DrawerTitle.Text = If(mode = "settings", "Gjør NOVA til din.", If(mode = "bookmarks", "Dine bokmerker.", "Her har du vært."))
            SettingsScroller.Visibility = If(mode = "settings", Visibility.Visible, Visibility.Collapsed)
            LibraryPanel.Visibility = If(mode = "settings", Visibility.Collapsed, Visibility.Visible)
            If mode <> "settings" Then RefreshLibrary()
            DrawerLayer.Visibility = Visibility.Visible
            RefreshInputScopes()
            AnimateEntrance(DrawerCard, 10)
            If mode = "settings" Then
                NameSetting.Focus()
            Else
                LibraryList.Focus()
            End If
        End Sub

        Private Sub RefreshLibrary()
            Dim entries = If(_drawerMode = "bookmarks", _state.Bookmarks.ToList(), _state.History.ToList())
            LibraryList.ItemsSource = entries
            LibraryHint.Text = If(entries.Count = 0, "Listen er tom.", If(_drawerMode = "bookmarks", "Åpne en side, og trykk på stjernen for å lagre den.", "De siste 500 besøkene. Private faner blir ikke tatt med."))
        End Sub

        Private Sub CloseDrawer()
            Preferences_Changed(Me, New RoutedEventArgs())
            DrawerLayer.Visibility = Visibility.Collapsed
            RefreshInputScopes()
            FocusActivePage()
        End Sub

        Private Sub RefreshInputScopes()
            Dim confirming = ConfirmLayer.Visibility = Visibility.Visible
            Dim commanding = CommandLayer.Visibility = Visibility.Visible
            Dim drawing = DrawerLayer.Visibility = Visibility.Visible
            Dim settingUp As Boolean = SetupLayer.Visibility = Visibility.Visible
            ShellBody.IsEnabled = Not (confirming OrElse commanding OrElse drawing OrElse settingUp)
            DrawerLayer.IsEnabled = Not (confirming OrElse commanding OrElse settingUp)
            CommandLayer.IsEnabled = Not (confirming OrElse settingUp)
            SetupLayer.IsEnabled = Not confirming
        End Sub

        Private Sub FocusActivePage()
            If ConfirmLayer.Visibility = Visibility.Visible Then
                ConfirmNoButton.Focus()
                Return
            ElseIf SetupLayer.Visibility = Visibility.Visible Then
                FocusSetupStep()
                Return
            ElseIf CommandLayer.Visibility = Visibility.Visible Then
                CommandSearchBox.Focus()
                Return
            ElseIf DrawerLayer.Visibility = Visibility.Visible Then
                If _drawerMode = "settings" Then
                    NameSetting.Focus()
                Else
                    LibraryList.Focus()
                End If
                Return
            End If
            If _active?.View IsNot Nothing AndAlso Not _active.IsHome Then
                _active.View.Focus()
            Else
                HomeSearchBox.Focus()
            End If
        End Sub

        Private Sub AddHistory(browserTab As BrowserTab)
            If browserTab.IsPrivate OrElse Not _state.Settings.RecordHistory OrElse Not UrlPolicy.IsWebUrl(browserTab.Address) Then Return
            _state.History.RemoveAll(Function(p) p.Url = browserTab.Address)
            _state.History.Insert(0, New PageEntry With {.Title = browserTab.Title, .Url = browserTab.Address, .VisitedAt = DateTimeOffset.Now})
            If _state.History.Count > 500 Then _state.History.RemoveRange(500, _state.History.Count - 500)
        End Sub

        Private Sub BookmarkCurrent()
            If _active Is Nothing OrElse Not UrlPolicy.IsWebUrl(_active.Address) Then
                ShowDrawer("bookmarks")
                Return
            End If
            Dim existing = _state.Bookmarks.FirstOrDefault(Function(p) p.Url = _active.Address)
            If existing IsNot Nothing Then
                _state.Bookmarks.Remove(existing)
                ShowToast("Bokmerket er fjernet.")
            Else
                If _state.Bookmarks.Count >= 500 Then
                    ShowToast("Bokmerkelisten er full. Fjern et bokmerke først.")
                    Return
                End If
                _state.Bookmarks.Add(New PageEntry With {.Title = _active.Title, .Url = _active.Address})
                ShowToast(If(_active.IsPrivate, "Bokmerket er lagret. Det beholdes også etter at den private fanen lukkes.", "Lagret i bokmerkene dine."))
            End If
            UpdateHome()
            UpdateChrome()
            If _drawerMode = "bookmarks" Then RefreshLibrary()
            ScheduleSave()
        End Sub

        Private Sub ShowCommands()
            If ConfirmLayer.Visibility = Visibility.Visible Then Return
            DrawerLayer.Visibility = Visibility.Collapsed
            CommandLayer.Visibility = Visibility.Visible
            RefreshInputScopes()
            CommandSearchBox.Clear()
            RefreshCommands()
            AnimateEntrance(CommandCard, -10)
            CommandSearchBox.Focus()
        End Sub

        Private Sub RefreshCommands()
            If CommandList Is Nothing Then Return
            Dim commands As New List(Of CommandItem) From {
                New CommandItem With {.Label = "Ny fane", .Hint = "Ctrl+T", .Execute = Sub() CreateTab()},
                New CommandItem With {.Label = "Ny privat fane", .Hint = "Ctrl+Shift+N", .Execute = Sub() CreateTab(isPrivate:=True)},
                New CommandItem With {.Label = "Tilpass tema og innstillinger", .Hint = "Utseende", .Execute = Sub() ShowDrawer("settings")},
                New CommandItem With {.Label = "Vis bokmerker", .Hint = "Lagrede sider", .Execute = Sub() ShowDrawer("bookmarks")},
                New CommandItem With {.Label = "Vis historikk", .Hint = "Ctrl+H", .Execute = Sub() ShowDrawer("history")},
                New CommandItem With {.Label = "Åpne igjen lukket fane", .Hint = "Ctrl+Shift+T", .Execute = AddressOf ReopenTab},
                New CommandItem With {.Label = "Vis / skjul sidefelt", .Hint = "Ctrl+B", .Execute = AddressOf ToggleSidebar},
                New CommandItem With {.Label = "Tilbake til startsiden", .Hint = "Alt+Home", .Execute = Sub() ShowHome(_active)},
                New CommandItem With {.Label = "Nedlastinger", .Hint = "Ctrl+J", .Execute = Async Sub() Await ShowDownloadsAsync()}}
            For Each browserTab In _tabs
                Dim target = browserTab
                commands.Add(New CommandItem With {.Label = "Bytt til: " & target.Title, .Hint = If(target.IsPrivate, "Privat fane", "Fane"), .Execute = Sub() TabList.SelectedItem = target})
            Next
            Dim query = CommandSearchBox.Text.Trim()
            Dim result = commands.Where(Function(c) query.Length = 0 OrElse c.Label.Contains(query, StringComparison.CurrentCultureIgnoreCase)).ToList()
            If query.Length > 0 Then
                result.Add(New CommandItem With {.Label = "Søk / åpne: " & query, .Hint = "Enter", .Execute = Async Sub() Await NavigateAsync(_active, query)})
            End If
            CommandList.ItemsSource = result
            If result.Count > 0 Then CommandList.SelectedIndex = 0
        End Sub

        Private Sub ExecuteSelectedCommand()
            Dim item = TryCast(CommandList.SelectedItem, CommandItem)
            If item Is Nothing Then Return
            CommandLayer.Visibility = Visibility.Collapsed
            RefreshInputScopes()
            ' Post work outside any WebView2 accelerator callback.
            Dispatcher.BeginInvoke(item.Execute, DispatcherPriority.Normal)
        End Sub

        Private Sub ToggleSidebar()
            _sidebarVisible = Not _sidebarVisible
            Sidebar.Visibility = If(_sidebarVisible, Visibility.Visible, Visibility.Collapsed)
            SidebarColumn.Width = New GridLength(If(_sidebarVisible, 238, 0))
            If _sidebarVisible Then AnimateEntrance(Sidebar, 8)
        End Sub

        Private Sub ChangeZoom(delta As Double)
            If _active Is Nothing Then Return
            _active.Zoom = Math.Clamp(Math.Round(_active.Zoom + delta, 2), 0.25, 3.0)
            If _active.View IsNot Nothing Then _active.View.ZoomFactor = _active.Zoom
            UpdateChrome()
        End Sub

        Private Sub ResetZoom()
            If _active Is Nothing Then Return
            _active.Zoom = 1
            If _active.View IsNot Nothing Then _active.View.ZoomFactor = 1
            UpdateChrome()
        End Sub

        Private Sub NavigateBack()
            If _active?.View?.CoreWebView2 Is Nothing OrElse Not _active.View.CanGoBack Then Return
            _active.IsHome = False
            _active.ErrorMessage = ""
            _active.View.GoBack()
            UpdateViewVisibility()
        End Sub

        Private Sub NavigateForward()
            If _active?.View?.CoreWebView2 Is Nothing OrElse Not _active.View.CanGoForward Then Return
            _active.IsHome = False
            _active.ErrorMessage = ""
            _active.View.GoForward()
            UpdateViewVisibility()
        End Sub

        Private Sub ReloadCurrent()
            If _active Is Nothing OrElse _active.IsHome Then Return
            If _active.ErrorMessage.Length > 0 Then
                Retry_Click(Me, New RoutedEventArgs())
            ElseIf _active.View?.CoreWebView2 IsNot Nothing Then
                If _active.IsLoading Then
                    _active.View.CoreWebView2.Stop()
                    _active.IsLoading = False
                    UpdateChrome()
                Else
                    _active.View.Reload()
                End If
            End If
        End Sub

        Private Sub CycleTab(direction As Integer)
            If _tabs.Count = 0 Then Return
            TabList.SelectedIndex = (Math.Max(0, TabList.SelectedIndex) + direction + _tabs.Count) Mod _tabs.Count
            TabList.ScrollIntoView(TabList.SelectedItem)
        End Sub

        Private Sub FocusAddress()
            AddressBox.Focus()
            AddressBox.SelectAll()
        End Sub

        Private Sub Window_PreviewKeyDown(sender As Object, e As KeyEventArgs)
            Dim pressedKey As Key = If(e.Key = Key.System, e.SystemKey, e.Key)
            If ConfirmLayer.Visibility = Visibility.Visible Then
                If pressedKey = Key.Escape Then CompleteConfirmation(False)
                If pressedKey <> Key.Tab AndAlso pressedKey <> Key.Enter AndAlso pressedKey <> Key.Space Then e.Handled = True
                Return
            End If
            If SetupLayer.Visibility = Visibility.Visible Then
                If pressedKey = Key.Escape Then
                    CancelSetup()
                    e.Handled = True
                End If
                ' Keep normal text editing, arrows, Tab, Enter and Alt+F4 working.
                ' Do not dispatch browser shortcuts while setup owns focus.
                Return
            End If
            If CommandLayer.Visibility = Visibility.Visible Then
                If pressedKey = Key.Escape Then
                    CommandLayer.Visibility = Visibility.Collapsed
                    RefreshInputScopes()
                    FocusActivePage()
                    e.Handled = True
                End If
                Return
            End If
            If DrawerLayer.Visibility = Visibility.Visible Then
                If pressedKey = Key.Escape Then
                    CloseDrawer()
                    e.Handled = True
                End If
                Return
            End If
            Dim ctrl = (Keyboard.Modifiers And ModifierKeys.Control) <> 0
            Dim shift = (Keyboard.Modifiers And ModifierKeys.Shift) <> 0
            Dim alt = (Keyboard.Modifiers And ModifierKeys.Alt) <> 0
            Dim action As Action = Nothing
            If ctrl Then
                Select Case pressedKey
                    Case Key.L : action = AddressOf FocusAddress
                    Case Key.K : action = AddressOf ShowCommands
                    Case Key.T
                        action = If(shift, CType(AddressOf ReopenTab, Action), New Action(Sub() CreateTab()))
                    Case Key.N
                        If shift Then action = Sub() CreateTab(isPrivate:=True)
                    Case Key.W : action = Async Sub() Await CloseTabAsync(_active)
                    Case Key.Tab : action = Sub() CycleTab(If(shift, -1, 1))
                    Case Key.B : action = AddressOf ToggleSidebar
                    Case Key.D : action = AddressOf BookmarkCurrent
                    Case Key.H : action = Sub() ShowDrawer("history")
                    Case Key.J : action = Async Sub() Await ShowDownloadsAsync()
                    Case Key.R : action = AddressOf ReloadCurrent
                    Case Key.D0, Key.NumPad0 : action = AddressOf ResetZoom
                    Case Key.OemPlus, Key.Add : action = Sub() ChangeZoom(0.1)
                    Case Key.OemMinus, Key.Subtract : action = Sub() ChangeZoom(-0.1)
                    Case Key.D1 To Key.D9
                        Dim index = CInt(pressedKey) - CInt(Key.D1)
                        action = Sub() TabList.SelectedIndex = If(index = 8, _tabs.Count - 1, Math.Min(index, _tabs.Count - 1))
                End Select
            ElseIf alt Then
                Select Case pressedKey
                    Case Key.Left : action = AddressOf NavigateBack
                    Case Key.Right : action = AddressOf NavigateForward
                    Case Key.Home : action = Sub() ShowHome(_active)
                End Select
            ElseIf pressedKey = Key.F5 Then
                action = AddressOf ReloadCurrent
            ElseIf pressedKey = Key.Escape AndAlso _active IsNot Nothing AndAlso _active.IsLoading Then
                action = AddressOf ReloadCurrent
            End If
            If action IsNot Nothing Then
                e.Handled = True
                Dispatcher.BeginInvoke(action, DispatcherPriority.Normal)
            End If
        End Sub

        Private Async Sub AddressBox_KeyDown(sender As Object, e As KeyEventArgs)
            If e.Key <> Key.Enter Then Return
            e.Handled = True
            Await NavigateAsync(_active, AddressBox.Text)
        End Sub
        Private Sub AddressBox_GotKeyboardFocus(sender As Object, e As KeyboardFocusChangedEventArgs)
            AddressBox.SelectAll()
        End Sub
        Private Async Sub HomeSearchBox_KeyDown(sender As Object, e As KeyEventArgs)
            If e.Key <> Key.Enter Then Return
            e.Handled = True
            Await NavigateAsync(_active, HomeSearchBox.Text)
        End Sub
        Private Async Sub HomeSearch_Click(sender As Object, e As RoutedEventArgs)
            Await NavigateAsync(_active, HomeSearchBox.Text)
        End Sub
        Private Async Sub Shortcut_Click(sender As Object, e As RoutedEventArgs)
            Dim entry = TryCast(CType(sender, FrameworkElement).DataContext, PageEntry)
            If entry IsNot Nothing Then Await NavigateAsync(_active, entry.Url)
        End Sub
        Private Async Sub LibraryEntry_Click(sender As Object, e As RoutedEventArgs)
            Dim entry = TryCast(CType(sender, FrameworkElement).DataContext, PageEntry)
            If entry Is Nothing Then Return
            CloseDrawer()
            Await NavigateAsync(_active, entry.Url)
        End Sub
        Private Sub DeleteEntry_Click(sender As Object, e As RoutedEventArgs)
            Dim entry = TryCast(CType(sender, FrameworkElement).DataContext, PageEntry)
            If entry Is Nothing Then Return
            If _drawerMode = "bookmarks" Then
                _state.Bookmarks.Remove(entry)
            Else
                _state.History.Remove(entry)
            End If
            RefreshLibrary()
            UpdateHome()
            UpdateChrome()
            ScheduleSave()
        End Sub
        Private Sub NewTab_Click(sender As Object, e As RoutedEventArgs)
            CreateTab()
            HomeSearchBox.Focus()
        End Sub
        Private Sub NewPrivateTab_Click(sender As Object, e As RoutedEventArgs)
            CreateTab(isPrivate:=True)
            ShowToast("Privat fane: ikke lagret i historikken. Nettsteder og nettleverandøren kan fortsatt se aktiviteten.")
        End Sub
        Private Async Sub CloseTab_Click(sender As Object, e As RoutedEventArgs)
            e.Handled = True
            Await CloseTabAsync(TryCast(CType(sender, FrameworkElement).DataContext, BrowserTab))
        End Sub
        Private Sub TabList_PreviewMouseDown(sender As Object, e As MouseButtonEventArgs)
            If e.ChangedButton <> MouseButton.Middle Then Return
            Dim item = TryCast(ItemsControl.ContainerFromElement(TabList, TryCast(e.OriginalSource, DependencyObject)), ListBoxItem)
            If item Is Nothing Then Return
            e.Handled = True
            Dim browserTab = TryCast(item.DataContext, BrowserTab)
            Dispatcher.BeginInvoke(New Action(Async Sub() Await CloseTabAsync(browserTab)))
        End Sub
        Private Sub Home_Click(sender As Object, e As RoutedEventArgs)
            ShowHome(_active)
        End Sub
        Private Sub Back_Click(sender As Object, e As RoutedEventArgs)
            NavigateBack()
        End Sub
        Private Sub Forward_Click(sender As Object, e As RoutedEventArgs)
            NavigateForward()
        End Sub
        Private Sub Reload_Click(sender As Object, e As RoutedEventArgs)
            ReloadCurrent()
        End Sub
        Private Async Sub Retry_Click(sender As Object, e As RoutedEventArgs)
            Dim browserTab = _active
            If browserTab Is Nothing Then Return
            ReleaseTabView(browserTab)
            browserTab.Initialization = Nothing
            Await NavigateAsync(browserTab, browserTab.Address)
        End Sub
        Private Sub BookmarkCurrent_Click(sender As Object, e As RoutedEventArgs)
            BookmarkCurrent()
        End Sub
        Private Sub Bookmarks_Click(sender As Object, e As RoutedEventArgs)
            ShowDrawer("bookmarks")
        End Sub
        Private Sub History_Click(sender As Object, e As RoutedEventArgs)
            ShowDrawer("history")
        End Sub
        Private Sub Settings_Click(sender As Object, e As RoutedEventArgs)
            ShowDrawer("settings")
        End Sub
        Private Sub CloseDrawer_Click(sender As Object, e As RoutedEventArgs)
            CloseDrawer()
        End Sub
        Private Sub DrawerBackdrop_Click(sender As Object, e As MouseButtonEventArgs)
            CloseDrawer()
        End Sub
        Private Sub ToggleSidebar_Click(sender As Object, e As RoutedEventArgs)
            ToggleSidebar()
        End Sub
        Private Sub ZoomReset_Click(sender As Object, e As RoutedEventArgs)
            ResetZoom()
        End Sub
        Private Sub Command_Click(sender As Object, e As RoutedEventArgs)
            ShowCommands()
        End Sub
        Private Sub CommandSearch_TextChanged(sender As Object, e As TextChangedEventArgs)
            RefreshCommands()
        End Sub
        Private Sub CommandSearch_KeyDown(sender As Object, e As KeyEventArgs)
            If e.Key = Key.Enter Then
                e.Handled = True
                ExecuteSelectedCommand()
            ElseIf e.Key = Key.Down OrElse e.Key = Key.Up Then
                Dim direction = If(e.Key = Key.Down, 1, -1)
                CommandList.SelectedIndex = Math.Clamp(CommandList.SelectedIndex + direction, 0, Math.Max(0, CommandList.Items.Count - 1))
                If CommandList.SelectedItem IsNot Nothing Then CommandList.ScrollIntoView(CommandList.SelectedItem)
                e.Handled = True
            End If
        End Sub
        Private Sub CommandList_MouseUp(sender As Object, e As MouseButtonEventArgs)
            If e.ChangedButton <> MouseButton.Left Then Return
            Dim row = ItemsControl.ContainerFromElement(CommandList, TryCast(e.OriginalSource, DependencyObject))
            If row IsNot Nothing Then ExecuteSelectedCommand()
        End Sub
        Private Sub CommandList_KeyDown(sender As Object, e As KeyEventArgs)
            If e.Key = Key.Enter Then
                ExecuteSelectedCommand()
                e.Handled = True
            End If
        End Sub
        Private Sub CommandBackdrop_Click(sender As Object, e As MouseButtonEventArgs)
            CommandLayer.Visibility = Visibility.Collapsed
            RefreshInputScopes()
            FocusActivePage()
        End Sub
        Private Sub Minimize_Click(sender As Object, e As RoutedEventArgs)
            WindowState = WindowState.Minimized
        End Sub
        Private Sub Maximize_Click(sender As Object, e As RoutedEventArgs)
            WindowState = If(WindowState = WindowState.Maximized, WindowState.Normal, WindowState.Maximized)
        End Sub
        Private Sub CloseWindow_Click(sender As Object, e As RoutedEventArgs)
            Close()
        End Sub
        Private Sub Window_StateChanged(sender As Object, e As EventArgs)
            If WindowFrame Is Nothing Then Return
            WindowFrame.Padding = New Thickness(If(WindowState = WindowState.Maximized, 7, 0))
            If MaximizeIcon IsNot Nothing Then MaximizeIcon.Kind = If(WindowState = WindowState.Maximized, "Restore", "Maximize")
            ConfigureAmbientMotion()
        End Sub
        Private Sub Connection_Click(sender As Object, e As RoutedEventArgs)
            If _active Is Nothing Then Return
            If _active.IsHome Then
                ShowToast("Dette er NOVAs lokale startside. Ingen nettside er åpen her.")
            ElseIf _active.Address.StartsWith("https://", StringComparison.OrdinalIgnoreCase) Then
                ShowToast("Adressen bruker HTTPS. Dette alene betyr ikke at nettstedet er pålitelig. Sertifikatfeil håndteres av nettmotoren; de blir ikke ignorert.")
            Else
                ShowToast("HTTP er ikke kryptert. Ikke skriv inn passord eller sensitive opplysninger på denne forbindelsen.")
            End If
        End Sub
        Private Async Sub Downloads_Click(sender As Object, e As RoutedEventArgs)
            Await ShowDownloadsAsync()
        End Sub
        Private Async Function ShowDownloadsAsync() As Task
            Dim browserTab = _active
            If browserTab Is Nothing Then Return
            Try
                If Not Await EnsureViewAsync(browserTab) Then Return
                If browserTab.Closed OrElse _isClosing Then Return
                If browserTab.IsHome Then
                    browserTab.IsHome = False
                    browserTab.Title = "Nedlastinger"
                    browserTab.Address = "about:blank"
                    browserTab.NotifyLocation()
                    UpdateViewVisibility()
                    UpdateChrome(True)
                End If
                browserTab.View.CoreWebView2.OpenDefaultDownloadDialog()
            Catch ex As Exception
                StateStore.LogError(ex)
                ShowToast("Nedlastingsvinduet kunne ikke åpnes. Åpne en nettside og prøv igjen.")
            End Try
        End Function
    End Class
End Namespace
