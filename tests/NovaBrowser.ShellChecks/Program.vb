Imports System
Imports System.Collections.ObjectModel
Imports System.Diagnostics
Imports System.IO
Imports System.Linq
Imports System.Reflection
Imports System.Threading
Imports System.Threading.Tasks
Imports System.Windows
Imports System.Windows.Controls
Imports System.Windows.Input
Imports System.Windows.Media
Imports System.Windows.Media.Imaging
Imports System.Windows.Shell
Imports System.Windows.Threading
Imports NovaBrowser

Namespace NovaBrowser.ShellChecks
    Public Module Program
        Private _passed As Integer
        Private _asyncError As Exception
        <STAThread>
        Public Function Main(args As String()) As Integer
            Dim profile = Path.Combine(Path.GetTempPath(), "NOVA-shell-tests-" & Guid.NewGuid().ToString("N"))
            Environment.SetEnvironmentVariable("NOVA_PROFILE_ROOT", profile)
            Dim window As MainWindow = Nothing
            Dim app As Application = Nothing
            Try
                Dim initial As New AppState()
                initial.Settings.SetupCompleted = True
                initial.Settings.StartMaximized = False
                initial.Settings.Animations = False
                initial.Settings.DisplayName = "David"
                Dim repository As New ProfileRepository(profile)
                repository.Save(initial)
                app = New Application With {.ShutdownMode = ShutdownMode.OnExplicitShutdown}
                SynchronizationContext.SetSynchronizationContext(New DispatcherSynchronizationContext(app.Dispatcher))
                AddHandler app.DispatcherUnhandledException,
                    Sub(sender, eventArgs)
                        _asyncError = eventArgs.Exception
                        eventArgs.Handled = True
                    End Sub
                app.Resources.MergedDictionaries.Add(New ResourceDictionary With {.Source = New Uri("pack://application:,,,/NOVA;component/UI/Styles.xaml")})
                window = New MainWindow()
                window.Show()
                window.Activate()
                Pump(250)
                Check("native window frame", window.WindowStyle = WindowStyle.SingleBorderWindow)
                Check("custom chrome no longer hides caption controls", WindowChrome.GetWindowChrome(window) Is Nothing)
                Check("real min/max/close styles present", NativeSnapshot.HasCaptionButtons(window))
                Check("window can resize", window.ResizeMode = ResizeMode.CanResize)
                window.WindowState = WindowState.Maximized
                Pump(150)
                Check("native maximize works", window.WindowState = WindowState.Maximized)
                window.WindowState = WindowState.Normal
                Pump(150)
                Dim tabList = DirectCast(window.FindName("TabList"), ListBox)
                Dim tabs = DirectCast(tabList.ItemsSource, ObservableCollection(Of BrowserTab))
                Check("initial home tab", tabs.Count = 1 AndAlso tabs(0).IsHome)
                Check("home does not start network engine", tabs(0).View Is Nothing)
                Check("dirty-exit marker written during session", Not repository.Load().LastExitClean)
                Dim second = DirectCast(CallMethod(window, "CreateTab", UrlPolicy.HomeUrl, False, False), BrowserTab)
                CallMethod(window, "TogglePinned", second)
                Check("pin moves tab to first position", tabs(0) Is second AndAlso second.IsPinned)
                CallMethod(window, "ToggleMuted", second)
                Check("mute stored before engine initialization", second.IsMuted)
                CallMethod(window, "DuplicateTab", second)
                Check("duplicate inherits mute", tabs.Count = 3 AndAlso tabs.Last().IsMuted)
                Dim pending = DirectCast(CallMethod(window, "CloseTabAsync", second), Task)
                Check("pinned close requires confirmation", Element(window, "ConfirmLayer").Visibility = Visibility.Visible)
                DirectCast(window.FindName("ConfirmNoButton"), Button).RaiseEvent(New RoutedEventArgs(Button.ClickEvent))
                Pump(100)
                Check("cancel preserves pinned tab", pending.IsCompleted AndAlso tabs.Contains(second))
                CallMethod(window, "ToggleFocusMode")
                Check("focus hides rail", Element(window, "AppRail").Visibility = Visibility.Collapsed)
                CallMethod(window, "ToggleFocusMode")
                Check("focus is reversible", Element(window, "AppRail").Visibility = Visibility.Visible)
                CallMethod(window, "ShowDrawer", "downloads")
                Check("download drawer opens", Element(window, "DownloadsPanel").Visibility = Visibility.Visible)
                Check("downloads do not turn home into about blank", tabs.All(Function(item) item.View Is Nothing))
                CallMethod(window, "CloseDrawer")
                Dim privateTab = DirectCast(CallMethod(window, "CreateTab", "https://private.invalid/never-persist", True, False), BrowserTab)
                Check("profile save succeeds", CBool(CallMethod(window, "PersistState")))
                Check("private URL never serialized", Not File.ReadAllText(Path.Combine(profile, "state.json")).Contains("private.invalid"))
                Check("pins persisted", repository.Load().Session.Any(Function(item) item.IsPinned))
                CallMethod(window, "RemoveTab", privateTab)
                Dim address = DirectCast(window.FindName("AddressBox"), TextBox)
                address.Focus()
                Keyboard.Focus(address)
                address.Text = "GitHub"
                Pump(150)
                Check("local bookmark suggestions populated", DirectCast(window.FindName("SuggestionList"), ListBox).Items.Count > 0)
                CallMethod(window, "CloseAddressSuggestions")
                CallMethod(window, "UpdateChrome", True)
                Dim output = If(args.Length > 0 AndAlso Not args(0).StartsWith("--", StringComparison.Ordinal), Path.GetFullPath(args(0)), Path.Combine(Environment.CurrentDirectory, "artifacts", "wpf"))
                Directory.CreateDirectory(output)
                For Each theme In New String() {"Midnight", "Dawn", "Forest", "Graphite"}
                    DirectCast(window.FindName("ThemeSetting"), ComboBox).SelectedItem = theme
                    Pump(150)
                    CaptureClient(window, Path.Combine(output, "nova-03-" & theme.ToLowerInvariant() & "-client.png"))
                    NativeSnapshot.Capture(window, Path.Combine(output, "nova-03-" & theme.ToLowerInvariant() & "-window.png"))
                Next
                DirectCast(window.FindName("ThemeSetting"), ComboBox).SelectedItem = "Midnight"
                CallMethod(window, "ShowDrawer", "settings")
                Pump(150)
                CaptureClient(window, Path.Combine(output, "nova-03-settings-client.png"))
                CallMethod(window, "CloseDrawer")
                CallMethod(window, "ShowSetup")
                Pump(150)
                CaptureClient(window, Path.Combine(output, "nova-03-setup-client.png"))
                window.Width = 820
                window.Height = 600
                Pump(150)
                Check("wizard fits small screen", Element(window, "SetupCard").ActualHeight <= Element(window, "SetupLayer").ActualHeight)
                Check("wizard next button stays visible", Element(window, "SetupNextButton").IsVisible)
                CallMethod(window, "CancelSetup")
                Check("sidebar auto-collapses on small screens", Element(window, "Sidebar").Visibility = Visibility.Collapsed)
                Check("address remains usable on small screens", address.ActualWidth > 200 AndAlso address.ActualHeight >= 24)
                Pump(150)
                CaptureClient(window, Path.Combine(output, "nova-03-compact-client.png"))
                window.Width = 1280
                window.Height = 820
                Pump(100)
                If args.Contains("--live") Then LiveChecks.Run(window, output)
                window.Close()
                Pump(150)
                Check("normal exit recorded", repository.Load().LastExitClean)
                Check("all web views released on close", tabs.All(Function(item) item.View Is Nothing))
                Console.WriteLine("PASS: " & _passed & " real WPF/engine checks. Only disposable profiles and loopback test pages were used.")
                Console.WriteLine("Client renders are RenderTargetBitmap images. Files ending -window.png are native PrintWindow captures, not mockups.")
                app.Shutdown()
                Return 0
            Catch ex As Exception
                Console.Error.WriteLine(ex.ToString())
                Return 1
            Finally
                If window IsNot Nothing AndAlso window.IsVisible Then window.Close()
                If app IsNot Nothing Then app.Shutdown()
                For attempt As Integer = 1 To 20
                    Try
                        If Directory.Exists(profile) Then Directory.Delete(profile, True)
                        Exit For
                    Catch ex As IOException
                        Thread.Sleep(200)
                    Catch ex As UnauthorizedAccessException
                        Thread.Sleep(200)
                    End Try
                Next
            End Try
        End Function
        Friend Function Element(window As MainWindow, name As String) As FrameworkElement
            Return DirectCast(window.FindName(name), FrameworkElement)
        End Function
        Friend Function CallMethod(window As MainWindow, name As String, ParamArray args As Object()) As Object
            Dim target = GetType(MainWindow).GetMethod(name, BindingFlags.Instance Or BindingFlags.NonPublic)
            If target Is Nothing Then Throw New MissingMethodException(name)
            Return target.Invoke(window, args)
        End Function
        Friend Sub Pump(milliseconds As Integer)
            Dim frame As New DispatcherFrame()
            Dim timer As New DispatcherTimer(DispatcherPriority.Background) With {.Interval = TimeSpan.FromMilliseconds(milliseconds)}
            AddHandler timer.Tick, Sub(sender, args)
                                       timer.Stop()
                                       frame.Continue = False
                                   End Sub
            timer.Start()
            Dispatcher.PushFrame(frame)
            If _asyncError IsNot Nothing Then Throw New InvalidOperationException("Unhandled WPF dispatcher exception", _asyncError)
        End Sub
        Friend Sub UntilTrue(condition As Func(Of Boolean), label As String, Optional seconds As Integer = 20)
            Dim clock = Stopwatch.StartNew()
            While Not condition()
                If clock.Elapsed > TimeSpan.FromSeconds(seconds) Then Throw New TimeoutException(label)
                Pump(50)
            End While
        End Sub
        Friend Sub AwaitTask(task As Task, label As String)
            UntilTrue(Function() task.IsCompleted, label, 40)
            task.GetAwaiter().GetResult()
        End Sub
        Friend Function AwaitResult(Of T)(task As Task(Of T), label As String) As T
            AwaitTask(task, label)
            Return task.GetAwaiter().GetResult()
        End Function
        Friend Sub CaptureClient(window As MainWindow, filePath As String)
            Dim content = DirectCast(window.Content, FrameworkElement)
            content.UpdateLayout()
            Dim bitmap As New RenderTargetBitmap(CInt(Math.Ceiling(content.ActualWidth)), CInt(Math.Ceiling(content.ActualHeight)), 96, 96, PixelFormats.Pbgra32)
            bitmap.Render(content)
            Dim encoder As New PngBitmapEncoder()
            encoder.Frames.Add(BitmapFrame.Create(bitmap))
            Using stream = File.Create(filePath)
                encoder.Save(stream)
            End Using
        End Sub
        Friend Sub Check(label As String, condition As Boolean)
            If Not condition Then Throw New InvalidOperationException(label)
            Console.WriteLine("PASS: " & label)
            _passed += 1
        End Sub
    End Module
End Namespace
