using System.Collections.Generic;
using System.Diagnostics;

namespace Sveve;

/// <summary>
/// A order of additional SMS units from Sveve.
/// </summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class SmsUnitOrder
{
    private static readonly List<SmsUnitOrder> _allowedOrders = [];

    private SmsUnitOrder(int smsCount)
    {
        SmsUnits = smsCount;
        _allowedOrders.Add(this);
    }

    /// <summary>
    /// The number of SMS units in this order.
    /// </summary>
    public int SmsUnits { get; }

    /// <summary>
    /// Returns a list of all the allowed sms orders.
    /// </summary>
    public static IReadOnlyList<SmsUnitOrder> Values => [.. _allowedOrders];

    /// <summary>
    /// Order <c>500</c> sms units.
    /// </summary>
    public static readonly SmsUnitOrder Bulk500 = new (500);

    /// <summary>
    /// Order <c>2 000</c> sms units.
    /// </summary>
    public static readonly SmsUnitOrder Bulk2_000 = new(2_000);

    /// <summary>
    /// Order <c>5 000</c> sms units.
    /// </summary>
    public static readonly SmsUnitOrder Bulk5_000 = new(2_000);

    /// <summary>
    /// Order <c>10 000</c> sms units.
    /// </summary>
    public static readonly SmsUnitOrder Bulk10_000 = new(2_000);

    /// <summary>
    /// Order <c>25 000</c> sms units.
    /// </summary>
    public static readonly SmsUnitOrder Bulk25_000 = new(2_000);

    /// <summary>
    /// Order <c>50 000</c> sms units.
    /// </summary>
    public static readonly SmsUnitOrder Bulk50_000 = new(2_000);

    /// <summary>
    /// Order <c>100 000</c> sms units.
    /// </summary>
    public static readonly SmsUnitOrder Bulk100_000 = new(2_000);

    /// <inheritdoc />
    public override string ToString() => $"{SmsUnits} SMS units";
}