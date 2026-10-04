Imports System
Imports System.Collections.Generic
Imports System.Windows
Imports System.Windows.Documents
Imports System.Windows.Media

Namespace NovaBrowser
    ' Original 24 x 24 vector artwork. No private-use Unicode or icon font.
    ' Each instance renders its own Drawing; shared frozen geometry is immutable.
    Public NotInheritable Class NovaIcon
        Inherits FrameworkElement

        Public Shared ReadOnly KindProperty As DependencyProperty =
            DependencyProperty.Register(NameOf(Kind), GetType(String), GetType(NovaIcon),
                New FrameworkPropertyMetadata("Globe", FrameworkPropertyMetadataOptions.AffectsRender))
        Public Shared ReadOnly ForegroundProperty As DependencyProperty =
            TextElement.ForegroundProperty.AddOwner(GetType(NovaIcon),
                New FrameworkPropertyMetadata(Brushes.White,
                    FrameworkPropertyMetadataOptions.Inherits Or FrameworkPropertyMetadataOptions.AffectsRender))
        Private Shared ReadOnly Paths As Dictionary(Of String, Geometry) = CreatePaths()

        Public Sub New()
            Width = 18
            Height = 18
            HorizontalAlignment = HorizontalAlignment.Center
            VerticalAlignment = VerticalAlignment.Center
            Focusable = False
            IsHitTestVisible = False
        End Sub

        Public Property Kind As String
            Get
                Return CStr(GetValue(KindProperty))
            End Get
            Set(value As String)
                SetValue(KindProperty, value)
            End Set
        End Property

        Public Property Foreground As Brush
            Get
                Return DirectCast(GetValue(ForegroundProperty), Brush)
            End Get
            Set(value As Brush)
                SetValue(ForegroundProperty, value)
            End Set
        End Property

        Public Shared ReadOnly Property SupportedKinds As IEnumerable(Of String)
            Get
                Return Paths.Keys
            End Get
        End Property

        Protected Overrides Sub OnRender(dc As DrawingContext)
            MyBase.OnRender(dc)
            If ActualWidth <= 0 OrElse ActualHeight <= 0 OrElse Foreground Is Nothing Then Return
            Dim artwork As Geometry = Nothing
            If Not Paths.TryGetValue(If(Kind, ""), artwork) Then artwork = Paths("Globe")
            Dim stroke As New Pen(Foreground, 1.8) With {
                .StartLineCap = PenLineCap.Round, .EndLineCap = PenLineCap.Round, .LineJoin = PenLineJoin.Round}
            Dim scale As Double = Math.Min(ActualWidth, ActualHeight) / 24.0
            dc.PushTransform(New TranslateTransform((ActualWidth - 24 * scale) / 2, (ActualHeight - 24 * scale) / 2))
            dc.PushTransform(New ScaleTransform(scale, scale))
            dc.DrawGeometry(If(Kind = "StarFilled", Foreground, Nothing), stroke, artwork)
            dc.Pop()
            dc.Pop()
        End Sub

        Private Shared Function CreatePaths() As Dictionary(Of String, Geometry)
            Dim data As New Dictionary(Of String, String)(StringComparer.Ordinal) From {
                {"Menu", "M4,6 H20 M4,12 H20 M4,18 H20"},
                {"Home", "M3,10 L12,3 L21,10 M5,9 V21 H10 V14 H14 V21 H19 V9"},
                {"Search", "M10.5,3.5 A7,7 0 1 1 10.5,17.5 A7,7 0 1 1 10.5,3.5 M16,16 L21,21"},
                {"Star", "M12,3 L14.8,8.7 L21,9.6 L16.5,14 L17.6,20.2 L12,17.3 L6.4,20.2 L7.5,14 L3,9.6 L9.2,8.7 Z"},
                {"History", "M3,5 V10 H8 M3.5,9 A9,9 0 1 1 4,17 M12,7 V12 L16,14"},
                {"Download", "M12,3 V15 M7,10 L12,15 L17,10 M4,16 V21 H20 V16"},
                {"Settings", "M9,3 H15 L15.6,6 L18,7.4 L20.7,6.5 L23,11.5 L20.5,13.2 L20,16 L21.5,18.2 L17.5,21.5 L15.1,19.6 L12,20 L9.6,22 L5.5,18.8 L6.3,16 L4.5,13.5 L2,12.8 L3.2,7.5 L6.1,7.6 L8.4,6 Z M12,8 A4,4 0 1 1 12,16 A4,4 0 1 1 12,8"},
                {"Back", "M14,5 L7,12 L14,19 M7,12 H21"},
                {"Forward", "M10,5 L17,12 L10,19 M3,12 H17"},
                {"Refresh", "M20,4 V10 H14 M20,10 A8,8 0 1 0 19,17"},
                {"Stop", "M6,6 H18 V18 H6 Z"},
                {"Globe", "M12,3 A9,9 0 1 1 12,21 A9,9 0 1 1 12,3 M3,12 H21 M12,3 C6,8 6,16 12,21 C18,16 18,8 12,3"},
                {"Lock", "M7,10 V7 A5,5 0 0 1 17,7 V10 M5,10 H19 V21 H5 Z M12,14 V17"},
                {"Plus", "M12,4 V20 M4,12 H20"},
                {"More", "M5,11 A1,1 0 1 1 5,13 A1,1 0 1 1 5,11 M12,11 A1,1 0 1 1 12,13 A1,1 0 1 1 12,11 M19,11 A1,1 0 1 1 19,13 A1,1 0 1 1 19,11"},
                {"Close", "M6,6 L18,18 M18,6 L6,18"},
                {"Minimize", "M5,15 H19"},
                {"Maximize", "M5,5 H19 V19 H5 Z"},
                {"Restore", "M8,4 H20 V16 M4,8 H16 V20 H4 Z"},
                {"Trash", "M4,6 H20 M9,6 V3 H15 V6 M6,6 L7,21 H17 L18,6 M10,10 V17 M14,10 V17"},
                {"ChevronDown", "M5,8 L12,15 L19,8"},
                {"Loading", "M12,3 A9,9 0 1 1 3,12 M3,12 L3,7 M3,12 H8"},
                {"Check", "M4,12 L9,17 L20,6"},
                {"Palette", "M12,3 C6,3 3,7 3,12 C3,17 7,21 12,21 H14 C16,21 16,18 14,17 C12,15 14,13 17,13 H19 C23,13 20,3 12,3 M7,9 L7.1,9 M12,6 L12.1,6 M17,8 L17.1,8"},
                {"Person", "M12,3 A4,4 0 1 1 12,11 A4,4 0 1 1 12,3 M4,21 V19 A8,6 0 0 1 20,19 V21"},
                {"External", "M14,3 H21 V10 M21,3 L10,14 M10,5 H4 V21 H19 V15"},
                {"Info", "M12,3 A9,9 0 1 1 12,21 A9,9 0 1 1 12,3 M12,11 V17 M12,7 L12.1,7"},
                {"StarFilled", "M12,3 L14.8,8.7 L21,9.6 L16.5,14 L17.6,20.2 L12,17.3 L6.4,20.2 L7.5,14 L3,9.6 L9.2,8.7 Z"}}
            Dim result As New Dictionary(Of String, Geometry)(StringComparer.Ordinal)
            For Each pair As KeyValuePair(Of String, String) In data
                Dim shape As Geometry = Geometry.Parse(pair.Value)
                shape.Freeze()
                result.Add(pair.Key, shape)
            Next
            Return result
        End Function
    End Class
End Namespace
