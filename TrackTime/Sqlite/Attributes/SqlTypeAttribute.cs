namespace TrackTime.Sqlite.Attributes;

[AttributeUsage(AttributeTargets.Property)]
internal class SqlTypeAttribute(SqlType type) : Attribute
{
    public SqlType SqlType { get; init; } = type;
}
