using Microsoft.Data.Sqlite;

namespace TrackTime;

internal static class Database
{
    private const string DatabaseFolderName = "SqliteDatabase";

    static Database()
    {
        //Create Database if not already there
        string connectionStr = new SqliteConnectionStringBuilder()
        {
            DataSource = Path.Combine(Application.ApplicationFolderPath, DatabaseFolderName),
        }.ToString();

        using SqliteConnection connection = new SqliteConnection(connectionStr);
        connection.Open();

        connection.
    }

    internal static void AddItem()
    {
        string connectionStr = new SqliteConnectionStringBuilder()
        {
            DataSource = Path.Combine(Application.ApplicationFolderPath, DatabaseFolderName),
            Mode = SqliteOpenMode.ReadWrite,
        }.ToString();

        using SqliteConnection connection = new SqliteConnection(connectionStr);
    }

    private static void CreateTable<T>(SqliteConnection conn, T type)
    {
        
    }
}
