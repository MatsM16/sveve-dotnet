using System;
using System.Collections.Generic;
using System.Text;

namespace Sveve;

/// <summary>
/// Defines how messages should be repeated.
/// </summary>
public sealed class SendRepetition
{
    private const int NotDefined = 0;
    private const int HourUnit = 11;
    private const int DayUnit = 5;
    private const int MonthUnit = 2;
    private const int WeekUnit = 99;

    private readonly int _unit;
    private readonly int _value;
    private readonly int? _endsAfter;
    private readonly DateTime? _endsOn;

    private SendRepetition(int unit, int value, int? endsAfter = null, DateTime? endsOn = null)
    {
        if (unit < 0)
            throw new ArgumentOutOfRangeException(nameof(unit));

        if (value < 0)
            throw new ArgumentOutOfRangeException(nameof(value));

        if (endsAfter.HasValue && endsAfter.Value < 0)
            throw new ArgumentOutOfRangeException(nameof(endsAfter));

        _unit = unit;
        _value = value;
        _endsAfter = endsAfter;
        _endsOn = endsOn;
    }

    /// <summary>
    /// Creates a new <see cref="SendRepetition"/> that never stops repeating.
    /// </summary>
    public static SendRepetition Never { get; } = new (NotDefined, NotDefined);

    /// <summary>
    /// Creates a new <see cref="SendRepetition"/> that repeats every <paramref name="hours"/> hours.
    /// </summary>
    /// <param name="hours">Number of hours between each subsequent sms.</param>
    public static SendRepetition Hourly(int hours) => new(hours, HourUnit);

    /// <summary>
    /// Creates a new <see cref="SendRepetition"/> that repeats every <paramref name="days"/> days.
    /// </summary>
    /// <param name="days">Number of days between each subsequent sms.</param>
    public static SendRepetition Daily(int days) => new (days, DayUnit);

    /// <summary>
    /// Creates a new <see cref="SendRepetition"/> that repeats every <paramref name="weeks"/> weeks.
    /// </summary>
    /// <param name="weeks">Number of weeks between each subsequent sms.</param>
    public static SendRepetition Weekly(int weeks) => new(weeks, WeekUnit);

    /// <summary>
    /// Creates a new <see cref="SendRepetition"/> that repeats every <paramref name="months"/> months.
    /// </summary>
    /// <param name="months">Number of months between each subsequent sms.</param>
    public static SendRepetition Monthly(int months) => new(months, MonthUnit);

    /// <summary>
    /// Creates a new <see cref="SendRepetition"/> with the same frequency that stops after the given number of <paramref name="repeatedCount"/> times.
    /// </summary>
    /// <remarks>
    /// This overrides existing endings.
    /// </remarks>
    /// <param name="repeatedCount">Number of times the messages will be sent.</param>
    public SendRepetition EndsAfter(int repeatedCount) => new (_unit, _value, endsAfter: _endsAfter);

    /// <summary>
    /// Creates a new <see cref="SendRepetition"/> with the same frequency that stops after the given <paramref name="date"/>.
    /// </summary>
    /// <remarks>
    /// This overrides existing endings.
    /// </remarks>
    /// <param name="date">Date after which the messages stops repeating.</param>
    public SendRepetition EndsOn(DateTime date) => new(_unit, _value, endsOn: date);

    /// <summary>
    /// Returns the reoccurrence formatted like URL query parameters.
    /// </summary>
    public override string ToString()
    {
        var properties = new Dictionary<string, object>();
        AddProperties(properties);

        var sb = new StringBuilder();
        foreach (var pair in properties)
            sb.Append(pair.Key).Append('=').Append(pair.Value);
        return sb.ToString();
    }

    internal void AddProperties(IDictionary<string, object> properties)
    {
        if (_unit is NotDefined && _value is NotDefined)
            return;

        properties["reoccurrence"] = $"{_value}|{_unit}";

        if (_endsAfter.HasValue)
        {
            properties["reoccurrence_ends"] = "after";
            properties["ends_after"] = _endsAfter.Value;
        }
        else if (_endsOn.HasValue)
        {
            properties["reoccurrence_ends"] = "on";
            properties["ends_on"] = _endsOn.Value.ToString("dd.MM.yyyy");
        }
        else
        {
            properties["reoccurrence_ends"] = "never";
        }
    }
}