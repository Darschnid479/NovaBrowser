Imports System
Imports System.Linq
Imports System.Threading.Tasks
Imports System.Windows
Imports System.Windows.Controls
Imports System.Windows.Input

Namespace NovaBrowser
    Partial Public Class MainWindow
        Private _updatingAddress As Boolean

        Private Sub AddressBox_TextChanged(sender As Object, args As TextChangedEventArgs)
            RefreshAddressSuggestions()
        End Sub
        Private Sub RefreshAddressSuggestions()
            If _updatingAddress OrElse AddressSuggestions Is Nothing OrElse _active Is Nothing OrElse Not AddressBox.IsKeyboardFocusWithin Then Return
            If Not ShellBody.IsEnabled Then Return
            Dim snapshot = _tabs.Select(Function(item) New TabSnapshot With {.Id = item.Id, .Title = item.Title, .Address = item.Address, .IsPrivate = item.IsPrivate})
            SuggestionList.ItemsSource = LocalSuggestions.Find(AddressBox.Text, snapshot, _state.Bookmarks, _state.History, _active.IsPrivate)
            SuggestionList.SelectedIndex = -1
            AddressSuggestions.IsOpen = SuggestionList.Items.Count > 0
        End Sub
        Private Sub CloseAddressSuggestions()
            If AddressSuggestions IsNot Nothing Then AddressSuggestions.IsOpen = False
        End Sub
        Private Async Function AcceptAddressSuggestionAsync() As Task
            Dim selected = TryCast(SuggestionList.SelectedItem, AddressSuggestion)
            CloseAddressSuggestions()
            If selected Is Nothing Then Return
            If selected.TabId <> Guid.Empty Then
                Dim target = _tabs.FirstOrDefault(Function(item) item.Id = selected.TabId)
                If target IsNot Nothing Then
                    TabList.SelectedItem = target
                    FocusActivePage()
                End If
            Else
                Await NavigateAsync(_active, selected.Address)
            End If
        End Function
        Private Async Sub SuggestionList_Click(sender As Object, args As MouseButtonEventArgs)
            Dim row = ItemsControl.ContainerFromElement(SuggestionList, TryCast(args.OriginalSource, DependencyObject))
            If row IsNot Nothing Then Await AcceptAddressSuggestionAsync()
        End Sub
        Private Async Sub SuggestionList_KeyDown(sender As Object, args As KeyEventArgs)
            If args.Key = Key.Enter Then
                args.Handled = True
                Await AcceptAddressSuggestionAsync()
            ElseIf args.Key = Key.Escape Then
                args.Handled = True
                CloseAddressSuggestions()
                AddressBox.Focus()
            End If
        End Sub
    End Class
End Namespace
