Imports System
Imports System.IO
Imports Microsoft.Web.WebView2.Core

Namespace NovaBrowser
    Public Class DownloadRow
        Inherits ObservableObject
        Public ReadOnly Property Operation As CoreWebView2DownloadOperation
        Public ReadOnly Property OwnerId As Guid
        Public ReadOnly Property IsPrivate As Boolean
        Public ReadOnly Property FilePath As String
        Public ReadOnly Property FileName As String
        Private _received As Double
        Private _total As Double
        Private _state As CoreWebView2DownloadState = CoreWebView2DownloadState.InProgress
        Private _canResume As Boolean
        Private _reason As String = ""
        Public Sub New(operation As CoreWebView2DownloadOperation, owner As BrowserTab, destination As String)
            Me.Operation = operation
            OwnerId = owner.Id
            IsPrivate = owner.IsPrivate
            FilePath = destination
            FileName = Path.GetFileName(destination)
            Capture()
        End Sub
        Public Sub Capture()
            _received = CDbl(Operation.BytesReceived)
            Dim total = Operation.TotalBytesToReceive
            _total = If(total.HasValue, CDbl(total.GetValueOrDefault()), 0)
            _state = Operation.State
            _canResume = Operation.CanResume
            _reason = Operation.InterruptReason.ToString()
            Changed()
        End Sub
        Public Sub MarkStopped(reason As String)
            _state = CoreWebView2DownloadState.Interrupted
            _canResume = False
            _reason = reason
            Changed()
        End Sub
        Private Sub Changed()
            For Each propertyName In New String() {NameOf(Status), NameOf(Percent), NameOf(UnknownSize), NameOf(IsActive), NameOf(IsComplete), NameOf(ControlLabel)}
                Notify(propertyName)
            Next
        End Sub
        Public ReadOnly Property IsActive As Boolean
            Get
                Return _state = CoreWebView2DownloadState.InProgress OrElse (_state = CoreWebView2DownloadState.Interrupted AndAlso _canResume)
            End Get
        End Property
        Public ReadOnly Property IsComplete As Boolean
            Get
                Return _state = CoreWebView2DownloadState.Completed
            End Get
        End Property
        Public ReadOnly Property ControlLabel As String
            Get
                Return If(_state = CoreWebView2DownloadState.InProgress, "Pause", "Fortsett")
            End Get
        End Property
        Public ReadOnly Property UnknownSize As Boolean
            Get
                Return _total <= 0 AndAlso _state = CoreWebView2DownloadState.InProgress
            End Get
        End Property
        Public ReadOnly Property Percent As Double
            Get
                If IsComplete Then Return 100
                Return If(_total > 0, Math.Clamp(_received * 100 / _total, 0, 100), 0)
            End Get
        End Property
        Public ReadOnly Property Status As String
            Get
                Dim prefix = If(IsPrivate, "Privat / ", "")
                If IsComplete Then Return prefix & "Ferdig / " & SizeLabel(_received)
                If _state = CoreWebView2DownloadState.Interrupted Then Return prefix & If(_canResume, "Pauset", "Stanset") & " / " & _reason
                Return prefix & SizeLabel(_received) & If(_total > 0, " av " & SizeLabel(_total), " / laster ned")
            End Get
        End Property
        Private Shared Function SizeLabel(bytes As Double) As String
            If bytes >= 1024.0 * 1024 * 1024 Then Return (bytes / (1024.0 * 1024 * 1024)).ToString("0.0") & " GB"
            If bytes >= 1024.0 * 1024 Then Return (bytes / (1024.0 * 1024)).ToString("0.0") & " MB"
            Return (bytes / 1024).ToString("0") & " KB"
        End Function
    End Class
End Namespace
