Imports System
Imports System.Collections.ObjectModel
Imports System.Diagnostics
Imports System.IO
Imports System.Linq
Imports System.Net
Imports System.Net.Sockets
Imports System.Text
Imports System.Threading
Imports System.Threading.Tasks
Imports System.Windows
Imports System.Windows.Controls
Imports NovaBrowser

Namespace NovaBrowser.ShellChecks
    Friend NotInheritable Class LiveChecks
        Public Shared Sub Run(window As MainWindow, output As String)
            Dim expected = Environment.GetEnvironmentVariable("NOVA_PROFILE_ROOT")
            Program.Check("live tests use isolated profile", Not String.IsNullOrEmpty(expected) AndAlso String.Equals(StateStore.DataFolder, expected, StringComparison.OrdinalIgnoreCase) AndAlso expected.StartsWith(Path.GetTempPath(), StringComparison.OrdinalIgnoreCase))
            Using server As New LocalFixture()
                Dim tabList = DirectCast(window.FindName("TabList"), ListBox)
                Dim tabs = DirectCast(tabList.ItemsSource, ObservableCollection(Of BrowserTab))
                Dim normal = DirectCast(Program.CallMethod(window, "CreateTab", UrlPolicy.HomeUrl, False, True), BrowserTab)
                Navigate(window, normal, server.BaseAddress & "a", "NOVA test A")
                Dim core = normal.View.CoreWebView2
                Console.WriteLine("WebView2 runtime: " & core.Environment.BrowserVersionString)
                Program.Check("host objects disabled", Not core.Settings.AreHostObjectsAllowed)
                Program.Check("web messages disabled", Not core.Settings.IsWebMessageEnabled)
                Program.Check("password autosave disabled", Not core.Profile.IsPasswordAutosaveEnabled)
                Program.AwaitResult(core.ExecuteScriptAsync("localStorage.setItem('nova_test','normal'); 'ok'"), "set normal storage")
                Navigate(window, normal, server.BaseAddress & "b", "NOVA test B")
                Program.Check("native back history available", core.CanGoBack)
                Program.CallMethod(window, "NavigateBack")
                WaitForPage(normal, "NOVA test A")
                Program.Check("back updates visible address", normal.Address = server.BaseAddress & "a")
                Program.CallMethod(window, "ShowHome", normal)
                Program.UntilTrue(Function() core.Source = "about:blank", "home blanks old page")
                Program.Check("home keeps local NOVA address", normal.IsHome AndAlso normal.Address = UrlPolicy.HomeUrl)
                Program.CallMethod(window, "NavigateBack")
                WaitForPage(normal, "NOVA test A")
                Program.Check("back from home returns to page", Not normal.IsHome)
                Program.CallMethod(window, "ToggleMuted", normal)
                Program.Check("mute reaches live engine", core.IsMuted)
                Program.CallMethod(window, "ToggleMuted", normal)
                Program.Check("unmute reaches live engine", Not core.IsMuted)
                Dim privateTab = DirectCast(Program.CallMethod(window, "CreateTab", UrlPolicy.HomeUrl, True, True), BrowserTab)
                Navigate(window, privateTab, server.BaseAddress & "a", "NOVA test A")
                Program.Check("private engine mode enabled", privateTab.View.CoreWebView2.Profile.IsInPrivateModeEnabled)
                Dim privateValue = Program.AwaitResult(privateTab.View.CoreWebView2.ExecuteScriptAsync("localStorage.getItem('nova_test')"), "read private storage")
                Program.Check("private page cannot read normal storage", privateValue = "null")
                Program.AwaitResult(privateTab.View.CoreWebView2.ExecuteScriptAsync("localStorage.setItem('nova_test','private'); 'ok'"), "set private storage")
                tabList.SelectedItem = normal
                Program.Pump(100)
                Program.Check("normal storage unaffected by private page", Program.AwaitResult(core.ExecuteScriptAsync("localStorage.getItem('nova_test')"), "read normal storage") = """normal""")
                Program.CallMethod(window, "RemoveTab", privateTab)
                Dim freshPrivate = DirectCast(Program.CallMethod(window, "CreateTab", UrlPolicy.HomeUrl, True, True), BrowserTab)
                Navigate(window, freshPrivate, server.BaseAddress & "a", "NOVA test A")
                Program.Check("new private session starts empty", Program.AwaitResult(freshPrivate.View.CoreWebView2.ExecuteScriptAsync("localStorage.getItem('nova_test')"), "new private storage") = "null")
                Program.CallMethod(window, "RemoveTab", freshPrivate)
                Dim home = DirectCast(Program.CallMethod(window, "CreateTab", UrlPolicy.HomeUrl, False, True), BrowserTab)
                Program.Pump(100)
                Program.AwaitTask(DirectCast(Program.CallMethod(window, "SleepTabAsync", normal), Task), "suspend hidden tab")
                Program.Check("suspend state agrees with engine", normal.IsSleeping = core.IsSuspended)
                Console.WriteLine("Hidden test page suspended: " & core.IsSuspended.ToString())
                tabList.SelectedItem = normal
                Program.Pump(100)
                Program.Check("activated page resumed", Not core.IsSuspended AndAlso Not normal.IsSleeping)
                Program.Check("page storage survives suspension", Program.AwaitResult(core.ExecuteScriptAsync("localStorage.getItem('nova_test')"), "storage after resume") = """normal""")
                Dim transient = DirectCast(Program.CallMethod(window, "CreateTab", UrlPolicy.HomeUrl, False, False), BrowserTab)
                Dim initializing = DirectCast(Program.CallMethod(window, "NavigateAsync", transient, server.BaseAddress & "b"), Task)
                Program.CallMethod(window, "RemoveTab", transient)
                Program.AwaitTask(initializing, "closed initialization settles")
                Program.Check("closing during initialization releases view", transient.Closed AndAlso transient.View Is Nothing)
                Navigate(window, normal, server.BaseAddress & "b", "NOVA test B")
                Program.CaptureClient(window, Path.Combine(output, "nova-03-loopback-page-client.png"))
                NativeSnapshot.Capture(window, Path.Combine(output, "nova-03-loopback-page-window.png"))
                ' Deliberately terminate ONLY the WebView2 browser that owns this
                ' unique temporary user-data directory. Never enumerate or kill
                ' processes by name, and never use the user's real profile.
                Dim pid = CInt(normal.View.CoreWebView2.BrowserProcessId)
                Using browser = Process.GetProcessById(pid)
                    browser.Kill(True)
                    If Not browser.WaitForExit(5000) Then Throw New TimeoutException("isolated test engine exit")
                End Using
                Program.UntilTrue(Function() normal.View Is Nothing AndAlso normal.ErrorMessage.Length > 0, "renderer failure handled")
                Program.Check("engine crash keeps window and tab metadata", window.IsVisible AndAlso tabs.Contains(normal) AndAlso Not normal.Closed AndAlso normal.Address = server.BaseAddress & "b")
                Program.CallMethod(window, "Retry_Click", window, New RoutedEventArgs())
                WaitForPage(normal, "NOVA test B")
                Program.Check("retry creates working engine after crash", normal.View.CoreWebView2.BrowserProcessId <> CUInt(pid))
                Program.Check("normal storage retained after engine restart", Program.AwaitResult(normal.View.CoreWebView2.ExecuteScriptAsync("localStorage.getItem('nova_test')"), "storage after engine restart") = """normal""")
                Program.Check("application still usable after restart", CBool(Program.CallMethod(window, "PersistState")))
                Console.WriteLine("PASS: real WebView2 smoke test; HTTP fixture bound only to 127.0.0.1, no public websites visited.")
            End Using
        End Sub
        Private Shared Sub Navigate(window As MainWindow, target As BrowserTab, address As String, title As String)
            Program.AwaitTask(DirectCast(Program.CallMethod(window, "NavigateAsync", target, address), Task), "initialize navigation")
            Program.Check("engine initialized for fixture", target.View?.CoreWebView2 IsNot Nothing AndAlso target.ErrorMessage.Length = 0)
            WaitForPage(target, title)
        End Sub
        Private Shared Sub WaitForPage(target As BrowserTab, title As String)
            Program.UntilTrue(Function() target.View?.CoreWebView2 IsNot Nothing AndAlso Not target.IsLoading AndAlso target.Title = title AndAlso target.ErrorMessage.Length = 0, "wait for " & title, 40)
        End Sub

        Private NotInheritable Class LocalFixture
            Implements IDisposable
            Private ReadOnly _listener As New TcpListener(IPAddress.Loopback, 0)
            Private ReadOnly _stop As New CancellationTokenSource()
            Private ReadOnly _loop As Task
            Public ReadOnly Property BaseAddress As String
            Public Sub New()
                _listener.Start()
                BaseAddress = "http://127.0.0.1:" & DirectCast(_listener.LocalEndpoint, IPEndPoint).Port.ToString() & "/"
                _loop = Task.Run(AddressOf ServeAsync)
            End Sub
            Private Async Function ServeAsync() As Task
                While Not _stop.IsCancellationRequested
                    Try
                        Using client = Await _listener.AcceptTcpClientAsync(_stop.Token)
                            Using stream = client.GetStream(), reader As New StreamReader(stream, Encoding.ASCII, False, 4096, True)
                                Dim first = Await reader.ReadLineAsync(_stop.Token)
                                If first Is Nothing Then Continue While
                                Dim header As String
                                Do
                                    header = Await reader.ReadLineAsync(_stop.Token)
                                Loop While Not String.IsNullOrEmpty(header)
                                Dim parts = first.Split(" "c)
                                Dim path = If(parts.Length > 1, parts(1), "/a")
                                Dim page = If(path.StartsWith("/b", StringComparison.Ordinal), "B", "A")
                                Dim html = "<!doctype html><html lang='no'><meta charset='utf-8'><title>NOVA test " & page & "</title><style>body{margin:0;min-height:100vh;display:grid;place-items:center;background:linear-gradient(130deg,#111525,#25203d);color:#f2f1ff;font:18px system-ui}main{max-width:720px;padding:60px}small{color:#b7a3ff;letter-spacing:3px}h1{font-size:56px;line-height:1.05}p{color:#b6bfd2;line-height:1.7}a{color:#c4b8ff;margin-right:24px}</style><main><small>NOVA / LOKALT TESTMILJØ</small><h1>Nettsider. På ordentlig.</h1><p>Dette er en kontrollert testside fra 127.0.0.1, vist av den faktiske WebView2-nettmotoren. Ingen offentlig nettside eller brukerdata inngår i testen.</p><a href='/a'>Testside A</a><a href='/b'>Testside B</a><p>Aktiv testside: " & page & "</p></main></html>"
                                Dim body = If(path.StartsWith("/favicon", StringComparison.Ordinal), Array.Empty(Of Byte)(), Encoding.UTF8.GetBytes(html))
                                Dim response = Encoding.ASCII.GetBytes("HTTP/1.1 200 OK" & vbCrLf & "Content-Type: text/html; charset=utf-8" & vbCrLf & "Content-Length: " & body.Length.ToString() & vbCrLf & "Cache-Control: no-store" & vbCrLf & "Connection: close" & vbCrLf & vbCrLf)
                                Await stream.WriteAsync(response, 0, response.Length, _stop.Token)
                                Await stream.WriteAsync(body, 0, body.Length, _stop.Token)
                            End Using
                        End Using
                    Catch ex As OperationCanceledException
                        Exit While
                    Catch ex As ObjectDisposedException
                        Exit While
                    Catch ex As IOException
                        ' A canceled browser navigation can close its socket early.
                    Catch ex As SocketException
                        If _stop.IsCancellationRequested Then Exit While
                    End Try
                End While
            End Function
            Public Sub Dispose() Implements IDisposable.Dispose
                _stop.Cancel()
                _listener.Stop()
                _loop.Wait(3000)
                _stop.Dispose()
            End Sub
        End Class
    End Class
End Namespace
