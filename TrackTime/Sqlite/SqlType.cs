namespace TrackTime.Sqlite;

internal enum SqlType : byte
{
    /// <summary>Examples: <see cref="Nullable"/></summary>
    Null,

    /// <summary>Examples: 2, -72, 134728238123</summary>
    Integer,

    /// <summary>Examples: 0.787, -5.8</summary>
    Real,

    /// <summary>Examples: UTF-8 / UTF-16 Strings</summary>
    Text,

    /// <summary>Examples: UTF-8 / UTF-16 Strings</summary>
    Blob
}
