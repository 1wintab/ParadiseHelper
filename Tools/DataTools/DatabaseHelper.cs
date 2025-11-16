using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;
using System.IO;
using System.Windows.Forms;
using Core;

/// <summary>
/// Static class to manage account data (login/password) stored persistently in a SQLite database file.
/// </summary>
public static class DatabaseHelper
{
    // Path to the SQLite database file: [AppDir]\Data\Database\accounts.db.
    private static readonly string dbPath = Path.Combine(FilePaths.Standard.DataBaseDirectory, "accounts.db");

    // Connection string used to open the database.
    private static readonly string connString = $"Data Source={dbPath};";

    /// <summary>
    /// Ensures the database directory, file, and the 'accounts' table exist. Creates them if missing.
    /// </summary>
    public static void InitDatabase()
    {
        // Ensure the directory for the database file exists.
        Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);

        using (SqliteConnection conn = new SqliteConnection(connString))
        {
            conn.Open();

            // SQL query to create the table if it doesn't already exist.
            string query = "CREATE TABLE IF NOT EXISTS accounts (id INTEGER PRIMARY KEY, login TEXT, password TEXT)";

            using (SqliteCommand cmd = new SqliteCommand(query, conn))
            {
                cmd.ExecuteNonQuery();
            }
        }
    }

    /// <summary>
    /// Inserts a new login/password pair into the database.
    /// </summary>
    /// <param name="login">The unique login name to insert.</param>
    /// <param name="password">The password associated with the login.</param>
    /// <returns>True if insertion was successful, False if the login already exists and insertion was skipped.</returns>
    public static bool InsertAccount(string login, string password)
    {
        using (SqliteConnection conn = new SqliteConnection(connString))
        {
            conn.Open();

            // First, check if the login already exists to prevent duplicates.
            string checkQuery = "SELECT COUNT(*) FROM accounts WHERE login = @login";

            using (SqliteCommand checkCmd = new SqliteCommand(checkQuery, conn))
            {
                checkCmd.Parameters.AddWithValue("@login", login);
                long count = (long)checkCmd.ExecuteScalar();

                if (count > 0)
                {
                    return false; // Login already exists.
                }
            }

            // If the login is unique, proceed with insertion.
            string insertQuery = "INSERT INTO accounts (login, password) VALUES (@login, @password)";

            using (SqliteCommand insertCmd = new SqliteCommand(insertQuery, conn))
            {
                insertCmd.Parameters.AddWithValue("@login", login);
                insertCmd.Parameters.AddWithValue("@password", password);
                insertCmd.ExecuteNonQuery();
                return true;
            }
        }
    }

    /// <summary>
    /// Retrieves a list of all account logins stored in the database.
    /// </summary>
    /// <returns>A <see cref="List{T}"/> of all unique login strings.</returns>
    public static List<string> GetLogins()
    {
        List<string> logins = new List<string>();

        try
        {
            using (SqliteConnection conn = new SqliteConnection(connString))
            {
                conn.Open();

                string query = "SELECT login FROM accounts";

                using (SqliteCommand cmd = new SqliteCommand(query, conn))
                {
                    using (SqliteDataReader reader = cmd.ExecuteReader())
                    {
                        // Read each login from the result set and add it to the list.
                        while (reader.Read())
                        {
                            logins.Add(reader.GetString(0));
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            // Display an error message if reading from the database fails.
            MessageBox.Show("Error retrieving logins: " + ex.Message);
        }

        return logins;
    }

    /// <summary>
    /// Retrieves the password for a given login name.
    /// </summary>
    /// <param name="login">The login name to search for.</param>
    /// <returns>The associated password string, or <see cref="string.Empty"/> if the login is not found.</returns>
    public static string GetPasswordForLogin(string login)
    {
        using (SqliteConnection conn = new SqliteConnection(connString))
        {
            conn.Open();

            string query = "SELECT password FROM accounts WHERE login = @login";

            using (SqliteCommand cmd = new SqliteCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@login", login);

                // ExecuteScalar returns the value of the first column in the first row.
                object result = cmd.ExecuteScalar();

                // Return the password or an empty string if the login was not found (result is null).
                return result != null ? result.ToString() : string.Empty;
            }
        }
    }

    /// <summary>
    /// Deletes an account entry based on the login name.
    /// </summary>
    /// <param name="login">The login name of the account to delete.</param>
    public static void DeleteAccount(string login)
    {
        using (var conn = new SqliteConnection(connString))
        {
            conn.Open();

            string query = "DELETE FROM accounts WHERE login = @login";

            using (SqliteCommand cmd = new SqliteCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@login", login);
                cmd.ExecuteNonQuery();
            }
        }
    }

    /// <summary>
    /// Updates the login and/or password for an existing account identified by the old login name.
    /// </summary>
    /// <param name="oldLogin">The current login name used to identify the record.</param>
    /// <param name="newLogin">The new login name to assign to the record.</param>
    /// <param name="newPassword">The new password to assign to the record.</param>
    public static void UpdateAccount(string oldLogin, string newLogin, string newPassword)
    {
        using (SqliteConnection conn = new SqliteConnection(connString))
        {
            conn.Open();

            string query = "UPDATE accounts SET login = @newLogin, password = @newPassword WHERE login = @oldLogin";

            using (SqliteCommand cmd = new SqliteCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@newLogin", newLogin);
                cmd.Parameters.AddWithValue("@newPassword", newPassword);
                cmd.Parameters.AddWithValue("@oldLogin", oldLogin);

                cmd.ExecuteNonQuery();
            }
        }
    }

    /// <summary>
    /// Checks if a specific login already exists in the database, optionally excluding a login name
    /// (useful for checking uniqueness during an update operation).
    /// </summary>
    /// <param name="login">The login name to check for existence.</param>
    /// <param name="excludeLogin">An optional login name to exclude from the count (e.g., the current user's login during an edit).</param>
    /// <returns>True if the login exists (and is not the excluded login, if provided); otherwise, false.</returns>
    public static bool LoginExists(string login, string excludeLogin = null)
    {
        using (SqliteConnection conn = new SqliteConnection(connString))
        {
            conn.Open();

            string query = "SELECT COUNT(*) FROM accounts WHERE login = @login";

            // If an excludeLogin is provided, add a WHERE clause to ignore that record.
            if (!string.IsNullOrEmpty(excludeLogin))
            {
                query += " AND login != @excludeLogin";
            }

            using (SqliteCommand cmd = new SqliteCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@login", login);

                if (!string.IsNullOrEmpty(excludeLogin))
                {
                    cmd.Parameters.AddWithValue("@excludeLogin", excludeLogin);
                }

                long count = (long)cmd.ExecuteScalar();
                return count > 0;
            }
        }
    }

    /// <summary>
    /// Deletes all entries from the 'accounts' table.
    /// </summary>
    public static void DeleteAllAccounts()
    {
        using (SqliteConnection conn = new SqliteConnection(connString))
        {
            conn.Open();

            using (SqliteCommand cmd = new SqliteCommand("DELETE FROM accounts;", conn))
            {
                cmd.ExecuteNonQuery();
            }
        }
    }
}