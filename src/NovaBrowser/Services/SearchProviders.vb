Imports System
Imports System.Collections.Generic
Imports System.Collections.ObjectModel
Imports System.Linq

Namespace NovaBrowser
    Public NotInheritable Class SearchProvider
        Public ReadOnly Property Name As String
        Public ReadOnly Property Domain As String
        Public ReadOnly Property QueryPrefix As String
        Public ReadOnly Property Initial As String
            Get
                Return Name.Substring(0, 1)
            End Get
        End Property
        Public Sub New(name As String, domain As String, queryPrefix As String)
            Me.Name = name
            Me.Domain = domain
            Me.QueryPrefix = queryPrefix
        End Sub
    End Class

    ' Shared by address handling, Settings, the wizard and the test project.
    Public NotInheritable Class SearchProviders
        Public Shared ReadOnly All As ReadOnlyCollection(Of SearchProvider) =
            New List(Of SearchProvider) From {
                New SearchProvider("Google", "google.com", "https://www.google.com/search?q="),
                New SearchProvider("DuckDuckGo", "duckduckgo.com", "https://duckduckgo.com/?q="),
                New SearchProvider("Bing", "bing.com", "https://www.bing.com/search?q=")}.AsReadOnly()
        Private Sub New()
        End Sub
        Public Shared ReadOnly Property Names As String()
            Get
                Return All.Select(Function(item) item.Name).ToArray()
            End Get
        End Property
        Public Shared Function IsKnown(name As String) As Boolean
            Return All.Any(Function(item) String.Equals(item.Name, name, StringComparison.Ordinal))
        End Function
        Public Shared Function GetProvider(name As String) As SearchProvider
            Return All.FirstOrDefault(Function(item) String.Equals(item.Name, name, StringComparison.Ordinal),
                All.First(Function(item) item.Name = "DuckDuckGo"))
        End Function
    End Class
End Namespace
