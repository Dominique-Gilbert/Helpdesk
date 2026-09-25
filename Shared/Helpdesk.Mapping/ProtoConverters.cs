using Google.Protobuf.WellKnownTypes;

namespace Helpdesk.Mapping;

/// <summary>
/// The primitive conversions every domain needs on the EF-entity -> proto-message hop.
/// Shared so the four services cannot drift from one another on date handling.
/// </summary>
public static class ProtoConverters
{
    public static string ToProto(Guid value) => value.ToString();

    public static string ToProto(Guid? value) => value?.ToString() ?? string.Empty;

    public static Guid ToGuid(string? value) =>
        Guid.TryParse(value, out var parsed) ? parsed : Guid.Empty;

    public static Guid? ToNullableGuid(string? value) =>
        Guid.TryParse(value, out var parsed) ? parsed : null;

    /// <summary>Protobuf timestamps are UTC by definition; force the kind rather than trust it.</summary>
    public static Timestamp ToProto(DateTime value) =>
        Timestamp.FromDateTime(DateTime.SpecifyKind(value, DateTimeKind.Utc));

    public static Timestamp? ToProto(DateTime? value) =>
        value.HasValue ? ToProto(value.Value) : null;

    public static DateTime ToDateTime(Timestamp? value) =>
        value?.ToDateTime() ?? default;

    public static DateTime? ToNullableDateTime(Timestamp? value) =>
        value?.ToDateTime();

    /// <summary>Proto3 strings have no "unset" state, only "" - for a field that's genuinely
    /// optional on the domain side (nullable), "" round-trips as null rather than as an empty
    /// string that would otherwise read as "set to nothing" everywhere downstream.</summary>
    public static string? ToNullableString(string? value) =>
        string.IsNullOrEmpty(value) ? null : value;

    public static TEnum ToEnum<TEnum>(string? value, TEnum fallback) where TEnum : struct, System.Enum =>
        System.Enum.TryParse<TEnum>(value, ignoreCase: true, out var parsed) ? parsed : fallback;
}
