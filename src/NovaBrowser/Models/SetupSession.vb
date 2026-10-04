Imports System

Namespace NovaBrowser
    ' Edits a copy: backing out of the wizard must not change saved preferences.
    ' Pure .NET, so state transitions can be exercised without a browser profile.
    Public NotInheritable Class SetupSession
        Public ReadOnly Property Draft As BrowserSettings
        Public Property StepIndex As Integer = 0
        Public ReadOnly Property HasSearchSelection As Boolean
            Get
                Return SearchProviders.IsKnown(Draft.SearchEngine)
            End Get
        End Property
        Public Sub New(original As BrowserSettings)
            If original Is Nothing Then Throw New ArgumentNullException(NameOf(original))
            Draft = original.Copy()
            If Not original.SetupCompleted Then Draft.SearchEngine = ""
        End Sub
        Public Function MoveNext() As Boolean
            If Not HasSearchSelection OrElse StepIndex >= 2 Then Return False
            StepIndex += 1
            Return True
        End Function
        Public Function MoveBack() As Boolean
            If StepIndex <= 0 Then Return False
            StepIndex -= 1
            Return True
        End Function
        Public Function Complete() As BrowserSettings
            If StepIndex <> 2 OrElse Not HasSearchSelection Then
                Throw New InvalidOperationException("Velg søkemotor og fullfør veiviseren først.")
            End If
            Dim result As BrowserSettings = Draft.Copy()
            result.DisplayName = UrlPolicy.CleanTitle(result.DisplayName)
            If result.DisplayName.Length > 32 Then result.DisplayName = result.DisplayName.Substring(0, 32)
            result.SetupCompleted = True
            Return result
        End Function
    End Class
End Namespace
