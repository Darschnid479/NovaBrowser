Imports System
Imports System.Globalization
Imports System.IO
Imports System.Runtime.InteropServices
Imports System.Text

Namespace NovaBrowser
    ' Pure formatter: does not read browser state, write files, or visit websites.
    Public NotInheritable Class ErrorDiagnostics
        Private Sub New()
        End Sub

        Public Shared Function FormatException(ex As Exception) As String
            If ex Is Nothing Then Throw New ArgumentNullException(NameOf(ex))
            Dim report As New StringBuilder()
            report.AppendLine(DateTimeOffset.Now.ToString("O", CultureInfo.InvariantCulture) & " NOVA 0.3.0 diagnostics v2")
            report.AppendLine("Runtime: " & RuntimeInformation.FrameworkDescription)
            report.AppendLine("OS: " & Environment.OSVersion.VersionString)
            report.AppendLine("Architecture: " & RuntimeInformation.ProcessArchitecture.ToString())
            Dim currentException As Exception = ex
            Dim depth As Integer = 0
            While currentException IsNot Nothing AndAlso depth < 8
                report.AppendLine("Exception[" & depth.ToString(CultureInfo.InvariantCulture) & "]: " & currentException.GetType().FullName)
                report.AppendLine("HResult: 0x" & currentException.HResult.ToString("X8", CultureInfo.InvariantCulture))
                Dim rawName As String = Nothing
                If TypeOf currentException Is FileNotFoundException Then
                    rawName = DirectCast(currentException, FileNotFoundException).FileName
                ElseIf TypeOf currentException Is FileLoadException Then
                    rawName = DirectCast(currentException, FileLoadException).FileName
                ElseIf TypeOf currentException Is BadImageFormatException Then
                    rawName = DirectCast(currentException, BadImageFormatException).FileName
                End If
                If rawName IsNot Nothing Then report.AppendLine("Module: " & KnownModuleName(rawName))
                ' Exception.Message / ToString / Data can contain URLs and private data.
                ' Keep known library names and stack traces, not arbitrary messages.
                report.AppendLine(If(currentException.StackTrace, "(no stack trace)"))
                currentException = currentException.InnerException
                depth += 1
            End While
            report.AppendLine()
            Return report.ToString()
        End Function

        Public Shared Function KnownModuleName(rawName As String) As String
            If String.IsNullOrWhiteSpace(rawName) Then Return "(not specified)"
            If rawName.Length > 1024 OrElse rawName.Contains("://", StringComparison.Ordinal) Then Return "(name redacted)"
            For Each character As Char In rawName
                If Char.IsControl(character) Then Return "(name redacted)"
            Next
            Dim candidate As String = rawName.Trim().Replace("\"c, "/"c)
            Dim slashIndex As Integer = candidate.LastIndexOf("/"c)
            If slashIndex >= 0 Then candidate = candidate.Substring(slashIndex + 1)
            Dim commaIndex As Integer = candidate.IndexOf(","c)
            If commaIndex >= 0 Then candidate = candidate.Substring(0, commaIndex).Trim()
            If candidate.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) Then
                candidate = candidate.Substring(0, candidate.Length - 4)
            End If
            ' Allow only known dependency names; do not record document names or paths.
            For Each knownName As String In New String() {
                "Microsoft.Windows.SDK.NET", "WinRT.Runtime", "WebView2Loader",
                "Microsoft.Web.WebView2.Core", "Microsoft.Web.WebView2.Wpf",
                "Microsoft.Web.WebView2.WinForms", "PresentationCore",
                "PresentationFramework", "WindowsBase", "System.Runtime"}
                If String.Equals(candidate, knownName, StringComparison.OrdinalIgnoreCase) Then Return knownName
            Next
            Return "(name redacted)"
        End Function
    End Class
End Namespace
