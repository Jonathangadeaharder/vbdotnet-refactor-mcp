Imports System
Imports System.Text

Namespace Utilities

    ''' <summary>
    ''' Provides utility functions for string manipulation
    ''' </summary>
    Public Module StringHelpers

        ''' <summary>
        ''' Capitalizes the first letter of each word
        ''' </summary>
        Public Function ToTitleCase(input As String) As String
            If String.IsNullOrEmpty(input) Then
                Return input
            End If

            Dim words = input.Split(" "c)
            Dim result As New StringBuilder()

            For Each word In words
                If word.Length > 0 Then
                    If result.Length > 0 Then
                        result.Append(" ")
                    End If
                    result.Append(Char.ToUpper(word(0)))
                    If word.Length > 1 Then
                        result.Append(word.Substring(1).ToLower())
                    End If
                End If
            Next

            Return result.ToString()
        End Function

        ''' <summary>
        ''' Truncates a string to the specified length
        ''' </summary>
        Public Function TruncateString(input As String, maxLength As Integer) As String
            If String.IsNullOrEmpty(input) Then
                Return input
            End If

            If input.Length <= maxLength Then
                Return input
            End If

            Return input.Substring(0, maxLength - 3) & "..."
        End Function

        ''' <summary>
        ''' Checks if a string is a valid email format
        ''' </summary>
        Public Function IsValidEmail(email As String) As Boolean
            If String.IsNullOrEmpty(email) Then
                Return False
            End If

            ' Simple validation
            Return email.Contains("@") AndAlso email.Contains(".")
        End Function

        ''' <summary>
        ''' Removes special characters from a string
        ''' </summary>
        Public Function RemoveSpecialCharacters(input As String) As String
            If String.IsNullOrEmpty(input) Then
                Return input
            End If

            Dim result As New StringBuilder()
            For Each c As Char In input
                If Char.IsLetterOrDigit(c) OrElse c = " "c Then
                    result.Append(c)
                End If
            Next

            Return result.ToString()
        End Function

        ''' <summary>
        ''' Counts the number of words in a string
        ''' </summary>
        Public Function CountWords(input As String) As Integer
            If String.IsNullOrEmpty(input) Then
                Return 0
            End If

            Dim words = input.Split(New Char() {" "c, vbTab, vbCr, vbLf}, StringSplitOptions.RemoveEmptyEntries)
            Return words.Length
        End Function

    End Module

End Namespace
