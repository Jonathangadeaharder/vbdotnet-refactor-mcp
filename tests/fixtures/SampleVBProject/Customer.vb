Imports System

Namespace Business

    ''' <summary>
    ''' Represents a customer in the system
    ''' </summary>
    Public Class Customer
        Private _customerName As String
        Private _customerAge As Integer
        Private _emailAddress As String

        Public Sub New(name As String, age As Integer, email As String)
            _customerName = name
            _customerAge = age
            _emailAddress = email
        End Sub

        ''' <summary>
        ''' Gets or sets the customer name
        ''' </summary>
        Public Property CustomerName As String
            Get
                Return _customerName
            End Get
            Set(value As String)
                _customerName = value
            End Set
        End Property

        ''' <summary>
        ''' Gets or sets the customer age
        ''' </summary>
        Public Property CustomerAge As Integer
            Get
                Return _customerAge
            End Get
            Set(value As Integer)
                If value >= 0 Then
                    _customerAge = value
                End If
            End Set
        End Property

        ''' <summary>
        ''' Gets or sets the email address
        ''' </summary>
        Public Property EmailAddress As String
            Get
                Return _emailAddress
            End Get
            Set(value As String)
                _emailAddress = value
            End Set
        End Property

        ''' <summary>
        ''' Validates if customer is eligible for premium service
        ''' </summary>
        Public Function IsEligibleForPremium() As Boolean
            Return _customerAge >= 18 AndAlso Not String.IsNullOrEmpty(_emailAddress)
        End Function

        ''' <summary>
        ''' Gets a formatted display name
        ''' </summary>
        Public Function GetDisplayName() As String
            Return String.Format("{0} (Age: {1})", _customerName, _customerAge)
        End Function

        ''' <summary>
        ''' Calculates discount percentage based on age
        ''' </summary>
        Public Function CalculateDiscount() As Decimal
            If _customerAge >= 65 Then
                Return 0.2D ' 20% senior discount
            ElseIf _customerAge >= 18 Then
                Return 0.1D ' 10% adult discount
            Else
                Return 0.05D ' 5% youth discount
            End If
        End Function
    End Class

End Namespace
