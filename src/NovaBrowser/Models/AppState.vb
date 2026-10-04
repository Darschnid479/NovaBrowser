Imports System
Imports System.Collections.Generic

Namespace NovaBrowser
    Public Class BrowserSettings
        Public Property DisplayName As String = "David"
        Public Property Theme As String = "Midnight"
        Public Property Accent As String = "Iris"
        Public Property Wallpaper As String = "Aurora"
        Public Property SearchEngine As String = "DuckDuckGo"
        Public Property Animations As Boolean = True
        Public Property ShowClock As Boolean = True
        Public Property CompactTabs As Boolean = False
        Public Property RestoreSession As Boolean = True
        Public Property RecordHistory As Boolean = True
        Public Property DefaultZoom As Double = 1.0
        ' Missing in old JSON => False: existing users also see the wizard once.
        Public Property SetupCompleted As Boolean = False
        Public Function Copy() As BrowserSettings
            ' All settings are scalar values or immutable strings.
            Return DirectCast(MemberwiseClone(), BrowserSettings)
        End Function
    End Class

    Public Class PageEntry
        Public Property Title As String = ""
        Public Property Url As String = ""
        Public Property VisitedAt As DateTimeOffset = DateTimeOffset.Now
        Public ReadOnly Property Host As String
            Get
                Dim parsed As Uri = Nothing
                Return If(Uri.TryCreate(Url, UriKind.Absolute, parsed), parsed.IdnHost, "")
            End Get
        End Property
        Public ReadOnly Property Initial As String
            Get
                Return If(String.IsNullOrWhiteSpace(Title), "N", Title.Substring(0, 1).ToUpperInvariant())
            End Get
        End Property
        Public ReadOnly Property Description As String
            Get
                Return Host & "  ·  " & VisitedAt.ToLocalTime().ToString("dd.MM HH:mm")
            End Get
        End Property
    End Class

    Public Class SessionEntry
        Public Property Title As String = "Ny fane"
        Public Property Url As String = UrlPolicy.HomeUrl
    End Class

    Public Class AppState
        Public Property SchemaVersion As Integer = 1
        Public Property Settings As BrowserSettings = New BrowserSettings()
        Public Property Bookmarks As List(Of PageEntry) = New List(Of PageEntry) From {
            New PageEntry With {.Title = "DuckDuckGo", .Url = "https://duckduckgo.com/"},
            New PageEntry With {.Title = "YouTube", .Url = "https://www.youtube.com/"},
            New PageEntry With {.Title = "GitHub", .Url = "https://github.com/"},
            New PageEntry With {.Title = "Wikipedia", .Url = "https://www.wikipedia.org/"}}
        Public Property History As List(Of PageEntry) = New List(Of PageEntry)()
        Public Property Session As List(Of SessionEntry) = New List(Of SessionEntry)()
        Public Property ActiveSessionIndex As Integer = 0
    End Class
End Namespace
