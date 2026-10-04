Imports System
Imports System.Windows
Imports System.Windows.Controls
Imports System.Windows.Media
Imports System.Windows.Media.Imaging
Imports NovaBrowser

Namespace NovaBrowser.UiChecks
    Public Module Program
        Private _passed As Integer

        <STAThread>
        Public Function Main() As Integer
            Dim app As Application = Nothing
            Try
                app = New Application With {.ShutdownMode = ShutdownMode.OnExplicitShutdown}
                ThemeManager.Apply(app.Resources, New BrowserSettings())
                app.Resources.MergedDictionaries.Add(New ResourceDictionary With {
                    .Source = New Uri("pack://application:,,,/NOVA.UiChecks;component/UI/Styles.xaml", UriKind.Absolute)})
                For Each kind As String In NovaIcon.SupportedKinds
                    Dim icon As New NovaIcon With {.Kind = kind, .Foreground = Brushes.White, .Width = 24, .Height = 24}
                    Layout(icon, 24, 24)
                    Dim bitmap As New RenderTargetBitmap(48, 48, 192, 192, PixelFormats.Pbgra32)
                    bitmap.Render(icon)
                    Dim pixels(48 * 48 * 4 - 1) As Byte
                    bitmap.CopyPixels(pixels, 48 * 4, 0)
                    Dim visiblePixels As Integer = 0
                    For offset As Integer = 3 To pixels.Length - 1 Step 4
                        If pixels(offset) > 0 Then visiblePixels += 1
                    Next
                    Check("vector icon draws pixels: " & kind, visiblePixels > 5)
                Next
                For Each theme As String In ThemeManager.Themes
                    ThemeManager.Apply(app.Resources, New BrowserSettings With {.Theme = theme})
                    For Each height As Double In New Double() {44, 48, 60}
                        Dim address As New TextBox With {
                            .Style = CType(app.Resources("AddressInput"), Style),
                            .Text = "https://example.com/path?q=NOVA", .FontFamily = New FontFamily("Segoe UI")}
                        Layout(address, 450, height)
                        Dim host As ScrollViewer = TryCast(address.Template.FindName("PART_ContentHost", address), ScrollViewer)
                        Check("text content host exists", host IsNot Nothing)
                        Check("content host honors center alignment", host.VerticalAlignment = VerticalAlignment.Center)
                        Check("padding is not also used as margin", host.Margin = New Thickness(0))
                        Dim position As Point = host.TranslatePoint(New Point(0, 0), address)
                        Check("text host is vertically centered", Math.Abs(position.Y + host.ActualHeight / 2 - height / 2) <= 1.5)
                        Dim caret As Rect = address.GetRectFromCharacterIndex(0)
                        Check("address text has a valid layout rectangle", Not caret.IsEmpty AndAlso caret.Height > 0)
                        Check("address line is not clipped", caret.Top >= -1 AndAlso caret.Bottom <= height + 1)
                        address.SelectAll()
                        Check("URL remains fully selectable", address.SelectionLength = address.Text.Length)
                    Next
                Next
                Console.WriteLine("PASS: " & _passed.ToString() & " WPF icon/text checks. No browser profile or WebView2 used.")
                Return 0
            Catch ex As Exception
                Console.Error.WriteLine("FAIL after " & _passed.ToString() & " UI checks: " & ex.ToString())
                Return 1
            Finally
                If app IsNot Nothing Then app.Shutdown()
            End Try
        End Function

        Private Sub Layout(element As FrameworkElement, width As Double, height As Double)
            element.ApplyTemplate()
            element.Measure(New Size(width, height))
            element.Arrange(New Rect(0, 0, width, height))
            element.UpdateLayout()
        End Sub
        Private Sub Check(label As String, condition As Boolean)
            If Not condition Then Throw New InvalidOperationException(label)
            _passed += 1
        End Sub
    End Module
End Namespace
