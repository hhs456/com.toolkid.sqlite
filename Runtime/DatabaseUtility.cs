using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Reflection;
using Mono.Data.Sqlite;
using System.Text;
using UnityEngine.UIElements;
using static UnityEngine.GraphicsBuffer;
using static PlasticPipe.PlasticProtocol.Messages.NegotiationCommand;
using System.IO;
using System.Data;

namespace Toolkid.SqliteWrapper {

    public static class DatabaseUtility {

        public static readonly Dictionary<Type, string> typeMap = new Dictionary<Type, string>() {
            {typeof(int), "INTEGER"},
            {typeof(string), "TEXT"},
            {typeof(float), "REAL"},
            {typeof(bool), "BOOLEAN"},            
            {typeof(DateTime), "DATETIME"}
        };

        public static string GetTableName(this Type type) {
            Attribute attribute = type.GetCustomAttributes(typeof(TableAttribute)).FirstOrDefault();
            return ((TableAttribute)attribute)?.Name ?? type.Name;
        }
        public static string GetColumnName(this PropertyInfo property) {
            Attribute attribute = property.GetCustomAttribute(typeof(ColumnAttribute));
            return ((ColumnAttribute)attribute)?.Name ?? property.Name;
        }

        public static bool IsPrimaryKey(this PropertyInfo property) {
            Attribute attribute = property.GetCustomAttribute(typeof(PrimaryKeyAttribute));
            return attribute != null;
        }

        public static bool TryFindPrimaryKey<T>(this T target, out PropertyInfo primaryKey) {
            primaryKey = target.GetType().FindPrimaryKey();
            return primaryKey != null;
        }
        public static PropertyInfo FindPrimaryKey<T>(this T target) {
            return target.GetType().FindPrimaryKey();
        }

        public static bool TryFindPrimaryKey(this Type target, out PropertyInfo primaryKey) {
            primaryKey = target.FindPrimaryKey();
            return primaryKey != null;
        }

        public static PropertyInfo FindPrimaryKey(this Type target) {
            var properties = target.GetProperties();
            foreach (var p in properties) {
                if (IsPrimaryKey(p)) {
                    return p;
                }
            }
            return null;
        }


        public static void Connect(string databasePath, Action<SqliteCommand> action) {
            string connectionString = $"URI=file:{databasePath}";
            using (SqliteConnection connection = new SqliteConnection(connectionString)) {
                connection.Open();
                using (SqliteCommand command = connection.CreateCommand()) {                    
                    action.Invoke(command);
                    Debug.LogFormat(command.CommandText);
                    command.ExecuteNonQuery();
                }
            }
        }

        public static int Count<T>(string databasePath) where T : new() {           
            return CountWith<T>(databasePath);
        }

        public static int CountWith<T>(string databasePath, Dictionary<string, object> conditions = null) where T : new() {
            string connectionString = $"URI=file:{databasePath}";
            int rowCount = 0;
            var whereBuilder = new StringBuilder();

            if (conditions != null && conditions.Count > 0) {
                whereBuilder.Append("WHERE ");
                foreach (var condition in conditions) {
                    whereBuilder.Append($"{condition.Key} = @{condition.Key} AND ");
                }
                whereBuilder.Length -= 5; // 刪除最後的 " AND "
            }

            Connect(databasePath, (command) => {
                command.CommandText = $"SELECT COUNT(*) FROM {GetTableName(typeof(T))} {whereBuilder}";

                if (conditions != null) {
                    foreach (var condition in conditions) {
                        command.Parameters.Add(new SqliteParameter($"@{condition.Key}", condition.Value));
                    }
                }

                rowCount = Convert.ToInt32(command.ExecuteScalar());
            });

            return rowCount;
        }
        public static void CreateTable<T>(string databasePath) where T : new() {
            if (!File.Exists(databasePath)) {
                using (File.Create(databasePath)) { }
            }

            string connectionString = $"URI=file:{databasePath}";
            var properties = typeof(T).GetProperties();

            var columnsBuilder = new StringBuilder();
            foreach (var p in properties) {
                columnsBuilder.Append($"{GetColumnName(p)} {typeMap[p.PropertyType]}");
                columnsBuilder.Append(IsPrimaryKey(p) ? " PRIMARY KEY," : ",");
            }

            columnsBuilder.Length--; // 移除尾部的逗號
            string columns = columnsBuilder.ToString();

            Connect(databasePath, (command) => {
                command.CommandText = $"CREATE TABLE IF NOT EXISTS {GetTableName(typeof(T))} ({columns})";
            });
        }

        public static void DropTable<T>(string databasePath) where T : new() {
            string connectionString = $"URI=file:{databasePath}";

            Connect(databasePath, (command) => {
                command.CommandText = $"DROP TABLE IF EXISTS {GetTableName(typeof(T))}";
                command.ExecuteNonQuery();
            });
        }

        public static void Update<T>(string databasePath, T target) where T : new() {
            var properties = typeof(T).GetProperties();
            var primaryKey = typeof(T).FindPrimaryKey().GetValue(target);

            // Convert the object properties into a dictionary
            var updates = properties.ToDictionary(p => GetColumnName(p), p => p.GetValue(target));

            Update<T>(databasePath, primaryKey, updates);
        }

        public static void Update<T>(string databasePath, object primaryKey, Dictionary<string, object> updates) where T : new() {
            string connectionString = $"URI=file:{databasePath}";

            var primaryKeyColumn = typeof(T).FindPrimaryKey().GetColumnName();
            var equalsBuilder = new StringBuilder();
            foreach (var update in updates) {
                equalsBuilder.Append($"{update.Key} = @{update.Key},");
            }
            equalsBuilder.Length--;

            Connect(databasePath, (command) => {
                command.CommandText = $"UPDATE {GetTableName(typeof(T))} SET {equalsBuilder} WHERE {primaryKeyColumn} = @{primaryKeyColumn}";
                command.Parameters.Add(new SqliteParameter($"@{primaryKeyColumn}", primaryKey));
                foreach (var update in updates) {
                    command.Parameters.Add(new SqliteParameter($"@{update.Key}", update.Value));
                }
            });
        }

        public static void Insert<T>(string databasePath, T target) where T : new() {
            string connectionString = $"URI=file:{databasePath}";
            var properties = typeof(T).GetProperties();
            var columnsBuilder = new StringBuilder();
            var resultsBuilder = new StringBuilder();            
            foreach (var p in properties) {
                columnsBuilder.Append($"{GetColumnName(p)},");
                resultsBuilder.Append($"@{GetColumnName(p)},");
            }
            columnsBuilder.Length--;
            resultsBuilder.Length--;
            Connect(databasePath, (command) => {
                command.CommandText = $"INSERT INTO {GetTableName(typeof(T))} ({columnsBuilder}) VALUES ({resultsBuilder})";
                foreach (var p in properties) {
                    string columnName = GetColumnName(p);
                    command.Parameters.Add(new SqliteParameter($"@{columnName}", p.GetValue(target)));
                }
            });
        }

        public static void Delete<T>(string databasePath, object primaryKey) where T : new() {
            string primaryKeyColumnName = typeof(T).FindPrimaryKey().GetColumnName();

            Dictionary<string, object> conditions = new Dictionary<string, object>
            {
                { primaryKeyColumnName, primaryKey }
            };

            Delete<T>(databasePath, conditions);
        }

        public static void Delete<T>(string databasePath, Dictionary<string, object> conditions = null) where T : new() {
            string connectionString = $"URI=file:{databasePath}";
            var properties = typeof(T).GetProperties();
            var whereBuilder = new StringBuilder();

            if (conditions != null && conditions.Count > 0) {
                whereBuilder.Append("WHERE ");
                foreach (var condition in conditions) {
                    whereBuilder.Append($"{condition.Key} = @{condition.Key} AND ");
                }
                whereBuilder.Length -= 5; // 刪除最後的 " AND "
            }

            Connect(databasePath, (command) => {
                command.CommandText = $"DELETE FROM {GetTableName(typeof(T))} {whereBuilder}";

                foreach (var condition in conditions) {
                    command.Parameters.Add(new SqliteParameter($"@{condition.Key}", condition.Value));
                }
            });
        }


        public static T Select<T>(string databasePath, object primaryKey) where T : new() {
            string connectionString = $"URI=file:{databasePath}";
            var properties = typeof(T).GetProperties();
            var columnsBuilder = new StringBuilder();
            var keyColumn = typeof(T).FindPrimaryKey().GetColumnName();
            foreach (var p in properties) {
                columnsBuilder.Append($"{GetColumnName(p)},");
            }
            columnsBuilder.Length--;

            T result = default; // 初始化返回值

            Connect(databasePath, (command) => {
                command.CommandText = $"SELECT {columnsBuilder} FROM {GetTableName(typeof(T))} WHERE {keyColumn} = @{keyColumn}";
                command.Parameters.Add(new SqliteParameter($"@{keyColumn}", primaryKey));
                using (var reader = command.ExecuteReader()) {
                    if (reader.Read()) {
                        result = new T();
                        foreach (var p in properties) {
                            var columnName = GetColumnName(p);
                            var value = reader[columnName];
                            p.SetValue(result, value == DBNull.Value ? null : value);
                        }
                    }
                }
            });

            return result;
        }

        public static List<T> Select<T>(string databasePath, Dictionary<string, object> conditions = null) where T : new() {
            string connectionString = $"URI=file:{databasePath}";
            var result = new List<T>();
            var whereBuilder = new StringBuilder();

            if (conditions != null && conditions.Count > 0) {
                whereBuilder.Append("WHERE ");
                whereBuilder.Append(string.Join(" AND ", conditions.Select(condition => $"{condition.Key} = @{condition.Key}")));
            }

            Connect(databasePath, (command) => {
                command.CommandText = $"SELECT * FROM {GetTableName(typeof(T))} {whereBuilder}";

                if (conditions != null) {
                    foreach (var condition in conditions) {
                        command.Parameters.Add(new SqliteParameter($"@{condition.Key}", condition.Value));
                    }
                }

                using (var reader = command.ExecuteReader()) {
                    var properties = typeof(T).GetProperties();

                    while (reader.Read()) {
                        T item = new T();

                        foreach (var p in properties) {
                            var columnName = GetColumnName(p);
                            if (reader.HasColumn(columnName)) {
                                var value = reader[columnName];
                                p.SetValue(item, value == DBNull.Value ? null : value);
                            }
                        }

                        result.Add(item);
                    }
                }
            });

            return result;
        }

        // Helper method to check if the reader contains a specific column
        public static bool HasColumn(this IDataReader reader, string columnName) {
            for (int i = 0; i < reader.FieldCount; i++) {
                if (reader.GetName(i).Equals(columnName, StringComparison.InvariantCultureIgnoreCase)) {
                    return true;
                }
            }
            return false;
        }

        public static List<T> SelectAll<T>(string databasePath) where T : new() {            
            return Select<T>(databasePath);
        }
    }
}