Imports System
Imports System.IO
Imports System.Runtime.InteropServices
Imports System.Windows
Imports System.Windows.Interop
Imports System.Windows.Media.Imaging

Namespace NovaBrowser.ShellChecks
    Friend NotInheritable Class NativeSnapshot
        <StructLayout(LayoutKind.Sequential)>
        Private Structure NativeRect
            Public Left As Integer
            Public Top As Integer
            Public Right As Integer
            Public Bottom As Integer
        End Structure
        <DllImport("user32.dll", EntryPoint:="GetWindowLongW")>
        Private Shared Function GetWindowLong(hwnd As IntPtr, index As Integer) As Integer
        End Function
        <DllImport("user32.dll")>
        Private Shared Function GetWindowRect(hwnd As IntPtr, ByRef rect As NativeRect) As Boolean
        End Function
        <DllImport("user32.dll")>
        Private Shared Function GetDC(hwnd As IntPtr) As IntPtr
        End Function
        <DllImport("user32.dll")>
        Private Shared Function ReleaseDC(hwnd As IntPtr, dc As IntPtr) As Integer
        End Function
        <DllImport("user32.dll")>
        Private Shared Function PrintWindow(hwnd As IntPtr, dc As IntPtr, flags As UInteger) As Boolean
        End Function
        <DllImport("gdi32.dll")>
        Private Shared Function CreateCompatibleDC(dc As IntPtr) As IntPtr
        End Function
        <DllImport("gdi32.dll")>
        Private Shared Function CreateCompatibleBitmap(dc As IntPtr, width As Integer, height As Integer) As IntPtr
        End Function
        <DllImport("gdi32.dll")>
        Private Shared Function SelectObject(dc As IntPtr, item As IntPtr) As IntPtr
        End Function
        <DllImport("gdi32.dll")>
        Private Shared Function DeleteObject(item As IntPtr) As Boolean
        End Function
        <DllImport("gdi32.dll")>
        Private Shared Function DeleteDC(dc As IntPtr) As Boolean
        End Function
        Public Shared Function HasCaptionButtons(window As Window) As Boolean
            Dim style = GetWindowLong(New WindowInteropHelper(window).Handle, -16)
            Const required As Integer = &HC00000 Or &H80000 Or &H40000 Or &H20000 Or &H10000
            Return (style And required) = required
        End Function
        Public Shared Sub Capture(window As Window, destination As String)
            Dim handle = New WindowInteropHelper(window).Handle
            Dim rectangle As New NativeRect()
            If Not GetWindowRect(handle, rectangle) Then Return
            Dim dc = GetDC(handle)
            Dim memory As IntPtr = IntPtr.Zero
            Dim bitmap As IntPtr = IntPtr.Zero
            Dim previous As IntPtr = IntPtr.Zero
            Try
                memory = CreateCompatibleDC(dc)
                bitmap = CreateCompatibleBitmap(dc, rectangle.Right - rectangle.Left, rectangle.Bottom - rectangle.Top)
                If memory = IntPtr.Zero OrElse bitmap = IntPtr.Zero Then Return
                previous = SelectObject(memory, bitmap)
                If Not PrintWindow(handle, memory, 2UI) Then
                    Console.WriteLine("INFO: Native PrintWindow capture unavailable; WPF client renders are retained.")
                    Return
                End If
                Dim image = Imaging.CreateBitmapSourceFromHBitmap(bitmap, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions())
                Dim encoder As New PngBitmapEncoder()
                encoder.Frames.Add(BitmapFrame.Create(image))
                Using stream = File.Create(destination)
                    encoder.Save(stream)
                End Using
            Finally
                If previous <> IntPtr.Zero Then SelectObject(memory, previous)
                If bitmap <> IntPtr.Zero Then DeleteObject(bitmap)
                If memory <> IntPtr.Zero Then DeleteDC(memory)
                If dc <> IntPtr.Zero Then ReleaseDC(handle, dc)
            End Try
        End Sub
    End Class
End Namespace
