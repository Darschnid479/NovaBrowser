"""One-time source migration. CI materializes and tests the resulting VB/XAML.
Not used by the browser or by end users. Never changes a user profile.
"""
from pathlib import Path
import re

R = Path(__file__).resolve().parents[1]
S = 'src/NovaBrowser/'
def read(name):
    return (R / name).read_text(encoding='utf-8-sig')
def put(name, text):
    path = R / name
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(text, encoding='utf-8', newline='\n')
def edit(name, before, after, count=1):
    text = read(name)
    if before not in text:
        raise RuntimeError('Migration anchor missing: ' + name + ': ' + before[:90])
    put(name, text.replace(before, after, count))
def method(name, start, end, replacement):
    text = read(name)
    a, b = text.index(start), text.index(end, text.index(start))
    put(name, text[:a] + replacement + text[b:])

if '<Version>0.3.0</Version>' in read(S + 'NovaBrowser.vbproj'):
    print('0.3.0 source already materialized')
    raise SystemExit(0)

edit(S+'Models/AppState.vb', 'Public Property DefaultZoom As Double = 1.0', '''Public Property DefaultZoom As Double = 1.0
        Public Property StartMaximized As Boolean = True
        Public Property WindowWidth As Double = 1280
        Public Property WindowHeight As Double = 820
        Public Property SleepingTabs As Boolean = False
        Public Property SleepAfterMinutes As Integer = 15''')
edit(S+'Models/AppState.vb', 'Public Class SessionEntry', '''Public Class SessionEntry
        Public Property IsPinned As Boolean
        Public Property IsMuted As Boolean
        Public Property Zoom As Double = 1.0''')
edit(S+'Models/AppState.vb', 'Public Property SchemaVersion As Integer = 1', 'Public Property SchemaVersion As Integer = 1\n        Public Property LastExitClean As Boolean = True')
edit(S+'Models/BrowserTab.vb', 'Public Property NavigationVersion As Integer', '''Public Property NavigationVersion As Integer
        Public Property ViewEpoch As Integer
        Public Property DocumentVersion As Integer
        Public Property CurrentNavigationId As ULong
        Public Property HomeBlankPending As Boolean
        Public Property LastActiveUtc As DateTimeOffset = DateTimeOffset.UtcNow
        Public Property SuspendPending As Boolean
        Public Property HasSensitivePermission As Boolean''')
properties = ''
for prop, field in [('IsPinned','_pinned'),('IsMuted','_muted'),('IsSleeping','_sleeping'),('IsPlayingAudio','_audio')]:
    properties += f'''        Private {field} As Boolean
        Public Property {prop} As Boolean
            Get
                Return {field}
            End Get
            Set(value As Boolean)
                If SetValue({field}, value) Then NotifyLocation()
            End Set
        End Property
'''
edit(S+'Models/BrowserTab.vb', '        Public ReadOnly Property IconKind As String', properties+'        Public ReadOnly Property IconKind As String')
edit(S+'Models/BrowserTab.vb', '                If IsPrivate Then Return "Lock"', '''                If IsSleeping Then Return "Sleep"
                If IsMuted Then Return "Mute"
                If IsPlayingAudio Then Return "Volume"
                If IsPinned Then Return "Pin"
                If IsPrivate Then Return "Lock"''')
edit(S+'Models/BrowserTab.vb', 'Return If(IsPrivate, "Privat · " & host, host)', 'Return If(IsPrivate, "Privat / ", "") & If(IsPinned, "Festet / ", "") & If(IsSleeping, "Hviler / ", If(IsMuted, "Dempet / ", "")) & host')
edit(S+'UI/NovaIcon.vb', '{"Close",', '''{"Pin", "M9,3 L15,3 14,9 18,13 18,15 6,15 6,13 10,9 Z M12,15 L12,22"},
            {"Sleep", "M5,6 H13 L5,16 H13 M16,3 H21 L16,9 H21"},
            {"Volume", "M3,9 H7 L12,5 V19 L7,15 H3 Z M16,8 Q21,12 16,16"},
            {"Mute", "M3,9 H7 L12,5 V19 L7,15 H3 Z M16,9 L22,15 M22,9 L16,15"},
            {"Close",''')

# Native caption is outside the content tree, so drawers cannot cover the X.
x = read(S+'MainWindow.xaml')
x = x.replace('Width="1460" Height="920" MinWidth="1000" MinHeight="680"', 'Width="1280" Height="820" MinWidth="760" MinHeight="500"')
x = x.replace('WindowStyle="None"', 'WindowStyle="SingleBorderWindow"')
x = re.sub(r'    <shell:WindowChrome.WindowChrome>.*?</shell:WindowChrome.WindowChrome>\n', '', x, flags=re.S)
x = x.replace('BorderThickness="1" CornerRadius="12">\n        <Grid x:Name="RootLayout">', 'BorderThickness="1" CornerRadius="0">\n        <Grid x:Name="RootLayout">', 1)
a = x.index('                <StackPanel Grid.Column="2" Orientation="Horizontal"')
b = x.index('                </StackPanel>', a) + len('                </StackPanel>')
x = x[:a] + '''                <StackPanel Grid.Column="2" Orientation="Horizontal" HorizontalAlignment="Right">
                    <Button x:Name="FocusModeButton" Content="Fokus" Padding="8,6" Click="FocusMode_Click" ToolTip="Ctrl+Shift+F" AutomationProperties.Name="Fokusmodus" />
                    <Button Content="Ctrl K" Padding="8,6" Click="Command_Click" ToolTip="Finn faner og kommandoer" AutomationProperties.Name="Kommandoer" />
                </StackPanel>''' + x[b:]
x = x.replace('PREVIEW 0.2','PREVIEW 0.3').replace('NOVA 0.2.0','NOVA 0.3.0')
x = x.replace('<ColumnDefinition Width="138" />','<ColumnDefinition Width="170" />',1)
x = x.replace('<ColumnDefinition Width="66" />','<ColumnDefinition x:Name="RailColumn" Width="66" />',1)
x = x.replace('<Border BorderBrush="{DynamicResource Stroke}" BorderThickness="0,0,1,0" Margin="0,8,0,12">','<Border x:Name="AppRail" BorderBrush="{DynamicResource Stroke}" BorderThickness="0,0,1,0" Margin="0,8,0,12">',1)
x = x.replace('PreviewMouseDown="TabList_PreviewMouseDown"','PreviewMouseDown="TabList_PreviewMouseDown" PreviewMouseRightButtonUp="TabList_ContextMenu"')
x = x.replace('KeyDown="AddressBox_KeyDown"','KeyDown="AddressBox_KeyDown" TextChanged="AddressBox_TextChanged"')
x = x.replace('Grid.RowSpan="2"','Grid.Row="1"')
x = x.replace('MaxWidth="900" Height="560"','MaxWidth="900" MaxHeight="560"')
x = x.replace('Width="580" MaxHeight="580"','Width="580" MaxWidth="{Binding ActualWidth, ElementName=RootLayout}" MaxHeight="580"')
x = x.replace('Width="430" HorizontalAlignment="Right"','Width="430" MaxWidth="{Binding ActualWidth, ElementName=RootLayout}" HorizontalAlignment="Right"')
x = x.replace('<CheckBox x:Name="CompactSetting"', '''<CheckBox x:Name="MaximizedSetting" Content="Start maksimert" Checked="Preferences_Changed" Unchecked="Preferences_Changed" />
                                <CheckBox x:Name="SleepSetting" Content="La ubrukte faner hvile etter 15 minutter" Checked="Preferences_Changed" Unchecked="Preferences_Changed" />
                                <TextBlock Text="Valgfritt. Lyd, nedlastinger og tillatelser beskyttes. Live-oppdateringer i andre bakgrunnsfaner kan pauses." Style="{StaticResource MutedText}" />
                                <CheckBox x:Name="CompactSetting"''')
x = x.replace('<Button Content="Slett historikken"', '''<Button Content="Eksporter bokmerker til JSON" HorizontalAlignment="Left" Click="ExportBookmarks_Click" />
                                <Button Content="Importer bokmerker fra JSON" HorizontalAlignment="Left" Click="ImportBookmarks_Click" />
                                <Button Content="Slett historikken"''')
pos = x.index('            <!-- Settings and library drawer -->')
x = x[:pos] + '''            <Popup x:Name="AddressSuggestions" PlacementTarget="{Binding ElementName=AddressBox}" Placement="Bottom" StaysOpen="False" AllowsTransparency="True" PopupAnimation="None">
                <Border Width="{Binding ActualWidth, ElementName=AddressBox}" MinWidth="240" Background="{DynamicResource Elevated}" BorderBrush="{DynamicResource Stroke}" BorderThickness="1" CornerRadius="12" Padding="7">
                    <ListBox x:Name="SuggestionList" MaxHeight="320" MouseLeftButtonUp="SuggestionList_Click" KeyDown="SuggestionList_KeyDown" AutomationProperties.Name="Lokale adresseforslag">
                        <ListBox.ItemTemplate><DataTemplate><StackPanel Margin="7,8"><TextBlock Text="{Binding Title}" FontWeight="SemiBold" TextTrimming="CharacterEllipsis" /><TextBlock Text="{Binding Detail}" Style="{StaticResource MutedText}" TextTrimming="CharacterEllipsis" Margin="0,4,0,0" /></StackPanel></DataTemplate></ListBox.ItemTemplate>
                    </ListBox>
                </Border>
            </Popup>
''' + x[pos:]
pos = x.index('                        <ScrollViewer x:Name="SettingsScroller"')
x = x[:pos] + '''                        <Grid x:Name="DownloadsPanel" Grid.Row="1" Visibility="Collapsed">
                            <Grid.RowDefinitions><RowDefinition Height="Auto" /><RowDefinition Height="*" /></Grid.RowDefinitions>
                            <TextBlock x:Name="DownloadsHint" Text="Ingen nedlastinger i denne økten." Style="{StaticResource MutedText}" Margin="0,0,0,16" />
                            <ListBox x:Name="DownloadList" Grid.Row="1" ScrollViewer.VerticalScrollBarVisibility="Auto" HorizontalContentAlignment="Stretch" AutomationProperties.Name="Nedlastinger">
                                <ListBox.ItemTemplate><DataTemplate><Border Background="{DynamicResource Field}" CornerRadius="12" Padding="14" Margin="0,4,0,8"><StackPanel>
                                    <TextBlock Text="{Binding FileName}" TextTrimming="CharacterEllipsis" FontWeight="SemiBold" />
                                    <TextBlock Text="{Binding Status}" Style="{StaticResource MutedText}" Margin="0,8,0,8" />
                                    <ProgressBar Height="3" Maximum="100" Value="{Binding Percent}" IsIndeterminate="{Binding UnknownSize}" />
                                    <WrapPanel Margin="0,10,0,0"><Button Content="{Binding ControlLabel}" IsEnabled="{Binding IsActive}" Click="DownloadPause_Click" Padding="9,6" /><Button Content="Avbryt" IsEnabled="{Binding IsActive}" Click="DownloadCancel_Click" Padding="9,6" /><Button Content="Vis i mappe" IsEnabled="{Binding IsComplete}" Click="DownloadReveal_Click" Padding="9,6" /></WrapPanel>
                                </StackPanel></Border></DataTemplate></ListBox.ItemTemplate>
                            </ListBox>
                        </Grid>
''' + x[pos:]
put(S+'MainWindow.xaml',x)

# Keep logging, but delegate profile I/O to a testable repository.
t = read(S+'Services/StateStore.vb')
t = t.replace('Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NOVA-Browser")','ResolveDataFolder()',1)
t = t.replace('        Private Sub New()', '''        Private Shared ReadOnly Repository As New ProfileRepository(DataFolder)
        Private Shared Function ResolveDataFolder() As String
            Dim custom = Environment.GetEnvironmentVariable("NOVA_PROFILE_ROOT")
            If Not String.IsNullOrWhiteSpace(custom) AndAlso Path.IsPathRooted(custom) Then Return Path.GetFullPath(custom)
            Return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NOVA-Browser")
        End Function
        Private Sub New()''',1)
a=t.index('        Public Shared Function Load()'); b=t.index('        Public Shared Sub LogError',a)
t=t[:a]+'''        Public Shared Function Load() As AppState
            Try
                Dim state = Repository.Load()
                LoadWarning = Repository.Warning
                Return state
            Catch ex As Exception
                LogError(ex)
                LoadWarning = "Profilen kunne ikke leses. Kontroller tilgang til datamappen."
                Return New AppState()
            End Try
        End Function
        Public Shared Function Save(state As AppState) As Boolean
            Try
                Repository.Save(state)
                Return True
            Catch ex As Exception
                LogError(ex)
                Return False
            End Try
        End Function
        Public Shared Function ForgetRecoveryCopies() As Boolean
            Try
                Repository.ForgetRecoveryCopies()
                Return True
            Catch ex As Exception
                LogError(ex)
                Return False
            End Try
        End Function

'''+t[b:]
put(S+'Services/StateStore.vb',t)

# Main lifecycle and session restore.
f=S+'MainWindow.xaml.vb'
edit(f,'            InitializePreferences()','            InitializePreferences()\n            InitializeWorkbench()')
edit(f,'            _clock.Start()', '''            _clock.Start()
            If _state.Settings.StartMaximized Then WindowState = WindowState.Maximized
            UpdateWorkbenchLayout()''')
edit(f,'            If _state.Settings.RestoreSession Then\n                For Each saved', '''            Dim restore = _state.Settings.RestoreSession
            If restore AndAlso Not _state.LastExitClean AndAlso _state.Session.Count > 0 Then
                restore = MessageBox.Show(Me, "NOVA ble ikke avsluttet normalt. Gjenopprette de vanlige fanene? Velg Nei for en ren start. Private faner gjenopprettes aldri.", "Gjenopprett faner", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) = MessageBoxResult.Yes
            End If
            If restore Then
                For Each saved''')
edit(f,'                    If browserTab IsNot Nothing Then browserTab.Title = UrlPolicy.CleanTitle(saved.Title)', '''                    If browserTab IsNot Nothing Then
                        browserTab.Title = UrlPolicy.CleanTitle(saved.Title)
                        browserTab.IsPinned = saved.IsPinned
                        browserTab.IsMuted = saved.IsMuted
                        browserTab.Zoom = saved.Zoom
                    End If''')
edit(f,'            _restoring = False\n            TabList.SelectedIndex','            _state.LastExitClean = False\n            _restoring = False\n            TabList.SelectedIndex')
edit(f,'            _active = TryCast(TabList.SelectedItem, BrowserTab)', '''            CloseAddressSuggestions()
            If _active IsNot Nothing Then _active.LastActiveUtc = DateTimeOffset.UtcNow
            _active = TryCast(TabList.SelectedItem, BrowserTab)
            WakeTab(_active)''')
edit(f,'            If replaceAddress OrElse Not AddressBox.IsKeyboardFocusWithin Then AddressBox.Text = _active.Address', '''            If replaceAddress OrElse Not AddressBox.IsKeyboardFocusWithin Then
                _updatingAddress = True
                AddressBox.Text = _active.Address
                _updatingAddress = False
            End If''')
edit(f,'            Try\n                Dim address = UrlPolicy.Resolve(input', '            CloseAddressSuggestions()\n            WakeTab(browserTab)\n            Try\n                Dim address = UrlPolicy.Resolve(input')
edit(f,'            browserTab.NavigationVersion += 1\n            browserTab.IsHome = True', '            CloseAddressSuggestions()\n            WakeTab(browserTab)\n            browserTab.NavigationVersion += 1\n            browserTab.IsHome = True')
edit(f,'            If browserTab.View?.CoreWebView2 IsNot Nothing Then browserTab.View.CoreWebView2.Navigate("about:blank")', '''            If browserTab.View?.CoreWebView2 IsNot Nothing Then
                Try
                    browserTab.HomeBlankPending = True
                    browserTab.View.CoreWebView2.Stop()
                    browserTab.View.CoreWebView2.Navigate("about:blank")
                Catch ex As Exception
                    StateStore.LogError(ex)
                    ReleaseTabView(browserTab)
                End Try
            End If''')
edit(f,'        Private Async Function InitializeViewAsync(browserTab As BrowserTab) As Task(Of Boolean)\n            Try','        Private Async Function InitializeViewAsync(browserTab As BrowserTab) As Task(Of Boolean)\n            Dim epoch = browserTab.ViewEpoch\n            Try')
edit(f,'                Dim env = Await _environment\n                If browserTab.Closed OrElse _isClosing Then Return False','                Dim env = Await _environment\n                If browserTab.Closed OrElse _isClosing OrElse epoch <> browserTab.ViewEpoch Then Return False')
edit(f,'                If browserTab.Closed OrElse _isClosing Then\n                    ReleaseTabView(browserTab)\n                    Return False\n                End If\n                ConfigureWebView', '''                If browserTab.Closed OrElse _isClosing OrElse epoch <> browserTab.ViewEpoch OrElse browserTab.View IsNot view Then
                    If browserTab.View Is view Then ReleaseTabView(browserTab)
                    Return False
                End If
                ConfigureWebView''')
edit(f,'            Catch ex As Exception\n                If _environment IsNot Nothing','            Catch ex As Exception\n                If epoch <> browserTab.ViewEpoch Then Return False\n                If _environment IsNot Nothing')
edit(f,'            core.Settings.AreHostObjectsAllowed','            core.IsMuted = browserTab.IsMuted\n            core.Settings.AreHostObjectsAllowed')
edit(f,'            If browserTab.IsHome AndAlso e.Uri = "about:blank" Then Return', '''            If e.Uri = "about:blank" AndAlso browserTab.HomeBlankPending Then
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
            If Not e.IsRedirected Then browserTab.HasSensitivePermission = False''')
edit(f,'            If browserTab.Closed OrElse _isClosing OrElse browserTab.IsHome Then Return\n            browserTab.IsLoading = False','            If browserTab.Closed OrElse _isClosing OrElse browserTab.IsHome OrElse e.NavigationId <> browserTab.CurrentNavigationId Then Return\n            browserTab.IsLoading = False')
edit(f,'            If _downloads.Values.Any(Function(t) t Is browserTab) Then', '''            If browserTab.IsPinned Then
                If Not Await AskAsync("Lukk festet fane?", "Denne fanen er festet. Vil du likevel lukke den?", "Lukk fane") Then Return
            End If
            If _downloads.Values.Any(Function(t) t Is browserTab) Then''')
edit(f,'.Title = browserTab.Title, .Url = browserTab.Address}', '.Title = browserTab.Title, .Url = browserTab.Address, .IsPinned = browserTab.IsPinned, .IsMuted = browserTab.IsMuted, .Zoom = browserTab.Zoom}')
edit(f,'            browserTab.NavigationVersion += 1\n            ReleaseTabView(browserTab)','            browserTab.NavigationVersion += 1\n            ForgetTabDownloads(browserTab)\n            ReleaseTabView(browserTab)')
edit(f,'            CreateTab(saved.Url)\n        End Sub', '''            Dim target = CreateTab(saved.Url, False, False)
            If target Is Nothing Then
                _closedTabs.Push(saved)
                Return
            End If
            target.IsPinned = saved.IsPinned
            target.IsMuted = saved.IsMuted
            target.Zoom = saved.Zoom
            If target.IsPinned Then _tabs.Move(_tabs.IndexOf(target), _tabs.Where(Function(item) item.IsPinned).Count() - 1)
            TabList.SelectedItem = target
        End Sub''')
edit(f,'        Private Sub PersistState()\n            If _fatalShutdown OrElse Not _sessionInitialized Then Return', '        Private Function PersistState() As Boolean\n            If _fatalShutdown OrElse Not _sessionInitialized Then Return False')
edit(f,'New SessionEntry With {.Title = t.Title, .Url = If(t.IsHome, UrlPolicy.HomeUrl, t.Address)}','New SessionEntry With {.Title = t.Title, .Url = If(t.IsHome, UrlPolicy.HomeUrl, t.Address), .IsPinned = t.IsPinned, .IsMuted = t.IsMuted, .Zoom = t.Zoom}')
edit(f,'            If Not StateStore.Save(_state) AndAlso Not _isClosing Then ShowToast("Kunne ikke lagre innstillingene. Kontroller plass og tilgang til datamappen.")\n        End Sub', '''            Dim saved = StateStore.Save(_state)
            If Not saved AndAlso Not _isClosing Then ShowToast("Kunne ikke lagre innstillingene. Kontroller plass og tilgang til datamappen.")
            Return saved
        End Function''')
edit(f,'            If Not _fatalShutdown Then PersistState()', '''            If Not _fatalShutdown Then
                _state.LastExitClean = True
                PersistState()
            End If''')
edit(f,'            _isClosing = True\n            _clock.Stop()','            _isClosing = True\n            StopWorkbench()\n            _clock.Stop()')
edit(f,'                browserTab.Closed = True\n                ReleaseTabView(browserTab)','                browserTab.Closed = True\n                ForgetTabDownloads(browserTab)\n                ReleaseTabView(browserTab)')
edit(f,'            _fatalShutdown = True\n            _allowClose = True','            _fatalShutdown = True\n            StopWorkbench()\n            _allowClose = True')
edit(f,'            Dim view As WebView2CompositionControl = browserTab.View\n            browserTab.View = Nothing', '''            Dim view As WebView2CompositionControl = browserTab.View
            browserTab.ViewEpoch += 1
            browserTab.Initialization = Nothing
            browserTab.IsSleeping = False
            browserTab.IsPlayingAudio = False
            browserTab.View = Nothing''')

# Preferences, shortcuts and drawers.
f=S+'MainWindow.Interactions.vb'
edit(f,'            CompactSetting.IsChecked = s.CompactTabs', '            CompactSetting.IsChecked = s.CompactTabs\n            MaximizedSetting.IsChecked = s.StartMaximized\n            SleepSetting.IsChecked = s.SleepingTabs')
edit(f,'            s.CompactTabs = CompactSetting.IsChecked.GetValueOrDefault()', '            s.CompactTabs = CompactSetting.IsChecked.GetValueOrDefault()\n            s.StartMaximized = MaximizedSetting.IsChecked.GetValueOrDefault()\n            s.SleepingTabs = SleepSetting.IsChecked.GetValueOrDefault()\n            ApplySleepPreference()')
edit(f,'            ThemeManager.Apply(Application.Current.Resources, s)', '            ThemeManager.Apply(Application.Current.Resources, s)\n            UpdateNativeTitleBar()')
edit(f,'        Private Sub ShowDrawer(mode As String)\n', '        Private Sub ShowDrawer(mode As String)\n            CloseAddressSuggestions()\n')
edit(f,'            LibraryPanel.Visibility = If(mode = "settings", Visibility.Collapsed, Visibility.Visible)\n            If mode <> "settings" Then RefreshLibrary()', '''            LibraryPanel.Visibility = If(mode = "bookmarks" OrElse mode = "history", Visibility.Visible, Visibility.Collapsed)
            DownloadsPanel.Visibility = If(mode = "downloads", Visibility.Visible, Visibility.Collapsed)
            If mode = "downloads" Then
                DrawerTitle.Text = "Dine nedlastinger."
                RefreshDownloads()
            ElseIf mode <> "settings" Then
                RefreshLibrary()
            End If''')
edit(f,'            Else\n                LibraryList.Focus()\n            End If\n        End Sub\n\n        Private Sub RefreshLibrary', '            ElseIf mode = "downloads" Then\n                DownloadList.Focus()\n            Else\n                LibraryList.Focus()\n            End If\n        End Sub\n\n        Private Sub RefreshLibrary')
edit(f,'            DrawerLayer.IsEnabled = Not (confirming', '            ShellHeader.IsEnabled = ShellBody.IsEnabled\n            DrawerLayer.IsEnabled = Not (confirming')
edit(f,'                Else\n                    LibraryList.Focus()','                ElseIf _drawerMode = "downloads" Then\n                    DownloadList.Focus()\n                Else\n                    LibraryList.Focus()')
edit(f,'        Private Sub ShowCommands()\n', '        Private Sub ShowCommands()\n            CloseAddressSuggestions()\n')
edit(f,'            For Each browserTab In _tabs\n                Dim target = browserTab', '''            commands.Add(New CommandItem With {.Label = "Fokusmodus", .Hint = "Ctrl+Shift+F", .Execute = AddressOf ToggleFocusMode})
            commands.Add(New CommandItem With {.Label = "La bakgrunnsfaner hvile", .Hint = "Pause nettsider", .Execute = Async Sub() Await SleepBackgroundTabsAsync()})
            commands.Add(New CommandItem With {.Label = "Fest eller løsne aktiv fane", .Hint = "Fane", .Execute = Sub() TogglePinned(_active)})
            commands.Add(New CommandItem With {.Label = "Demp eller slå på lyd", .Hint = "Fane", .Execute = Sub() ToggleMuted(_active)})
            commands.Add(New CommandItem With {.Label = "Dupliser aktiv fane", .Hint = "Fane", .Execute = Sub() DuplicateTab(_active)})
            commands.Add(New CommandItem With {.Label = "Lagre nettsiden som PDF", .Hint = "Eksporter", .Execute = Async Sub() Await SavePagePdfAsync()})
            commands.Add(New CommandItem With {.Label = "Skriv ut nettsiden", .Hint = "Ctrl+P", .Execute = AddressOf PrintCurrentPage})
            For Each browserTab In _tabs
                If _active IsNot Nothing AndAlso browserTab.IsPrivate <> _active.IsPrivate Then Continue For
                Dim target = browserTab''')
method(f,'        Private Sub ToggleSidebar()', '        Private Sub ChangeZoom(', '''        Private Sub ToggleSidebar()
            _sidebarManual = True
            _sidebarVisible = Sidebar.Visibility <> Visibility.Visible
            _focusMode = False
            FocusModeButton.Content = "Fokus"
            UpdateWorkbenchLayout()
        End Sub

''')
edit(f,'            If ConfirmLayer.Visibility = Visibility.Visible Then\n                If pressedKey','            If pressedKey = Key.F4 AndAlso (Keyboard.Modifiers And ModifierKeys.Alt) <> 0 Then Return\n            If ConfirmLayer.Visibility = Visibility.Visible Then\n                If pressedKey')
edit(f,'                    Case Key.R : action = AddressOf ReloadCurrent', '                    Case Key.R : action = AddressOf ReloadCurrent\n                    Case Key.P : action = AddressOf PrintCurrentPage\n                    Case Key.F\n                        If shift Then action = AddressOf ToggleFocusMode')
method(f,'        Private Async Sub AddressBox_KeyDown(', '        Private Sub AddressBox_GotKeyboardFocus', '''        Private Async Sub AddressBox_KeyDown(sender As Object, e As KeyEventArgs)
            If e.Key = Key.Escape Then
                CloseAddressSuggestions()
                UpdateChrome(True)
                FocusActivePage()
                e.Handled = True
                Return
            End If
            If AddressSuggestions.IsOpen AndAlso (e.Key = Key.Down OrElse e.Key = Key.Up) Then
                Dim direction = If(e.Key = Key.Down, 1, -1)
                SuggestionList.SelectedIndex = Math.Clamp(SuggestionList.SelectedIndex + direction, 0, Math.Max(0, SuggestionList.Items.Count - 1))
                If SuggestionList.SelectedItem IsNot Nothing Then SuggestionList.ScrollIntoView(SuggestionList.SelectedItem)
                e.Handled = True
                Return
            End If
            If e.Key <> Key.Enter Then Return
            e.Handled = True
            If AddressSuggestions.IsOpen AndAlso SuggestionList.SelectedItem IsNot Nothing Then
                Await AcceptAddressSuggestionAsync()
            Else
                CloseAddressSuggestions()
                Await NavigateAsync(_active, AddressBox.Text)
            End If
        End Sub
''')
method(f,'        Private Sub Window_StateChanged(', '        Private Sub Connection_Click(', '''        Private Sub Window_StateChanged(sender As Object, e As EventArgs)
            If WindowFrame Is Nothing Then Return
            ConfigureAmbientMotion()
            UpdateWorkbenchLayout()
        End Sub
''')
method(f,'        Private Async Function ShowDownloadsAsync()', '    End Class', '''        Private Function ShowDownloadsAsync() As Task
            ShowDrawer("downloads")
            Return Task.CompletedTask
        End Function
''')

f=S+'MainWindow.Privacy.vb'
edit(f,'                Dim requestPage = browserTab.View.CoreWebView2.Source','                Dim requestPage = browserTab.View.CoreWebView2.Source\n                Dim requestVersion = browserTab.DocumentVersion')
edit(f,'browserTab.View.CoreWebView2.Source = requestPage Then','browserTab.View.CoreWebView2.Source = requestPage AndAlso browserTab.DocumentVersion = requestVersion Then')
edit(f,'                    e.State = CoreWebView2PermissionState.Allow','                    e.State = CoreWebView2PermissionState.Allow\n                    browserTab.HasSensitivePermission = True')
edit(f,'        Private Function AskAsync(title As String, detail As String, yesText As String) As Task(Of Boolean)\n','        Private Function AskAsync(title As String, detail As String, yesText As String) As Task(Of Boolean)\n            CloseAddressSuggestions()\n')
edit(f,'            PersistState()\n            RefreshLibrary()', '''            If Not PersistState() Then Return
            If Not StateStore.ForgetRecoveryCopies() Then
                ShowToast("Historikken er tom, men en sikkerhetskopi kunne ikke slettes. Se feilloggen.")
                Return
            End If
            RefreshLibrary()''')
edit(f,'                PersistState()\n                ShowToast', '''                If Not PersistState() Then Return
                If Not StateStore.ForgetRecoveryCopies() Then
                    ShowToast("Nettstedsdata er slettet, men en profilkopi kunne ikke slettes. Se feilloggen.")
                    Return
                End If
                ShowToast''')

# Keep source assertions relevant rather than asserting obsolete custom controls.
t=read('tools/check_source.py').replace('0.2.0','0.3.0').replace("'MaximizeIcon'","'FocusModeButton'").replace('len(kinds) == 28','len(kinds) == 32').replace('All 28 vector','All 32 vector')
t=t.replace("events = {'Loaded'", "events = {'PreviewMouseRightButtonUp','MouseLeftButtonUp','Loaded'")
t=t.replace("for path in sorted(ROOT.rglob('*')):", "for path in sorted(ROOT.rglob('*')):\n        if any(part in {'bin','obj','out','.git','artifacts'} for part in path.parts): continue")
t=t.replace('if p.is_file()))', "if p.is_file() and not any(part in {'bin','obj','out','.git','artifacts'} for part in p.parts)))")
put('tools/check_source.py',t)
t=read('tools/check_package.py').replace("check('original app version preserved','<Version>0.2.0</Version>'", "check('app version 0.3.0','<Version>0.3.0</Version>'")
put('tools/check_package.py',t)
for project in ['tests/NovaBrowser.Checks/NovaBrowser.Checks.vbproj','tests/NovaBrowser.UiChecks/NovaBrowser.UiChecks.vbproj']:
    block=''
    for name in ['BrowserPolicies','ProfileRepository']:
        block += f'    <Compile Include="../../src/NovaBrowser/Services/{name}.vb" Link="{name}.vb" />\n'
    edit(project,'  </ItemGroup>',block+'  </ItemGroup>')
edit('tests/NovaBrowser.Checks/Program.vb','                CheckSetupFlow()','                CheckSetupFlow()\n                _passed += FeatureChecks.Run()')
# A separate script guards stale engine callbacks and process-failure recovery.
exec(compile(read('engineering/engine03.py'), 'engineering/engine03.py', 'exec'))
for name in [S+'Program.vb', S+'Services/ErrorDiagnostics.vb', S+'NovaBrowser.vbproj']:
    put(name, read(name).replace('0.2.0','0.3.0'))
print('0.3.0 source migration applied; compilation and runtime tests still required')
