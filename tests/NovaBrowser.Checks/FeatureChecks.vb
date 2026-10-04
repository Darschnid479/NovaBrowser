Imports System
Imports System.Collections.Generic
Imports System.IO
Imports System.Linq
Imports System.Text.Json
Imports NovaBrowser

Namespace NovaBrowser.Checks
    Public NotInheritable Class FeatureChecks
        Private Shared _count As Integer
        Public Shared Function Run() As Integer
            _count = 0
            Dim state As New AppState()
            state.Settings.WindowWidth = Double.NaN
            state.Settings.WindowHeight = Double.PositiveInfinity
            state.Settings.SleepAfterMinutes = -1
            state.Session.Add(New SessionEntry With {.Url = "file:///C:/secret", .Zoom = -2})
            state.Session.Add(New SessionEntry With {.Url = "https://example.com/", .Zoom = Double.NaN, .IsPinned = True, .IsMuted = True})
            state.ActiveSessionIndex = 100
            state = SessionValidator.Normalize(state)
            Check("finite width", state.Settings.WindowWidth = 1280)
            Check("finite height", state.Settings.WindowHeight = 820)
            Check("bounded sleep timeout", state.Settings.SleepAfterMinutes = 5)
            Check("invalid restored URL removed", state.Session.Count = 1)
            Check("invalid tab zoom repaired", state.Session(0).Zoom = 1)
            Check("pin and mute retained", state.Session(0).IsPinned AndAlso state.Session(0).IsMuted)
            Check("active index bounded", state.ActiveSessionIndex = 0)
            Dim old = JsonSerializer.Deserialize(Of AppState)("{""SchemaVersion"":1,""Settings"":{},""Session"":[{""Url"":""nova://home""}]}")
            Check("0.2 profile is compatible", SessionValidator.Normalize(old).Session.Count = 1)
            Check("old tab has safe zoom default", old.Session(0).Zoom = 1)
            Check("sleep is opt-in", Not New BrowserSettings().SleepingTabs)
            Check("maximized startup default", New BrowserSettings().StartMaximized)
            state.Settings.RestoreSession = False
            Check("restore disabled erases saved list", SessionValidator.Normalize(state).Session.Count = 0)
            Dim rejected = False
            Try
                SessionValidator.Normalize(New AppState With {.SchemaVersion = 999})
            Catch ex As InvalidDataException
                rejected = True
            End Try
            Check("unknown schema not silently accepted", rejected)
            For mask As Integer = 0 To 255
                Dim currentMask = mask
                Dim flags = Enumerable.Range(0, 8).Select(Function(bit) (currentMask And (1 << bit)) <> 0).ToArray()
                Check("sleep exclusion mask " & mask, TabSleepPolicy.CanSleep(flags(0), flags(1), flags(2), flags(3), flags(4), flags(5), flags(6), flags(7)) = (mask = 0))
            Next
            Dim now = DateTimeOffset.UtcNow
            Check("not idle too early", Not TabSleepPolicy.IsIdle(now.AddMinutes(-14), now, 15))
            Check("idle at threshold", TabSleepPolicy.IsIdle(now.AddMinutes(-15), now, 15))
            Check("clock moving backwards is safe", Not TabSleepPolicy.IsIdle(now.AddHours(1), now, 5))
            Dim tabs As New List(Of TabSnapshot) From {
                New TabSnapshot With {.Id = Guid.NewGuid(), .Title = "Example normal", .Address = "https://example.com/"},
                New TabSnapshot With {.Id = Guid.NewGuid(), .Title = "Example private", .Address = "https://secret.example/", .IsPrivate = True}}
            Dim bookmarks As New List(Of PageEntry) From {New PageEntry With {.Title = "Example bookmark", .Url = "https://example.com/"}}
            Dim history As New List(Of PageEntry) From {New PageEntry With {.Title = "Example history", .Url = "https://history.example/"}}
            Dim normal = LocalSuggestions.Find("example", tabs, bookmarks, history, False)
            Check("duplicate normal URL collapsed", normal.Count = 2)
            Check("open tab wins over bookmark", normal(0).TabId = tabs(0).Id)
            Check("private tab not exposed in normal suggestions", Not normal.Any(Function(item) item.Address.Contains("secret")))
            Dim privateResults = LocalSuggestions.Find("example", tabs, bookmarks, history, True)
            Check("normal tab not exposed in private suggestions", Not privateResults.Any(Function(item) item.TabId = tabs(0).Id))
            Check("history not exposed in private suggestions", Not privateResults.Any(Function(item) item.Address.Contains("history")))
            Check("private tab available in matching context", privateResults.Any(Function(item) item.TabId = tabs(1).Id))
            Check("empty input reveals no history", LocalSuggestions.Find("", tabs, bookmarks, history, False).Count = 0)
            Check("oversized input ignored", LocalSuggestions.Find(New String("x"c, 2049), tabs, bookmarks, history, False).Count = 0)
            Check("query is case-insensitive", LocalSuggestions.Find("EXAMPLE", tabs, bookmarks, history, False).Count = normal.Count)
            Dim many As New List(Of PageEntry)()
            For index As Integer = 0 To 30
                many.Add(New PageEntry With {.Title = "Result " & index, .Url = "https://example.com/" & index})
            Next
            Check("suggestion count capped", LocalSuggestions.Find("result", Nothing, many, Nothing, False).Count = 8)
            many.Add(New PageEntry With {.Title = "Dangerous", .Url = "javascript:alert(1)"})
            Check("non-web URL excluded", Not LocalSuggestions.Find("dangerous", Nothing, many, Nothing, False).Any())
            Dim paths As New List(Of PageEntry) From {New PageEntry With {.Title = "Case", .Url = "https://example.com/A"}, New PageEntry With {.Title = "Case", .Url = "https://example.com/a"}}
            Check("case-sensitive paths remain distinct", LocalSuggestions.Find("Case", Nothing, paths, Nothing, False).Count = 2)
            CheckRepository()
            Return _count
        End Function
        Private Shared Sub CheckRepository()
            Dim root = Path.Combine(Path.GetTempPath(), "NOVA-persistence-tests-" & Guid.NewGuid().ToString("N"))
            Try
                Dim repository As New ProfileRepository(root)
                Dim state = repository.Load()
                Check("fresh profile loads", state IsNot Nothing)
                state.Settings.DisplayName = "First"
                repository.Save(state)
                state.Settings.DisplayName = "Second"
                repository.Save(state)
                Check("disk round trip", repository.Load().Settings.DisplayName = "Second")
                Check("last-known-good backup created", File.Exists(Path.Combine(root, "state.json.bak")))
                Check("no temp file after success", Not Directory.EnumerateFiles(root, "*.tmp-*").Any())
                Dim writeFailed = False
                state.Settings.DefaultZoom = Double.NaN
                Try
                    repository.Save(state)
                Catch ex As Exception
                    writeFailed = True
                End Try
                Check("invalid numeric JSON fails write", writeFailed)
                Check("failed write preserves old primary", repository.Load().Settings.DisplayName = "Second")
                Check("failed write leaves no partial temp", Not Directory.EnumerateFiles(root, "*.tmp-*").Any())
                state.Settings.DefaultZoom = 1
                File.WriteAllText(Path.Combine(root, "state.json"), "broken json")
                Check("corrupt primary recovers backup", repository.Load().Settings.DisplayName = "First")
                Check("recovery warning visible", repository.Warning.Length > 0)
                Check("corrupt original retained", Directory.EnumerateFiles(root, "*.corrupt-*").Any())
                repository.Save(state)
                Check("save works after recovery", repository.Load().Settings.DisplayName = "Second")
                repository.ForgetRecoveryCopies()
                Check("erase removes backup", Not File.Exists(Path.Combine(root, "state.json.bak")))
                Check("erase removes corrupt copies", Not Directory.EnumerateFiles(root, "*.corrupt-*").Any())
                Check("erase retains active profile", File.Exists(Path.Combine(root, "state.json")))
            Finally
                If Directory.Exists(root) Then Directory.Delete(root, True)
            End Try
        End Sub
        Private Shared Sub Check(label As String, condition As Boolean)
            If Not condition Then Throw New InvalidOperationException(label)
            _count += 1
        End Sub
    End Class
End Namespace
