Imports System
Imports System.Collections.Generic
Imports System.ComponentModel
Imports System.Runtime.CompilerServices
Imports System.Threading.Tasks
Imports Microsoft.Web.WebView2.Wpf

Namespace NovaBrowser
    Public Class ObservableObject
        Implements INotifyPropertyChanged
        Public Event PropertyChanged As PropertyChangedEventHandler Implements INotifyPropertyChanged.PropertyChanged
        Protected Function SetValue(Of T)(ByRef field As T, value As T, <CallerMemberName> Optional name As String = Nothing) As Boolean
            If EqualityComparer(Of T).Default.Equals(field, value) Then Return False
            field = value
            RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs(name))
            Return True
        End Function
        Protected Sub Notify(name As String)
            RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs(name))
        End Sub
    End Class

    Public Class BrowserTab
        Inherits ObservableObject
        Private _title As String = "Ny fane"
        Private _address As String = UrlPolicy.HomeUrl
        Private _loading As Boolean
        Private _private As Boolean
        Private _error As String = ""
        Public ReadOnly Property Id As Guid = Guid.NewGuid()
        Public Property View As WebView2CompositionControl
        Public Property Initialization As Task(Of Boolean)
        Public Property Closed As Boolean
        Public Property NavigationVersion As Integer
        Public Property ViewEpoch As Integer
        Public Property InitializationCancellation As System.Threading.CancellationTokenSource
        Public Property DocumentVersion As Integer
        Public Property CurrentNavigationId As ULong
        Public Property HomeBlankPending As Boolean
        Public Property LastActiveUtc As DateTimeOffset = DateTimeOffset.UtcNow
        Public Property SuspendPending As Boolean
        Public Property HasSensitivePermission As Boolean
        Public Property Zoom As Double = 1.0
        Public Property LastHistoryUrl As String = ""
        Public Property IsHome As Boolean = True
        Public Property Title As String
            Get
                Return _title
            End Get
            Set(value As String)
                SetValue(_title, value)
            End Set
        End Property
        Public Property Address As String
            Get
                Return _address
            End Get
            Set(value As String)
                If SetValue(_address, value) Then Notify(NameOf(Subtitle))
            End Set
        End Property
        Public Property IsLoading As Boolean
            Get
                Return _loading
            End Get
            Set(value As Boolean)
                If SetValue(_loading, value) Then Notify(NameOf(IconKind))
            End Set
        End Property
        Public Property IsPrivate As Boolean
            Get
                Return _private
            End Get
            Set(value As Boolean)
                If SetValue(_private, value) Then
                    Notify(NameOf(IconKind))
                    Notify(NameOf(Subtitle))
                End If
            End Set
        End Property
        Public Property ErrorMessage As String
            Get
                Return _error
            End Get
            Set(value As String)
                SetValue(_error, value)
            End Set
        End Property
        Private _pinned As Boolean
        Public Property IsPinned As Boolean
            Get
                Return _pinned
            End Get
            Set(value As Boolean)
                If SetValue(_pinned, value) Then NotifyLocation()
            End Set
        End Property
        Private _muted As Boolean
        Public Property IsMuted As Boolean
            Get
                Return _muted
            End Get
            Set(value As Boolean)
                If SetValue(_muted, value) Then NotifyLocation()
            End Set
        End Property
        Private _sleeping As Boolean
        Public Property IsSleeping As Boolean
            Get
                Return _sleeping
            End Get
            Set(value As Boolean)
                If SetValue(_sleeping, value) Then NotifyLocation()
            End Set
        End Property
        Private _audio As Boolean
        Public Property IsPlayingAudio As Boolean
            Get
                Return _audio
            End Get
            Set(value As Boolean)
                If SetValue(_audio, value) Then NotifyLocation()
            End Set
        End Property
        Public ReadOnly Property IconKind As String
            Get
                If IsLoading Then Return "Loading"
                If IsSleeping Then Return "Sleep"
                If IsMuted Then Return "Mute"
                If IsPlayingAudio Then Return "Volume"
                If IsPinned Then Return "Pin"
                If IsPrivate Then Return "Lock"
                Return If(IsHome, "Home", "Globe")
            End Get
        End Property
        Public ReadOnly Property Subtitle As String
            Get
                Dim host = If(IsHome, "Startside", UrlPolicy.DisplayHost(Address))
                Return If(IsPrivate, "Privat / ", "") & If(IsPinned, "Festet / ", "") & If(IsSleeping, "Hviler / ", If(IsMuted, "Dempet / ", "")) & host
            End Get
        End Property
        Public Sub NotifyLocation()
            Notify(NameOf(IconKind))
            Notify(NameOf(Subtitle))
        End Sub
    End Class
End Namespace
