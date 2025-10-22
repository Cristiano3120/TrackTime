using Microsoft.Data.Sqlite;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace TrackTime.Sqlite;

internal class SqliteDatabase
{
    private const string DatabaseFolderName = "SqliteDatabase";
    private readonly string _dataSource;

    public SqliteDatabase(string? dataSource)
    {
        string folderPath = Path.Combine(Application.ApplicationFolderPath, DatabaseFolderName);
        _ = Directory.CreateDirectory(folderPath);
        

        _dataSource = dataSource ?? Path.Combine(folderPath, "database.db");

        //Create Database if not already there
        string connectionStr = new SqliteConnectionStringBuilder()
        {
            DataSource = _dataSource,
            Mode = SqliteOpenMode.ReadWriteCreate,
        }.ToString();

        using SqliteConnection connection = new(connectionStr);
        connection.Open();
    }

    /// <summary>
    /// The returned connection has to be opened first!
    /// </summary>
    /// <returns></returns>
    private SqliteConnection CreateSqliteConnection(SqliteOpenMode openMode)
    {
        string connectionStr = new SqliteConnectionStringBuilder()
        {
            DataSource = _dataSource,
            Mode = openMode,
        }.ToString();

        return new SqliteConnection(connectionStr);
    }

    public void CreateTable<T>()
    {
        using SqliteConnection sqliteConnection = CreateSqliteConnection(openMode: SqliteOpenMode.ReadWriteCreate);
        sqliteConnection.Open();

        string tableName = typeof(T).Name;

        using (SqliteCommand checkCommand = sqliteConnection.CreateCommand())
        {
            checkCommand.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name=@tableName;";
            _ = checkCommand.Parameters.AddWithValue("@tableName", tableName);

            object? result = checkCommand.ExecuteScalar();
            if (result != null) //Table exists
            {
                return;
            }
        }

        using (SqliteCommand command = SqliteParser.ParseToSqlTable<T>(sqliteConnection))
        {
            _ = command.ExecuteNonQuery();
        }
    }

    public void DeleteTable(string tableName)
    {
        using SqliteConnection sqliteConnection = CreateSqliteConnection(openMode: SqliteOpenMode.ReadWriteCreate);
        sqliteConnection.Open();

        using (SqliteCommand sqliteCommand = new($"DROP TABLE {tableName}", sqliteConnection))
        {
            _ = sqliteCommand.ExecuteNonQuery();
        }  
    }

    public void Insert<T>(T data)
    {
        using SqliteConnection sqliteConnection = CreateSqliteConnection(openMode: SqliteOpenMode.ReadWrite);
        sqliteConnection.Open();

        using (SqliteCommand sqliteCommand = SqliteParser.ParseToSqlRow(data, SqlOperation.Insert, sqliteConnection))
        {
            _ = sqliteCommand.ExecuteNonQuery(); 
        }
    }

    public void Update<T>(T data)
    {
        using SqliteConnection sqliteConnection = CreateSqliteConnection(openMode: SqliteOpenMode.ReadWrite);
        sqliteConnection.Open();

        using (SqliteCommand sqliteCommand = SqliteParser.ParseToSqlRow(data, SqlOperation.Update, sqliteConnection))
        {
            _ = sqliteCommand.ExecuteNonQuery(); 
        }
    }

    public bool Remove<T>(T data)
    {
        using SqliteConnection sqliteConnection = CreateSqliteConnection(openMode: SqliteOpenMode.ReadWrite);
        sqliteConnection.Open();

        using (SqliteCommand sqliteCommand = SqliteParser.ParseToSqlRow(data, SqlOperation.Delete, sqliteConnection))
        {
            return sqliteCommand.ExecuteNonQuery() > 0;
        }
    }

    public IEnumerable<T> GetAll<T>() where T : new()
    {
        using SqliteConnection sqliteConnection = CreateSqliteConnection(openMode: SqliteOpenMode.ReadWrite);
        sqliteConnection.Open();
        
        using (SqliteCommand sqliteCommand = new($"SELECT * FROM {typeof(T).Name}", sqliteConnection))
        {
            SqliteDataReader reader = sqliteCommand.ExecuteReader();

            while (reader.Read())
            {
                yield return SqliteParser.ParseToCSharp<T>(reader);
            } 
        }
    }
}
