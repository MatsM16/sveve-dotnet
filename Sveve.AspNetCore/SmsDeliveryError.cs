namespace Sveve.AspNetCore;

/// <summary>
/// The reason a SMS delivery failed.
/// </summary>
/// <param name="Code">A well known error code.</param>
/// <param name="Description">A description of the error.</param>
public sealed record SmsDeliveryError(string Code, string Description);