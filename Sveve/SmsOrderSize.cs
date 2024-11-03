using System.Collections.Generic;
using System.Diagnostics;

namespace Sveve;

/// <summary>
/// Allowed sms order bulk sizes.
/// </summary>
[DebuggerDisplay("{ToString()}")]
public sealed class SmsOrderSize
{
    private static readonly List<SmsOrderSize> _allowedOrders = [];

    private SmsOrderSize(int smsCount)
    {
        SmsCount = smsCount;
        _allowedOrders.Add(this);
    }

    /// <summary>
    /// The number of SMS units in this order.
    /// </summary>
    public int SmsCount { get; }

    /// <summary>
    /// Returns a list of all the allowed sms orders.
    /// </summary>
    public static IReadOnlyList<SmsOrderSize> Values => [.. _allowedOrders];

    /// <summary>
    /// Order <c>500</c> sms units.
    /// </summary>
    public static readonly SmsOrderSize Bulk500 = new (500);

    /// <summary>
    /// Order <c>2 000</c> sms units.
    /// </summary>
    public static readonly SmsOrderSize Bulk2_000 = new(2_000);

    /// <summary>
    /// Order <c>5 000</c> sms units.
    /// </summary>
    public static readonly SmsOrderSize Bulk5_000 = new(2_000);

    /// <summary>
    /// Order <c>10 000</c> sms units.
    /// </summary>
    public static readonly SmsOrderSize Bulk10_000 = new(2_000);

    /// <summary>
    /// Order <c>25 000</c> sms units.
    /// </summary>
    public static readonly SmsOrderSize Bulk25_000 = new(2_000);

    /// <summary>
    /// Order <c>50 000</c> sms units.
    /// </summary>
    public static readonly SmsOrderSize Bulk50_000 = new(2_000);

    /// <summary>
    /// Order <c>100 000</c> sms units.
    /// </summary>
    public static readonly SmsOrderSize Bulk100_000 = new(2_000);

    /// <inheritdoc />
    public override string ToString() => $"{SmsCount} SMS units";
}