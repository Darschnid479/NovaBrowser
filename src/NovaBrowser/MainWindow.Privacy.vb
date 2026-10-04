Imports System
Imports System.Linq
Imports System.Threading.Tasks
Imports System.Windows
Imports Microsoft.Web.WebView2.Core

Namespace NovaBrowser
    Partial Public Class MainWindow
        Private Async Function HandleNewWindowAsync(source As BrowserTab, e As CoreWebView2NewWindowRequestedEventArgs) As Task
            e.Handled = True
            If source.Closed OrElse _isClosing Then Return
            If Not e.IsUserInitiated OrElse Not UrlPolicy.IsAllowedNavigation(e.Uri) Then
                ShowToast("Et automatisk popupvindu eller en blokkert adresse ble stoppet.")
                Return
            End If
            Dim deferral = e.GetDeferral()
            Dim target As BrowserTab = Nothing
            Try
                ' Same environment + matching profile preserve window.opener and login flows.
                target = CreateTab(UrlPolicy.HomeUrl, source.IsPrivate, False)
                If target Is Nothing Then Return
                If Not Await EnsureViewAsync(target) Then Return
                If source.Closed OrElse target.Closed OrElse _isClosing Then Return
                target.IsHome = False
                target.Title = "Ny side"
                target.Address = e.Uri
                target.NotifyLocation()
                e.NewWindow = target.View.CoreWebView2
                TabList.SelectedItem = target
                ScheduleSave()
            Catch ex As Exception
                StateStore.LogError(ex)
                If target IsNot Nothing AndAlso Not target.Closed Then RemoveTab(target)
                ShowToast("Den nye fanen kunne ikke åpnes.")
            Finally
                Try
                    deferral.Complete()
                Catch ex As Exception
                    StateStore.LogError(ex)
                End Try
            End Try
        End Function

        Private Async Function HandlePermissionAsync(browserTab As BrowserTab, e As CoreWebView2PermissionRequestedEventArgs) As Task
            e.State = CoreWebView2PermissionState.Deny
            e.SavesInProfile = False
            If browserTab.Closed OrElse _isClosing OrElse browserTab IsNot _active OrElse Not e.IsUserInitiated OrElse Not UrlPolicy.IsWebUrl(e.Uri) Then Return
            Dim name As String
            Select Case e.PermissionKind
                Case CoreWebView2PermissionKind.Camera : name = "kameraet"
                Case CoreWebView2PermissionKind.Microphone : name = "mikrofonen"
                Case CoreWebView2PermissionKind.Geolocation : name = "posisjonen din"
                Case CoreWebView2PermissionKind.ClipboardRead : name = "innholdet på utklippstavlen"
                Case CoreWebView2PermissionKind.Notifications : name = "varsler"
                Case Else : Return
            End Select
            Dim deferral = e.GetDeferral()
            Try
                Dim requestPage = browserTab.View.CoreWebView2.Source
                Dim requestVersion = browserTab.DocumentVersion
                Dim origin = New Uri(e.Uri).GetLeftPart(UriPartial.Authority)
                Dim allowed = Await AskAsync("Tillate tilgang til " & name & "?", origin & Environment.NewLine & Environment.NewLine & "Gi bare tilgang dersom du stoler på dette nettstedet. Valget lagres ikke som en varig tillatelse.", "Tillat")
                If allowed AndAlso Not browserTab.Closed AndAlso Not _isClosing AndAlso browserTab Is _active AndAlso browserTab.View?.CoreWebView2 IsNot Nothing AndAlso browserTab.View.CoreWebView2.Source = requestPage AndAlso browserTab.DocumentVersion = requestVersion Then
                    e.State = CoreWebView2PermissionState.Allow
                    browserTab.HasSensitivePermission = True
                End If
            Catch ex As Exception
                StateStore.LogError(ex)
                e.State = CoreWebView2PermissionState.Deny
            Finally
                Try
                    deferral.Complete()
                Catch ex As Exception
                    StateStore.LogError(ex)
                End Try
            End Try
        End Function

        Private Function AskAsync(title As String, detail As String, yesText As String) As Task(Of Boolean)
            CloseAddressSuggestions()
            If _isClosing OrElse _confirmCompletion IsNot Nothing Then Return Task.FromResult(False)
            _confirmCompletion = New TaskCompletionSource(Of Boolean)(TaskCreationOptions.RunContinuationsAsynchronously)
            ConfirmTitle.Text = title
            ConfirmDetail.Text = detail
            ConfirmYesButton.Content = yesText
            ConfirmLayer.Visibility = Visibility.Visible
            RefreshInputScopes()
            ConfirmNoButton.Focus()
            Return _confirmCompletion.Task
        End Function
        Private Sub CompleteConfirmation(answer As Boolean)
            Dim completion = _confirmCompletion
            _confirmCompletion = Nothing
            ConfirmLayer.Visibility = Visibility.Collapsed
            RefreshInputScopes()
            If DrawerLayer.Visibility = Visibility.Visible Then
                NameSetting.Focus()
            Else
                FocusActivePage()
            End If
            completion?.TrySetResult(answer)
        End Sub
        Private Sub ConfirmNo_Click(sender As Object, e As RoutedEventArgs)
            CompleteConfirmation(False)
        End Sub
        Private Sub ConfirmYes_Click(sender As Object, e As RoutedEventArgs)
            CompleteConfirmation(True)
        End Sub

        Private Async Sub ClearHistory_Click(sender As Object, e As RoutedEventArgs)
            If Not Await AskAsync("Slette historikken?", "Dette fjerner NOVAs liste med tidligere besøkte sider og listen over nylig lukkede faner. Åpne faner og bokmerker blir beholdt.", "Slett historikken") Then Return
            _state.History.Clear()
            _closedTabs.Clear()
            If Not PersistState() Then Return
            If Not StateStore.ForgetRecoveryCopies() Then
                ShowToast("Historikken er tom, men en sikkerhetskopi kunne ikke slettes. Se feilloggen.")
                Return
            End If
            RefreshLibrary()
            ShowToast("NOVAs historikk er slettet. Nye besøk lagres dersom historikk er slått på.")
        End Sub

        Private Async Sub ClearSiteData_Click(sender As Object, e As RoutedEventArgs)
            If Not Await AskAsync("Slette nettstedsdata?", "Informasjonskapsler, hurtiglager og andre nettstedsdata i den vanlige profilen slettes. Du blir logget ut av nettsteder. Bokmerker beholdes. Åpne nettsteder kan opprette nye data.", "Slett data") Then Return
            ClearDataButton.IsEnabled = False
            Try
                Dim normal = _tabs.FirstOrDefault(Function(t) Not t.IsPrivate)
                If normal Is Nothing Then normal = CreateTab(UrlPolicy.HomeUrl, False, False)
                If normal Is Nothing OrElse Not Await EnsureViewAsync(normal) Then Return
                Await normal.View.CoreWebView2.Profile.ClearBrowsingDataAsync(CoreWebView2BrowsingDataKinds.AllProfile)
                _state.History.Clear()
                _closedTabs.Clear()
                If Not PersistState() Then Return
                If Not StateStore.ForgetRecoveryCopies() Then
                    ShowToast("Nettstedsdata er slettet, men en profilkopi kunne ikke slettes. Se feilloggen.")
                    Return
                End If
                ShowToast("Nettstedsdata i den vanlige profilen er slettet. Bokmerkene er beholdt.")
            Catch ex As Exception
                StateStore.LogError(ex)
                ShowToast("Kunne ikke slette alle nettstedsdata. Prøv igjen etter at nettsidene er lukket.")
            Finally
                ClearDataButton.IsEnabled = True
            End Try
        End Sub
    End Class
End Namespace
