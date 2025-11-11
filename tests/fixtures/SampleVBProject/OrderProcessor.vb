Imports System
Imports System.Collections.Generic
Imports System.Linq

Namespace Business

    ''' <summary>
    ''' Processes customer orders
    ''' </summary>
    Public Class OrderProcessor
        Private _taxRate As Decimal = 0.08D
        Private _shippingCost As Decimal = 5.99D

        ''' <summary>
        ''' Processes an order and calculates total cost
        ''' </summary>
        Public Function ProcessOrder(items As List(Of OrderItem), customer As Customer) As OrderResult
            ' Validate inputs
            If items Is Nothing OrElse items.Count = 0 Then
                Throw New ArgumentException("Order must contain at least one item")
            End If

            If customer Is Nothing Then
                Throw New ArgumentNullException(NameOf(customer))
            End If

            ' Calculate subtotal
            Dim subtotal As Decimal = 0
            For Each item In items
                subtotal += item.Price * item.Quantity
            Next

            ' Apply customer discount
            Dim discount As Decimal = customer.CalculateDiscount()
            Dim discountAmount As Decimal = subtotal * discount

            ' Calculate tax
            Dim taxableAmount As Decimal = subtotal - discountAmount
            Dim taxAmount As Decimal = taxableAmount * _taxRate

            ' Calculate shipping
            Dim shipping As Decimal = _shippingCost
            If subtotal >= 50 Then
                shipping = 0 ' Free shipping for orders over $50
            End If

            ' Calculate total
            Dim total As Decimal = taxableAmount + taxAmount + shipping

            ' Create result
            Dim result As New OrderResult()
            result.Subtotal = subtotal
            result.DiscountAmount = discountAmount
            result.TaxAmount = taxAmount
            result.ShippingCost = shipping
            result.Total = total
            result.CustomerName = customer.CustomerName

            Return result
        End Function

        ''' <summary>
        ''' Validates order items for availability
        ''' </summary>
        Public Function ValidateOrderItems(items As List(Of OrderItem)) As Boolean
            If items Is Nothing Then
                Return False
            End If

            For Each item In items
                If item.Quantity <= 0 Then
                    Return False
                End If
                If item.Price < 0 Then
                    Return False
                End If
            Next

            Return True
        End Function

        ''' <summary>
        ''' Calculates estimated delivery date
        ''' </summary>
        Public Function GetEstimatedDeliveryDate(orderDate As DateTime, expedited As Boolean) As DateTime
            Dim businessDays As Integer = If(expedited, 2, 5)
            Dim deliveryDate As DateTime = orderDate

            Dim daysAdded As Integer = 0
            While daysAdded < businessDays
                deliveryDate = deliveryDate.AddDays(1)
                ' Skip weekends
                If deliveryDate.DayOfWeek <> DayOfWeek.Saturday AndAlso deliveryDate.DayOfWeek <> DayOfWeek.Sunday Then
                    daysAdded += 1
                End If
            End While

            Return deliveryDate
        End Function
    End Class

    ''' <summary>
    ''' Represents an item in an order
    ''' </summary>
    Public Class OrderItem
        Public Property ProductName As String
        Public Property Price As Decimal
        Public Property Quantity As Integer
    End Class

    ''' <summary>
    ''' Represents the result of processing an order
    ''' </summary>
    Public Class OrderResult
        Public Property Subtotal As Decimal
        Public Property DiscountAmount As Decimal
        Public Property TaxAmount As Decimal
        Public Property ShippingCost As Decimal
        Public Property Total As Decimal
        Public Property CustomerName As String
    End Class

End Namespace
