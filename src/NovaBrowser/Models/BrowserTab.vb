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
        Public ReadOnly Property IconKind As String
            Get
                If IsLoading Then Return "Loading"
                If IsPrivate Then Return "Lock"
                Return If(IsHome, "Home", "Globe")
            End Get
        End Property
        Public ReadOnly Property Subtitle As String
            Get
                Dim host = If(IsHome, "Startside", UrlPolicy.DisplayHost(Address))
                Return If(IsPrivate, "Privat · " & host, host)
            End Get
        End Property
        Public Sub NotifyLocation()
            Notify(NameOf(IconKind))
            Notify(NameOf(Subtitle))
        End Sub
    End Class
End Namespace
