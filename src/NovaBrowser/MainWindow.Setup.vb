Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports System.Windows
Imports System.Windows.Controls
Imports System.Windows.Input
Imports System.Windows.Media

Namespace NovaBrowser
    Public NotInheritable Class SetupThemeOption
        Public Property Name As String = ""
        Public Property Description As String = ""
        Public Property Background As String = ""
        Public Property Surface As String = ""
        Public Property Highlight As String = ""
        Public Property Outline As String = ""
    End Class

    Partial Public Class MainWindow
        Private _setupSession As SetupSession
        Private _setupUiReady As Boolean
        Private _sessionInitialized As Boolean

        Private Sub ShowSetup()
            If _isClosing Then Return
            _saveTimer.Stop()
            _setupUiReady = False
            _setupSession = New SetupSession(_state.Settings)
            Dim draft As BrowserSettings = _setupSession.Draft
            SetupSearchList.ItemsSource = SearchProviders.All
            SetupSearchList.SelectedItem = If(SearchProviders.IsKnown(draft.SearchEngine),
                SearchProviders.GetProvider(draft.SearchEngine), Nothing)
            Dim choices As New List(Of SetupThemeOption) From {
                New SetupThemeOption With {.Name = "Midnight", .Description = "Mørk og rolig", .Background = "#0D0F16", .Surface = "#1B2131", .Highlight = "#B1A1FF", .Outline = "#46506A"},
                New SetupThemeOption With {.Name = "Dawn", .Description = "Lys og luftig", .Background = "#F3F3F8", .Surface = "#E4E6F0", .Highlight = "#6659A3", .Outline = "#ABB2C4"},
                New SetupThemeOption With {.Name = "Forest", .Description = "Dype grønntoner", .Background = "#0C1615", .Surface = "#192B28", .Highlight = "#83E3B8", .Outline = "#42665A"},
                New SetupThemeOption With {.Name = "Graphite", .Description = "Dempet og grå", .Background = "#121212", .Surface = "#303030", .Highlight = "#D3D3D3", .Outline = "#666666"}}
            SetupThemeList.ItemsSource = choices
            SetupThemeList.SelectedItem = choices.FirstOrDefault(Function(item) item.Name = draft.Theme, choices(0))
            SetupAccentSetting.ItemsSource = ThemeManager.Accents
            SetupAccentSetting.SelectedItem = draft.Accent
            If SetupAccentSetting.SelectedIndex < 0 Then SetupAccentSetting.SelectedIndex = 0
            SetupAnimationsSetting.IsChecked = draft.Animations
            SetupNameSetting.Text = draft.DisplayName
            SetupRestoreSetting.IsChecked = draft.RestoreSession
            SetupHistorySetting.IsChecked = draft.RecordHistory
            SetupCancelButton.Content = If(_sessionInitialized, "Avbryt", "Lukk NOVA")
            DrawerLayer.Visibility = Visibility.Collapsed
            CommandLayer.Visibility = Visibility.Collapsed
            SetupLayer.Visibility = Visibility.Visible
            _setupUiReady = True
            RefreshSetupStep(True)
            RefreshInputScopes()
            AnimateEntrance(SetupCard, 14)
        End Sub

        Private Sub RefreshSetupStep(moveFocus As Boolean)
            If _setupSession Is Nothing Then Return
            Dim stepNumber As Integer = _setupSession.StepIndex
            SetupSearchPage.Visibility = If(stepNumber = 0, Visibility.Visible, Visibility.Collapsed)
            SetupThemePage.Visibility = If(stepNumber = 1, Visibility.Visible, Visibility.Collapsed)
            SetupPersonalPage.Visibility = If(stepNumber = 2, Visibility.Visible, Visibility.Collapsed)
            Dim titles As String() = {"Velg din søkemotor.", "Sett ditt preg på NOVA.", "Et siste, personlig preg."}
            Dim descriptions As String() = {
                "Dette valget brukes når du søker fra adressefeltet eller startsiden.",
                "Velg tema og aksentfarge. Prøv deg frem – alt kan endres senere.",
                "Velg hva startsiden skal kalle deg, og hvordan NOVA starter neste gang."}
            SetupTitle.Text = titles(stepNumber)
            SetupDescription.Text = descriptions(stepNumber)
            SetupStepLabel.Text = (stepNumber + 1).ToString() & " / 3"
            SetupBackButton.Visibility = If(stepNumber = 0, Visibility.Hidden, Visibility.Visible)
            SetupNextButton.Content = If(stepNumber = 2, "Start NOVA", "Neste")
            SetupNextButton.IsEnabled = _setupSession.HasSearchSelection
            Dim labels As TextBlock() = {SetupRailSearch, SetupRailTheme, SetupRailPersonal}
            For index As Integer = 0 To labels.Length - 1
                labels(index).SetResourceReference(TextBlock.ForegroundProperty, If(index = stepNumber, "Accent", "Muted"))
            Next
            SetupErrorText.Visibility = Visibility.Collapsed
            RefreshSetupSummary()
            If moveFocus Then
                SetupScroller.ScrollToTop()
                AnimateEntrance(SetupPageContent, 10)
                FocusSetupStep()
            End If
        End Sub

        Private Sub FocusSetupStep()
            If _setupSession Is Nothing Then Return
            Select Case _setupSession.StepIndex
                Case 0 : SetupSearchList.Focus()
                Case 1 : SetupThemeList.Focus()
                Case Else : SetupNameSetting.Focus()
            End Select
        End Sub

        Private Sub RefreshSetupSummary()
            If _setupSession Is Nothing Then Return
            Dim draft As BrowserSettings = _setupSession.Draft
            SetupSummary.Text = "Søkemotor: " & draft.SearchEngine & Environment.NewLine &
                "Utseende: " & draft.Theme & " / " & draft.Accent & Environment.NewLine &
                "Du kan endre alt under Innstillinger."
        End Sub

        Private Sub SetupSearch_Changed(sender As Object, e As SelectionChangedEventArgs)
            If Not _setupUiReady OrElse _setupSession Is Nothing Then Return
            Dim provider As SearchProvider = TryCast(SetupSearchList.SelectedItem, SearchProvider)
            _setupSession.Draft.SearchEngine = If(provider Is Nothing, "", provider.Name)
            RefreshSetupStep(False)
        End Sub

        Private Sub SetupTheme_Changed(sender As Object, e As SelectionChangedEventArgs)
            If Not _setupUiReady OrElse _setupSession Is Nothing Then Return
            Dim choice As SetupThemeOption = TryCast(SetupThemeList.SelectedItem, SetupThemeOption)
            If choice Is Nothing Then Return
            _setupSession.Draft.Theme = choice.Name
            PreviewSetupAppearance()
        End Sub

        Private Sub SetupAppearance_Changed(sender As Object, e As RoutedEventArgs)
            If Not _setupUiReady OrElse _setupSession Is Nothing Then Return
            _setupSession.Draft.Accent = CStr(SetupAccentSetting.SelectedItem)
            _setupSession.Draft.Animations = SetupAnimationsSetting.IsChecked.GetValueOrDefault()
            PreviewSetupAppearance()
        End Sub

        Private Sub PreviewSetupAppearance()
            ThemeManager.Apply(Application.Current.Resources, _setupSession.Draft)
            ConfigureAmbientMotion()
            RefreshSetupSummary()
        End Sub

        Private Sub SetupPersonal_Changed(sender As Object, e As RoutedEventArgs)
            If Not _setupUiReady OrElse _setupSession Is Nothing Then Return
            _setupSession.Draft.DisplayName = SetupNameSetting.Text
            _setupSession.Draft.RestoreSession = SetupRestoreSetting.IsChecked.GetValueOrDefault()
            _setupSession.Draft.RecordHistory = SetupHistorySetting.IsChecked.GetValueOrDefault()
            RefreshSetupSummary()
        End Sub

        Private Sub SetupNext_Click(sender As Object, e As RoutedEventArgs)
            If _setupSession Is Nothing OrElse Not _setupSession.HasSearchSelection Then Return
            If _setupSession.StepIndex < 2 Then
                If _setupSession.MoveNext() Then RefreshSetupStep(True)
                Return
            End If
            FinishSetup()
        End Sub

        Private Sub SetupBack_Click(sender As Object, e As RoutedEventArgs)
            If _setupSession IsNot Nothing AndAlso _setupSession.MoveBack() Then RefreshSetupStep(True)
        End Sub

        Private Sub FinishSetup()
            If _setupSession Is Nothing Then Return
            Dim oldSettings As BrowserSettings = _state.Settings
            Dim completed As BrowserSettings = _setupSession.Complete()
            _state.Settings = completed
            ' Persist before restoring tabs. On disk failure the wizard stays open.
            If Not StateStore.Save(_state) Then
                _state.Settings = oldSettings
                SetupErrorText.Text = "Kunne ikke lagre valgene. Kontroller ledig plass og tilgang til datamappen, og prøv igjen."
                SetupErrorText.Visibility = Visibility.Visible
                Return
            End If
            _setupUiReady = False
            SetupLayer.Visibility = Visibility.Collapsed
            _setupSession = Nothing
            ThemeManager.Apply(Application.Current.Resources, _state.Settings)
            InitializePreferences()
            For Each browserTab As BrowserTab In _tabs
                If browserTab.View?.CoreWebView2 IsNot Nothing Then ConfigureWebView(browserTab)
            Next
            If Not _sessionInitialized Then StartBrowsingSession()
            UpdateHome()
            UpdateClock()
            RefreshInputScopes()
            FocusActivePage()
            ScheduleSave()
            ShowToast("NOVA er klar. Du søker med " & _state.Settings.SearchEngine & ".")
        End Sub

        Private Sub CancelSetup()
            If Not _sessionInitialized Then
                Close()
                Return
            End If
            _setupUiReady = False
            _setupSession = Nothing
            SetupLayer.Visibility = Visibility.Collapsed
            ThemeManager.Apply(Application.Current.Resources, _state.Settings)
            UpdateHome()
            RefreshInputScopes()
            FocusActivePage()
        End Sub

        Private Sub SetupCancel_Click(sender As Object, e As RoutedEventArgs)
            CancelSetup()
        End Sub

        Private Sub RestartSetup_Click(sender As Object, e As RoutedEventArgs)
            Preferences_Changed(Me, New RoutedEventArgs())
            ShowSetup()
        End Sub
    End Class
End Namespace
