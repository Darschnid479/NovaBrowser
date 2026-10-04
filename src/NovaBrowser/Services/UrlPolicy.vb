Imports System
Imports System.Net
Imports System.Text.RegularExpressions

Namespace NovaBrowser
    ' Shared with the executable test project; no WPF or WebView2 dependency.
    Public NotInheritable Class UrlPolicy
        Public Const HomeUrl As String = "nova://home"
        Private Sub New()
        End Sub

        Public Shared Function Resolve(input As String, provider As String) As String
            Dim text = If(input, "").Trim()
            If text.Length = 0 OrElse String.Equals(text, HomeUrl, StringComparison.OrdinalIgnoreCase) Then Return HomeUrl
            If text.Length > 8192 OrElse text.IndexOfAny(New Char() {ChrW(0), ChrW(10), ChrW(13)}) >= 0 Then
                Throw New ArgumentException("Adressen er for lang eller inneholder ugyldige tegn.")
            End If
            If text.StartsWith("http://", StringComparison.OrdinalIgnoreCase) OrElse text.StartsWith("https://", StringComparison.OrdinalIgnoreCase) Then
                Return ValidateWebUrl(text)
            End If
            ' Explicit local paths are not queries and must never become local-file access.
            If Regex.IsMatch(text, "^[A-Za-z]:[\\/]") OrElse text.StartsWith("\\", StringComparison.Ordinal) Then
                Throw New ArgumentException("Lokale filer kan ikke åpnes fra adressefeltet.")
            End If
            ' A bare domain, IP, localhost, or domain:port becomes an address, not a query.
            Dim candidate As Uri = Nothing
            If Not Regex.IsMatch(text, "\s|^\d+$") AndAlso Uri.TryCreate("https://" & text, UriKind.Absolute, candidate) Then
                Dim host = candidate.Host
                Dim ip As IPAddress = Nothing
                Dim isIp = IPAddress.TryParse(host.Trim("["c, "]"c), ip)
                If host.Contains("."c) OrElse isIp OrElse host.Equals("localhost", StringComparison.OrdinalIgnoreCase) Then
                    If Not String.IsNullOrEmpty(candidate.UserInfo) Then Throw New ArgumentException("Adresser med brukernavn eller passord i selve adressen er blokkert.")
                    Dim scheme = If(host.Equals("localhost", StringComparison.OrdinalIgnoreCase) OrElse (isIp AndAlso IPAddress.IsLoopback(ip)), "http://", "https://")
                    Return ValidateWebUrl(scheme & text)
                End If
            End If
            If Regex.IsMatch(text, "^[A-Za-z][A-Za-z0-9+.-]*:") Then
                Throw New ArgumentException("NOVA støtter http- og https-adresser. Skript og andre adressetyper er blokkert.")
            End If
            Return SearchProviders.GetProvider(provider).QueryPrefix & Uri.EscapeDataString(text)
        End Function

        Private Shared Function ValidateWebUrl(text As String) As String
            Dim value As Uri = Nothing
            If Not Uri.TryCreate(text, UriKind.Absolute, value) OrElse Not IsWebUrl(text) Then
                Throw New ArgumentException("Adressen er ikke gyldig. Kontroller nettadressen og prøv igjen.")
            End If
            ' Show ASCII/punycode host names to reduce Unicode domain ambiguity.
            Dim builder As New UriBuilder(value) With {.Host = value.IdnHost}
            Return builder.Uri.AbsoluteUri
        End Function

        Public Shared Function IsWebUrl(value As String) As Boolean
            Dim parsed As Uri = Nothing
            Return Uri.TryCreate(value, UriKind.Absolute, parsed) AndAlso
                (parsed.Scheme = Uri.UriSchemeHttp OrElse parsed.Scheme = Uri.UriSchemeHttps) AndAlso
                Not String.IsNullOrEmpty(parsed.Host) AndAlso String.IsNullOrEmpty(parsed.UserInfo)
        End Function

        Public Shared Function IsAllowedNavigation(value As String) As Boolean
            Return IsWebUrl(value) OrElse String.Equals(value, "about:blank", StringComparison.OrdinalIgnoreCase)
        End Function

        Public Shared Function DisplayHost(value As String) As String
            Dim parsed As Uri = Nothing
            If Uri.TryCreate(value, UriKind.Absolute, parsed) Then Return parsed.IdnHost
            Return "Nettsted"
        End Function

        Public Shared Function CleanTitle(value As String) As String
            Dim result = Regex.Replace(If(value, ""), "[\p{Cc}\p{Cf}]", "").Trim()
            Return If(result.Length > 160, result.Substring(0, 160), result)
        End Function
    End Class
End Namespace
