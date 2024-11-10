using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Sveve;

/// <summary>
/// Defines how messages should be repeated.
/// </summary>
[DebuggerDisplay("{ToString,nq}")]
public sealed class SendRepeat
{
    private const int NotDefined = 0;
    private const int HourUnit = 11;
    private const int DayUnit = 5;
    private const int MonthUnit = 2;
    private const int WeekUnit = 99;

    private readonly int _unit;
    private readonly int _value;
    private readonly int? _times;
    private readonly DateTime? _until;

    private SendRepeat(int unit, int value, int? times = null, DateTime? until = null)
    {
        if (value < 1)
            throw new ArgumentOutOfRangeException(nameof(value));

        if (times.HasValue && times.Value < 1)
            throw new ArgumentOutOfRangeException(nameof(times));

        _unit = unit;
        _value = value;
        _times = times;
        _until = until;
    }

    /// <summary>
    /// Creates a new <see cref="SendRepeat"/> that never stops repeating.
    /// </summary>
    public static SendRepeat Never { get; } = new (NotDefined, NotDefined);

    /// <summary>
    /// Creates a new <see cref="SendRepeat"/> that repeats every <paramref name="hours"/> hours.
    /// </summary>
    /// <remarks>
    /// This repetition never stops. You probably want a stop condition.
    /// </remarks>
    /// <param name="hours">Number of hours between each subsequent sms.</param>
    public static SendRepeat Hourly(int hours = 1) => new(hours, HourUnit);

    /// <summary>
    /// Creates a new <see cref="SendRepeat"/> that repeats every <paramref name="days"/> days.
    /// </summary>
    /// <remarks>
    /// This repetition never stops. You probably want a stop condition.
    /// </remarks>
    /// <param name="days">Number of days between each subsequent sms.</param>
    public static SendRepeat Daily(int days = 1) => new (days, DayUnit);

    /// <summary>
    /// Creates a new <see cref="SendRepeat"/> that repeats every <paramref name="weeks"/> weeks.
    /// </summary>
    /// <remarks>
    /// This repetition never stops. You probably want a stop condition.
    /// </remarks>
    /// <param name="weeks">Number of weeks between each subsequent sms.</param>
    public static SendRepeat Weekly(int weeks = 1) => new(weeks, WeekUnit);

    /// <summary>
    /// Creates a new <see cref="SendRepeat"/> that repeats every <paramref name="months"/> months.
    /// </summary>
    /// <remarks>
    /// This repetition never stops. You probably want a stop condition.
    /// </remarks>
    /// <param name="months">Number of months between each subsequent sms.</param>
    public static SendRepeat Monthly(int months = 1) => new(months, MonthUnit);

    /// <summary>
    /// Creates a copy of this repetition that stops after the sms has been sent <paramref name="sendCount"/> times.
    /// </summary>
    /// <remarks>
    /// This overrides existing stop-condition if any.
    /// </remarks>
    /// <param name="sendCount">Number of times the messages will be sent.</param>
    public SendRepeat Times(int sendCount) => new (_unit, _value, times: sendCount);

    /// <summary>
    /// Creates a copy of this repetition that stops after the given <paramref name="date"/>.
    /// </summary>
    /// <remarks>
    /// This overrides existing stop-condition if any.
    /// </remarks>
    /// <param name="date">Date after which the messages stops repeating.</param>
    public SendRepeat Until(DateTime date) => new(_unit, _value, until: date);

    /// <summary>
    /// Returns a string representation of the repetition.
    /// </summary>
    /// <remarks>
    /// The returned value can later be used to create the exact same repetition by calling <see cref="Parse"/>
    /// </remarks>
    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.Append("unit=").Append(_unit switch
        {
            HourUnit => "hour",
            DayUnit => "day",
            WeekUnit => "week",
            MonthUnit => "month",
            _ => "unknown"
        });
        if (_value > 1)
            sb.Append(";value=").Append(_value);
        if (_times.HasValue)
            sb.Append(";times=").Append(_times.Value);
        if (_until.HasValue)
            sb.Append(";until=").Append(_until.Value.ToString("dd.MM.yyyy"));
        return sb.ToString();
    }

    /// <summary>
    /// Parses a string obtained using <see cref="ToString"/> into a new <see cref="SendRepeat"/> instance.
    /// </summary>
    public static SendRepeat Parse(string? sendRepeatString)
    {
        if (sendRepeatString is null)
            return Never;

        var properties = sendRepeatString.Split(';').Select(s => s.Split('=')).Where(a => a.Length is 2).ToDictionary(x => x[0], x => x[1]);
        if (!properties.TryGetValue("unit", out var unitString))
            return Never;

        if (!properties.TryGetValue("value", out var valueString) || !int.TryParse(valueString, out var value) || value < 1)
            value = 1;

        var unit = unitString.ToLower() switch
        {
            "hour" => HourUnit,
            "day" => DayUnit,
            "week" => WeekUnit,
            "month" => MonthUnit,
            _ => NotDefined
        };

        if (unit is NotDefined)
            return Never;

        if (properties.TryGetValue("times", out var timesString) && int.TryParse(timesString, out var times))
            return new SendRepeat(unit, value, times: times);

        if (properties.TryGetValue("until", out var untilString) && DateTime.TryParseExact(untilString, "dd.MM.yyyy", null, System.Globalization.DateTimeStyles.None, out var until))
            return new SendRepeat(unit, value, until: until);

        return new SendRepeat(unit, value);
    }

    internal void AddProperties(IDictionary<string, object?> properties)
    {
        if (_unit is NotDefined && _value is NotDefined)
            return;

        properties["reoccurrence"] = $"{_value}|{_unit}";

        if (_times.HasValue)
        {
            properties["reoccurrence_ends"] = "after";
            properties["ends_after"] = _times.Value;
        }
        else if (_until.HasValue)
        {
            properties["reoccurrence_ends"] = "on";
            properties["ends_on"] = _until.Value.ToString("dd.MM.yyyy");
        }
        else
        {
            properties["reoccurrence_ends"] = "never";
        }
    }
}