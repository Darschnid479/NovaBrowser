Imports System
Imports System.IO
Imports System.Reflection
Imports System.Threading
Imports System.Windows

Namespace NovaBrowser
    Public Module Program
        <STAThread>
        Public Sub Main()
            Dim created As Boolean
            Using instance As New Mutex(True, "Local\NOVA.Browser.Desktop.v1", created)
                If Not created Then
                    MessageBox.Show("NOVA er allerede åpen. Se etter vinduet på oppgavelinjen.", "NOVA")
                    Return
                End If
                Try
                    ' Detect a missing WinRT projection before a WPF layout event throws.
                    VerifyCompositionDependencies()
                    Dim app As New Application With {.ShutdownMode = ShutdownMode.OnMainWindowClose}
                    Dim handlingFatalError As Boolean = False
                    AddHandler app.DispatcherUnhandledException,
                        Sub(sender, e)
                            e.Handled = True
                            StateStore.LogError(e.Exception)
                            ' A second error during cleanup must not open another dialog.
                            If handlingFatalError Then Return
                            handlingFatalError = True
                            Dim main As MainWindow = TryCast(app.MainWindow, MainWindow)
                            If main IsNot Nothing Then main.PrepareForFatalShutdown()
                            MessageBox.Show("NOVA møtte en feil og må lukkes." & Environment.NewLine &
                                "Feiltype: " & e.Exception.GetType().Name & Environment.NewLine &
                                "Detaljer: " & Path.Combine(StateStore.DataFolder, "error.log") & Environment.NewLine &
                                "Åpne loggen med AAPNE-FEILLOGG.cmd i prosjektmappen.",
                                "NOVA 0.3.0", MessageBoxButton.OK, MessageBoxImage.Error)
                            app.Shutdown(1)
                        End Sub
                    app.Resources.MergedDictionaries.Add(New ResourceDictionary With {
                        .Source = New Uri("pack://application:,,,/UI/Styles.xaml", UriKind.Absolute)})
                    app.Run(New MainWindow())
                Catch ex As Exception
                    Environment.ExitCode = 1
                    StateStore.LogError(ex)
                    MessageBox.Show("NOVA kunne ikke starte. " & ex.Message & Environment.NewLine &
                        "Detaljer: " & Path.Combine(StateStore.DataFolder, "error.log"),
                        "NOVA 0.3.0", MessageBoxButton.OK, MessageBoxImage.Error)
                Finally
                    instance.ReleaseMutex()
                End Try
            End Using
        End Sub

        Private Sub VerifyCompositionDependencies()
            For Each moduleName As String In New String() {"WinRT.Runtime", "Microsoft.Windows.SDK.NET"}
                Try
                    Assembly.Load(New AssemblyName(moduleName))
                Catch ex As Exception When TypeOf ex Is FileNotFoundException OrElse
                                           TypeOf ex Is FileLoadException OrElse
                                           TypeOf ex Is BadImageFormatException
                    Throw New InvalidOperationException(
                        "NOVA kunne ikke laste programdelen " & moduleName & "." & Environment.NewLine &
                        "Pakk ut hele den nye ZIP-filen i en ny mappe, og kjør START-NOVA.cmd." & Environment.NewLine &
                        "Bruker du EXE-utgaven, må alle filene i win-x64-mappen ligge sammen.", ex)
                End Try
            Next
        End Sub
    End Module
End Namespace
