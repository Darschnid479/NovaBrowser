Imports System
Imports System.Collections.Generic
Imports System.IO
Imports System.Linq

Namespace NovaBrowser
    ' No networking in this file. Typing in the address bar stays on this PC.
    Public Class TabSnapshot
        Public Property Id As Guid
        Public Property Title As String = ""
        Public Property Address As String = ""
        Public Property IsPrivate As Boolean
    End Class

    Public Class AddressSuggestion
        Public Property Title As String = ""
        Public Property Address As String = ""
        Public Property Kind As String = ""
        Public Property TabId As Guid
        Public Property Score As Integer
        Public ReadOnly Property Detail As String
            Get
                Return Kind & " / " & Address
            End Get
        End Property
    End Class

    Public NotInheritable Class LocalSuggestions
        Private Sub New()
        End Sub
        Public Shared Function Find(query As String, tabs As IEnumerable(Of TabSnapshot), bookmarks As IEnumerable(Of PageEntry), history As IEnumerable(Of PageEntry), privateContext As Boolean) As List(Of AddressSuggestion)
            query = If(query, "").Trim()
            If query.Length = 0 OrElse query.Length > 2048 Then Return New List(Of AddressSuggestion)()
            Dim result As New List(Of AddressSuggestion)()
            For Each item In If(tabs, Enumerable.Empty(Of TabSnapshot)())
                If item Is Nothing OrElse item.IsPrivate <> privateContext Then Continue For
                AddMatch(result, query, item.Title, item.Address, "Åpen fane", 40, item.Id)
            Next
            For Each item In If(bookmarks, Enumerable.Empty(Of PageEntry)())
                If item IsNot Nothing Then AddMatch(result, query, item.Title, item.Url, "Bokmerke", 20, Guid.Empty)
            Next
            If Not privateContext Then
                For Each item In If(history, Enumerable.Empty(Of PageEntry)())
                    If item IsNot Nothing Then AddMatch(result, query, item.Title, item.Url, "Historikk", 0, Guid.Empty)
                Next
            End If
            ' Path and query are case-sensitive. Never merge distinct /A and /a URLs.
            Return result.OrderByDescending(Function(item) item.Score).
                GroupBy(Function(item) item.Address, StringComparer.Ordinal).
                Select(Function(group) group.First()).Take(8).ToList()
        End Function
        Private Shared Sub AddMatch(result As List(Of AddressSuggestion), query As String, title As String, address As String, kind As String, boost As Integer, tabId As Guid)
            If Not UrlPolicy.IsWebUrl(address) Then Return
            title = UrlPolicy.CleanTitle(title)
            Dim host = UrlPolicy.DisplayHost(address)
            Dim score As Integer = -1
            If host.StartsWith(query, StringComparison.OrdinalIgnoreCase) Then
                score = 60
            ElseIf title.StartsWith(query, StringComparison.CurrentCultureIgnoreCase) Then
                score = 50
            ElseIf title.Contains(query, StringComparison.CurrentCultureIgnoreCase) OrElse address.Contains(query, StringComparison.OrdinalIgnoreCase) Then
                score = 10
            End If
            If score < 0 Then Return
            result.Add(New AddressSuggestion With {.Title = If(title.Length > 0, title, host), .Address = address, .Kind = kind, .TabId = tabId, .Score = score + boost})
        End Sub
    End Class

    Public NotInheritable Class TabSleepPolicy
        Private Sub New()
        End Sub
        Public Shared Function CanSleep(active As Boolean, home As Boolean, loading As Boolean, audio As Boolean, pinned As Boolean, downloading As Boolean, sensitivePermission As Boolean, closed As Boolean) As Boolean
            Return Not (active OrElse home OrElse loading OrElse audio OrElse pinned OrElse downloading OrElse sensitivePermission OrElse closed)
        End Function
        Public Shared Function IsIdle(lastActive As DateTimeOffset, now As DateTimeOffset, minutes As Integer) As Boolean
            Return now >= lastActive AndAlso now - lastActive >= TimeSpan.FromMinutes(Math.Clamp(minutes, 5, 120))
        End Function
    End Class

    Public NotInheritable Class SessionValidator
        Private Sub New()
        End Sub
        Public Shared Function Normalize(state As AppState) As AppState
            If state Is Nothing OrElse state.SchemaVersion <> 1 Then Throw New InvalidDataException("Ukjent profilformat.")
            If state.Settings Is Nothing Then state.Settings = New BrowserSettings()
            state.Bookmarks = CleanPages(state.Bookmarks)
            state.History = CleanPages(state.History)
            state.Session = If(state.Session, New List(Of SessionEntry)()).Where(Function(entry) entry IsNot Nothing AndAlso
                (entry.Url = UrlPolicy.HomeUrl OrElse UrlPolicy.IsWebUrl(entry.Url))).Take(50).ToList()
            For Each entry In state.Session
                entry.Title = UrlPolicy.CleanTitle(entry.Title)
                entry.Zoom = Finite(entry.Zoom, 0.25, 3, 1)
            Next
            Dim settings = state.Settings
            If Not SearchProviders.IsKnown(settings.SearchEngine) Then
                settings.SearchEngine = "DuckDuckGo"
                settings.SetupCompleted = False
            End If
            settings.DisplayName = UrlPolicy.CleanTitle(settings.DisplayName)
            If settings.DisplayName.Length > 32 Then settings.DisplayName = settings.DisplayName.Substring(0, 32)
            settings.DefaultZoom = Finite(settings.DefaultZoom, 0.5, 2, 1)
            settings.WindowWidth = Finite(settings.WindowWidth, 760, 7680, 1280)
            settings.WindowHeight = Finite(settings.WindowHeight, 500, 4320, 820)
            settings.SleepAfterMinutes = Math.Clamp(settings.SleepAfterMinutes, 5, 120)
            If Not settings.RestoreSession Then state.Session.Clear()
            state.ActiveSessionIndex = Math.Clamp(state.ActiveSessionIndex, 0, Math.Max(0, state.Session.Count - 1))
            Return state
        End Function
        Private Shared Function CleanPages(pages As List(Of PageEntry)) As List(Of PageEntry)
            Dim result = If(pages, New List(Of PageEntry)()).Where(Function(entry) entry IsNot Nothing AndAlso UrlPolicy.IsWebUrl(entry.Url)).Take(500).ToList()
            For Each entry In result
                entry.Title = UrlPolicy.CleanTitle(entry.Title)
            Next
            Return result
        End Function
        Public Shared Function Finite(value As Double, minimum As Double, maximum As Double, fallback As Double) As Double
            If Double.IsNaN(value) OrElse Double.IsInfinity(value) Then Return fallback
            Return Math.Clamp(value, minimum, maximum)
        End Function
    End Class
End Namespace
