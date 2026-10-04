Imports System
Imports System.IO
Imports System.Text.Json
Imports NovaBrowser

Namespace NovaBrowser.Checks
    Public Module Program
        Private _passed As Integer
        Public Function Main() As Integer
            Try
                Equal("empty input", UrlPolicy.HomeUrl, UrlPolicy.Resolve("", "DuckDuckGo"))
                Equal("null input", UrlPolicy.HomeUrl, UrlPolicy.Resolve(Nothing, "DuckDuckGo"))
                Equal("home URI", UrlPolicy.HomeUrl, UrlPolicy.Resolve("NOVA://HOME", "Google"))
                Equal("bare domain", "https://example.com/", UrlPolicy.Resolve("example.com", "DuckDuckGo"))
                Equal("domain and path", "https://example.com/a?q=1", UrlPolicy.Resolve("example.com/a?q=1", "Google"))
                Equal("explicit HTTPS", "https://example.com/", UrlPolicy.Resolve("https://example.com", "DuckDuckGo"))
                Equal("explicit HTTP", "http://example.com/", UrlPolicy.Resolve("http://example.com", "Google"))
                Equal("localhost", "http://localhost:8080/a", UrlPolicy.Resolve("localhost:8080/a", "Google"))
                Equal("loopback", "http://127.0.0.1:8000/", UrlPolicy.Resolve("127.0.0.1:8000", "Google"))
                Equal("public IP", "https://1.1.1.1/", UrlPolicy.Resolve("1.1.1.1", "Google"))
                Equal("port", "https://example.com:8443/", UrlPolicy.Resolve("example.com:8443", "Google"))
                Equal("trimming", "https://example.com/", UrlPolicy.Resolve("  example.com  ", "Google"))
                Equal("DuckDuckGo query", "https://duckduckgo.com/?q=hello%20world", UrlPolicy.Resolve("hello world", "DuckDuckGo"))
                Equal("Google query", "https://www.google.com/search?q=hello%20world", UrlPolicy.Resolve("hello world", "Google"))
                Equal("Bing query", "https://www.bing.com/search?q=hello%20world", UrlPolicy.Resolve("hello world", "Bing"))
                Equal("numeric query", "https://duckduckgo.com/?q=1234", UrlPolicy.Resolve("1234", "DuckDuckGo"))
                Equal("fallback provider", "https://duckduckgo.com/?q=hello", UrlPolicy.Resolve("hello", "Unknown"))
                Equal("Unicode query", "https://duckduckgo.com/?q=bl%C3%A5%20himmel", UrlPolicy.Resolve("blå himmel", "DuckDuckGo"))
                Equal("escaped operators", "https://duckduckgo.com/?q=a%20%26%20b%3D1", UrlPolicy.Resolve("a & b=1", "DuckDuckGo"))
                For Each blocked In New String() {
                    "javascript:alert(1)", "data:text/html,test", "file:///C:/Windows/win.ini",
                    "about:blank", "mailto:person@example.com", "powershell:run", "nova://other",
                    "C:\Windows\win.ini", "\\server\share\file.txt",
                    "https://user:pass@example.com", "user:pass@example.com", "https://"}
                    Reject(blocked)
                Next
                Reject("example.com" & ChrW(10) & "injected")
                Reject("a" & ChrW(0) & "b")
                Reject(New String("a"c, 8193))
                Check("HTTPS accepted", UrlPolicy.IsWebUrl("https://example.com/"))
                Check("HTTP accepted", UrlPolicy.IsWebUrl("http://localhost/"))
                Check("file rejected", Not UrlPolicy.IsWebUrl("file:///C:/test.txt"))
                Check("credentials rejected", Not UrlPolicy.IsWebUrl("https://a:b@example.com/"))
                Check("blank only for engine", UrlPolicy.IsAllowedNavigation("about:blank"))
                Check("script rejected for engine", Not UrlPolicy.IsAllowedNavigation("javascript:alert(1)"))
                Check("blob top-level rejected", Not UrlPolicy.IsAllowedNavigation("blob:https://example.com/123"))
                Equal("title controls removed", "HelloWorld", UrlPolicy.CleanTitle("Hello" & ChrW(&H202E) & ChrW(10) & "World"))
                Equal("title length cap", 160, UrlPolicy.CleanTitle(New String("x"c, 500)).Length)
                Equal("missing title", "", UrlPolicy.CleanTitle(Nothing))
                Dim state As New AppState()
                state.Settings.Theme = "Forest"
                state.Settings.Accent = "Mint"
                state.Settings.DisplayName = "David"
                state.Bookmarks.Clear()
                state.Bookmarks.Add(New PageEntry With {.Title = "Eksempel", .Url = "https://example.com/"})
                state.Session.Add(New SessionEntry With {.Title = "Ny fane", .Url = UrlPolicy.HomeUrl})
                Dim copy = JsonSerializer.Deserialize(Of AppState)(JsonSerializer.Serialize(state))
                Check("state round-trip", copy IsNot Nothing)
                Equal("theme round-trip", "Forest", copy.Settings.Theme)
                Equal("accent round-trip", "Mint", copy.Settings.Accent)
                Equal("bookmark round-trip", "https://example.com/", copy.Bookmarks(0).Url)
                Equal("session round-trip", UrlPolicy.HomeUrl, copy.Session(0).Url)
                Dim missing As New FileNotFoundException("private message https://secret.example/?token=123", "Microsoft.Windows.SDK.NET, Version=10.0.17763.10, Culture=neutral")
                Dim diagnostic As String = ErrorDiagnostics.FormatException(New InvalidOperationException("outer private message", missing))
                Check("diagnostics retains inner exception", diagnostic.Contains("System.IO.FileNotFoundException", StringComparison.Ordinal))
                Check("diagnostics retains missing SDK name", diagnostic.Contains("Module: Microsoft.Windows.SDK.NET", StringComparison.Ordinal))
                Check("diagnostics retains HRESULT", diagnostic.Contains("HResult: 0x", StringComparison.Ordinal))
                Check("diagnostics retains runtime", diagnostic.Contains("Runtime: ", StringComparison.Ordinal))
                Check("diagnostics hides exception messages", Not diagnostic.Contains("private message", StringComparison.Ordinal))
                Check("diagnostics hides URL", Not diagnostic.Contains("secret.example", StringComparison.Ordinal))
                Equal("native module basename only", "WebView2Loader", ErrorDiagnostics.KnownModuleName("C:\Users\Someone\Private\WebView2Loader.dll"))
                Equal("projection module basename only", "WinRT.Runtime", ErrorDiagnostics.KnownModuleName("C:\Users\Someone\WinRT.Runtime.dll"))
                Equal("unknown file name redacted", "(name redacted)", ErrorDiagnostics.KnownModuleName("C:\Secret\private-document.pdf"))
                Equal("URL as filename redacted", "(name redacted)", ErrorDiagnostics.KnownModuleName("https://secret.example/WinRT.Runtime.dll"))
                Equal("newline filename rejected", "(name redacted)", ErrorDiagnostics.KnownModuleName("WinRT.Runtime.dll" & ChrW(10) & "extra"))
                Equal("empty module name", "(not specified)", ErrorDiagnostics.KnownModuleName(Nothing))
                Dim unknown As String = ErrorDiagnostics.FormatException(New FileLoadException("private load failure", "secret-document.pdf"))
                Check("unknown module never exposed", Not unknown.Contains("secret-document", StringComparison.Ordinal))
                CheckSetupFlow()
                _passed += FeatureChecks.Run()
                Console.WriteLine("PASS: " & _passed.ToString() & " checks. No external websites visited; only disposable test profiles used.")
                Return 0
            Catch ex As Exception
                Console.Error.WriteLine("FAIL after " & _passed.ToString() & " checks: " & ex.Message)
                Return 1
            End Try
        End Function
        Private Sub CheckSetupFlow()
            Dim original As New BrowserSettings With {.Theme = "Forest", .Accent = "Mint"}
            Check("new profile needs setup", Not original.SetupCompleted)
            Dim flow As New SetupSession(original)
            Equal("wizard begins at step zero", 0, flow.StepIndex)
            Check("no automatic search selection", Not flow.HasSearchSelection)
            Check("next blocked without provider", Not flow.MoveNext())
            Equal("blocked next keeps step zero", 0, flow.StepIndex)
            Check("back cannot underflow", Not flow.MoveBack())
            flow.Draft.Theme = "Dawn"
            Equal("theme preview does not mutate original", "Forest", original.Theme)
            Dim earlyCompletionRejected As Boolean = False
            Try
                flow.Complete()
            Catch ex As InvalidOperationException
                earlyCompletionRejected = True
            End Try
            Check("incomplete wizard cannot finish", earlyCompletionRejected)
            For Each provider As SearchProvider In SearchProviders.All
                Check("provider is recognized", SearchProviders.IsKnown(provider.Name))
                Check("provider prefix is HTTPS", provider.QueryPrefix.StartsWith("https://", StringComparison.Ordinal))
                Equal("provider query encoding", provider.QueryPrefix & "a%20%26%20b", UrlPolicy.Resolve("a & b", provider.Name))
            Next
            Check("invalid provider is rejected", Not SearchProviders.IsKnown("javascript:alert(1)"))
            Check("null provider is rejected", Not SearchProviders.IsKnown(Nothing))
            Equal("provider fallback unchanged", "DuckDuckGo", SearchProviders.GetProvider(Nothing).Name)
            flow.Draft.SearchEngine = "Google"
            Check("explicit selection enables next", flow.HasSearchSelection)
            Check("move to appearance", flow.MoveNext())
            Equal("appearance step", 1, flow.StepIndex)
            Check("move back", flow.MoveBack())
            Equal("search selection kept after back", "Google", flow.Draft.SearchEngine)
            Check("return to appearance", flow.MoveNext())
            Check("move to personalization", flow.MoveNext())
            Check("cannot overflow last step", Not flow.MoveNext())
            flow.Draft.DisplayName = "David" & ChrW(&H202E) & ChrW(10)
            flow.Draft.Animations = False
            flow.Draft.RestoreSession = False
            Dim completed As BrowserSettings = flow.Complete()
            Check("completion marker is set", completed.SetupCompleted)
            Equal("selected search retained", "Google", completed.SearchEngine)
            Equal("name sanitized on completion", "David", completed.DisplayName)
            Equal("other preferences retained", "Mint", completed.Accent)
            Check("animation choice retained", Not completed.Animations)
            Check("restore choice retained", Not completed.RestoreSession)
            Check("original completion marker unchanged", Not original.SetupCompleted)
            Equal("original search unchanged", "DuckDuckGo", original.SearchEngine)
            Check("original restore unchanged", original.RestoreSession)
            Dim saved As BrowserSettings = JsonSerializer.Deserialize(Of BrowserSettings)(JsonSerializer.Serialize(completed))
            Check("completed marker round trips", saved.SetupCompleted)
            Dim reopen As New SetupSession(saved)
            Equal("reopened wizard keeps selected engine", "Google", reopen.Draft.SearchEngine)
            reopen.Draft.Theme = "Graphite"
            Equal("cancel-safe draft remains separate", "Dawn", saved.Theme)
            Dim oldJson As String = "{""SchemaVersion"":1,""Settings"":{""SearchEngine"":""Bing"",""DisplayName"":""David"",""Theme"":""Forest""},""Bookmarks"":[{""Title"":""Saved"",""Url"":""https://example.com/""}],""Session"":[{""Title"":""Saved tab"",""Url"":""https://example.org/""}]}"
            Dim migrated As AppState = JsonSerializer.Deserialize(Of AppState)(oldJson)
            Check("old profile gets wizard once", Not migrated.Settings.SetupCompleted)
            Equal("old theme remains intact", "Forest", migrated.Settings.Theme)
            Equal("old bookmark remains intact", "https://example.com/", migrated.Bookmarks(0).Url)
            Equal("old saved tab remains intact", "https://example.org/", migrated.Session(0).Url)
            Dim upgraded As New SetupSession(migrated.Settings)
            upgraded.Draft.SearchEngine = "Bing"
            upgraded.MoveNext()
            upgraded.MoveNext()
            migrated.Settings = upgraded.Complete()
            Dim reloaded As AppState = JsonSerializer.Deserialize(Of AppState)(JsonSerializer.Serialize(migrated))
            Check("upgraded profile skips wizard next launch", reloaded.Settings.SetupCompleted)
            Equal("upgrade preserves bookmark", "https://example.com/", reloaded.Bookmarks(0).Url)
            Equal("upgrade preserves saved session", "https://example.org/", reloaded.Session(0).Url)
        End Sub

        Private Sub Check(label As String, condition As Boolean)
            If Not condition Then Throw New Exception(label)
            _passed += 1
        End Sub
        Private Sub Equal(Of T)(label As String, expected As T, actual As T)
            Check(label & " | expected=" & Convert.ToString(expected) & " actual=" & Convert.ToString(actual), Collections.Generic.EqualityComparer(Of T).Default.Equals(expected, actual))
        End Sub
        Private Sub Reject(value As String)
            Dim rejected = False
            Try
                UrlPolicy.Resolve(value, "DuckDuckGo")
            Catch ex As ArgumentException
                rejected = True
            End Try
            Check("blocked URI", rejected)
        End Sub
    End Module
End Namespace
