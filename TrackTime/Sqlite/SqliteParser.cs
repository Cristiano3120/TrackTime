using Microsoft.Data.Sqlite;
using System.Reflection;
using System.Text.Json;
using TrackTime.Sqlite.Attributes;

namespace TrackTime.Sqlite;

internal static class SqliteParser
{
    public static SqliteCommand ParseToSqlTable<T>(SqliteConnection sqliteConnection)
    {
        List<string> columns = [];
        Type type = typeof(T);

        foreach (PropertyInfo property in type.GetProperties())
        {
            if (property.GetCustomAttribute<IgnoreAttribute>() is not null)
            {
                continue;
            }

            SqlType sqlType = property.GetCustomAttribute<SqlTypeAttribute>()?.SqlType
                         ?? MapClrToSqliteType(property.PropertyType);

            string columnName = property.Name;
            string columnSql = $"{columnName} {sqlType}";

            if (Attribute.IsDefined(property, typeof(PrimaryKeyAttribute)))
            {
                columnSql += " PRIMARY KEY";
            }

            columns.Add(columnSql);
        }

        string tableName = type.Name;
        return new SqliteCommand($"CREATE TABLE IF NOT EXISTS {tableName} ({string.Join(", ", columns)});", sqliteConnection);
    }

    public static SqliteCommand ParseToSqlRow<T>(T input, SqlOperation operation, SqliteConnection sqliteConnection)
    {
        Type type = typeof(T);
        string tableName = type.Name;

        List<string> setClauses = [];
        List<string> columnNames = [];
        List<string> parameterNames = [];

        PropertyInfo? primaryKeyProp = type.GetProperties()
            .FirstOrDefault(p => Attribute.IsDefined(p, typeof(PrimaryKeyAttribute)));

        if (operation is SqlOperation.Update or SqlOperation.Delete && primaryKeyProp is null)
            throw new InvalidOperationException($"Update/Delete requires a primary key on {type.Name}");

        foreach (PropertyInfo property in type.GetProperties())
        {
            if (property.GetCustomAttribute<IgnoreAttribute>() is not null)
                continue;

            string columnName = property.Name;

            if (operation == SqlOperation.Insert)
            {
                columnNames.Add(columnName);
                parameterNames.Add($"@{columnName}");
            }
            else if (operation == SqlOperation.Update && property != primaryKeyProp)
            {
                setClauses.Add($"{columnName} = @{columnName}");
            }
        }

        string sql = operation switch
        {
            SqlOperation.Insert =>
                $"INSERT INTO {tableName} ({string.Join(", ", columnNames)}) VALUES ({string.Join(", ", parameterNames)});",

            SqlOperation.Update =>
                $"UPDATE {tableName} SET {string.Join(", ", setClauses)} WHERE {primaryKeyProp!.Name} = @PrimaryKey;",

            SqlOperation.Delete =>
                $"DELETE FROM {tableName} WHERE {primaryKeyProp!.Name} = @PrimaryKey;",

            _ => throw new NotImplementedException()
        };

        SqliteCommand command = new(sql, sqliteConnection);

        foreach (PropertyInfo property in type.GetProperties())
        {
            if (property.GetCustomAttribute<IgnoreAttribute>() is not null)
                continue;

            SqlType sqlType = property.GetCustomAttribute<SqlTypeAttribute>()?.SqlType
                ?? MapClrToSqliteType(property.PropertyType);

            object? value = sqlType == SqlType.Text && property.PropertyType != typeof(string)
                ? JsonSerializer.Serialize(property.GetValue(input))
                : property.GetValue(input);

            if (operation == SqlOperation.Insert || (operation == SqlOperation.Update && property != primaryKeyProp))
                _ = command.Parameters.AddWithValue($"@{property.Name}", value ?? DBNull.Value);
        }

        if (operation != SqlOperation.Insert && primaryKeyProp is not null)
            _ = command.Parameters.AddWithValue("@PrimaryKey", primaryKeyProp.GetValue(input) ?? DBNull.Value);

        return command;
    }


    public static T ParseToCSharp<T>(SqliteDataReader reader) where T : new()
    {
        T obj = new();
        Type type = typeof(T);

        foreach (PropertyInfo property in type.GetProperties())
        {
            if (property.GetCustomAttribute<IgnoreAttribute>() is not null)
                continue;

            SqlType sqlType = property.GetCustomAttribute<SqlTypeAttribute>()?.SqlType
                ?? MapClrToSqliteType(property.PropertyType);

            object? value = null;

            int ordinal = reader.GetOrdinal(property.Name);
            if (reader.IsDBNull(ordinal))
                continue;

            switch (sqlType)
            {
                case SqlType.Integer:
                    Type intPropType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;

                    if (intPropType == typeof(int))
                        value = reader.GetInt32(ordinal);
                    else if (intPropType == typeof(long))
                        value = reader.GetInt64(ordinal);
                    else if (intPropType == typeof(bool))
                        value = reader.GetInt32(ordinal) != 0;
                    else if (intPropType.IsEnum)
                        value = Enum.ToObject(intPropType, reader.GetInt32(ordinal));
                    else
                        value = Convert.ChangeType(reader.GetValue(ordinal), intPropType);
                    break;

                case SqlType.Text:
                    string text = reader.GetString(ordinal);
                    Type strPropType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;

                    value = strPropType == typeof(string) 
                        ? text 
                        : JsonSerializer.Deserialize(text, strPropType);

                    break;

                case SqlType.Real:
                    intPropType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;

                    if (intPropType == typeof(double))
                        value = reader.GetDouble(ordinal);
                    else if (intPropType == typeof(float))
                        value = Convert.ToSingle(reader.GetDouble(ordinal));
                    else
                        value = Convert.ChangeType(reader.GetValue(ordinal), intPropType);
                    break;

                case SqlType.Blob:
                    value = (byte[])reader.GetValue(ordinal);
                    break;

                case SqlType.Null:
                    value = null;
                    break;
            }

            if (value != null)
            {
                object? converted = SafeConvert(value, property.PropertyType);
                if (converted != null)
                    property.SetValue(obj, converted);
            }
        }

        return obj;
    }

    private static object? SafeConvert(object value, Type targetType)
    {
        targetType = Nullable.GetUnderlyingType(targetType) ?? targetType;

        try
        {
            if (targetType.IsEnum)
            {
                if (value is string s)
                    return Enum.Parse(targetType, s, ignoreCase: true);
                return Enum.ToObject(targetType, Convert.ToInt32(value));
            }

            if (value is string str && string.IsNullOrWhiteSpace(str))
                return null;

            return Convert.ChangeType(value, targetType);
        }
        catch
        {
            return null;
        }
    }


    private static SqlType MapClrToSqliteType(Type type)
    {
        if (type == typeof(int) || type == typeof(long) || type == typeof(short) || type == typeof(byte) || type == typeof(bool))
        {
            return SqlType.Integer;
        }
        else if (type == typeof(float) || type == typeof(double) || type == typeof(decimal))
        {
            return SqlType.Real;
        }
        else if (type == typeof(byte[]))
        {
            return SqlType.Blob;
        }

        return SqlType.Text;
    }
}
