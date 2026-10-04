Imports System
Imports System.Collections.Generic
Imports System.Windows
Imports System.Windows.Media

Namespace NovaBrowser
    Public NotInheritable Class ThemeManager
        Public Shared ReadOnly Themes As String() = {"Midnight", "Dawn", "Forest", "Graphite"}
        Public Shared ReadOnly Accents As String() = {"Iris", "Cyan", "Mint", "Coral", "Gold", "Blue"}
        Private Sub New()
        End Sub
        Public Shared Sub Apply(resources As ResourceDictionary, settings As BrowserSettings)
            Dim palette As String()
            Select Case settings.Theme
                Case "Dawn"
                    palette = {"#F3F3F8", "#EBECF4", "#FFFFFF", "#E4E6F0", "#D3D8E6", "#1D2639", "#59647B"}
                Case "Forest"
                    palette = {"#0C1615", "#101D1B", "#192B28", "#162421", "#2B423C", "#E5F3ED", "#9CB5AD"}
                Case "Graphite"
                    palette = {"#121212", "#191919", "#232323", "#202020", "#363636", "#F1F1F1", "#ADADAD"}
                Case Else
                    settings.Theme = "Midnight"
                    palette = {"#0D0F16", "#121621", "#1B2131", "#171C29", "#2B3348", "#EEF1FA", "#A2ACC3"}
            End Select
            Dim names = {"Bg", "Panel", "Elevated", "Field", "Stroke", "Text", "Muted"}
            For i = 0 To names.Length - 1
                resources(names(i)) = Brush(palette(i))
            Next
            Dim accentColor As String
            Select Case settings.Accent
                Case "Cyan" : accentColor = "#69D4F2"
                Case "Mint" : accentColor = "#83E3B8"
                Case "Coral" : accentColor = "#FFAA96"
                Case "Gold" : accentColor = "#F1CA7D"
                Case "Blue" : accentColor = "#91B8FF"
                Case Else : settings.Accent = "Iris" : accentColor = "#B1A1FF"
            End Select
            Dim accent = CType(ColorConverter.ConvertFromString(accentColor), Color)
            Dim light = settings.Theme = "Dawn"
            If light Then accent = Color.FromRgb(CByte(accent.R * 0.52), CByte(accent.G * 0.5), CByte(accent.B * 0.64))
            resources("Accent") = New SolidColorBrush(accent)
            resources("AccentSoft") = New SolidColorBrush(Color.FromArgb(30, accent.R, accent.G, accent.B))
            resources("OnAccent") = Brush(If(light, "#FFFFFF", "#10131B"))
            resources("Good") = Brush(If(light, "#236B54", "#83E3B8"))
            resources("Danger") = Brush(If(light, "#B53D4C", "#FFA1AE"))
            Dim home As New LinearGradientBrush With {.StartPoint = New Point(0, 0), .EndPoint = New Point(1, 1)}
            home.GradientStops.Add(New GradientStop(CType(ColorConverter.ConvertFromString(palette(1)), Color), 0))
            home.GradientStops.Add(New GradientStop(CType(ColorConverter.ConvertFromString(palette(0)), Color), 1))
            resources("HomeBackground") = home
            resources("HaloBrush") = Halo(accent)
            resources("SecondaryHalo") = Halo(If(light, Color.FromRgb(42, 129, 128), Color.FromRgb(50, 169, 167)))
        End Sub
        Private Shared Function Halo(color As Color) As RadialGradientBrush
            Dim b As New RadialGradientBrush()
            b.GradientStops.Add(New GradientStop(Color.FromArgb(65, color.R, color.G, color.B), 0))
            b.GradientStops.Add(New GradientStop(Color.FromArgb(0, color.R, color.G, color.B), 1))
            Return b
        End Function
        Private Shared Function Brush(value As String) As SolidColorBrush
            Dim b As New SolidColorBrush(CType(ColorConverter.ConvertFromString(value), Color))
            b.Freeze()
            Return b
        End Function
    End Class
End Namespace
