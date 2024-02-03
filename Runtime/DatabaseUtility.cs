using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Data;
using System.Reflection;
using Mono.Data.Sqlite;
using System.Text;
using UnityEngine.UIElements;
using static UnityEngine.GraphicsBuffer;

namespace Toolkid.SqliteWrapper {

    public struct CommandParameter<T> {
        public string[] columns;
        public object[] values;
        public readonly string paramText;
        public readonly string equalText;
        public readonly string valueText;

        public CommandParameter(T target) {
            PropertyInfo[] properties = target.GetType().GetProperties();
            columns = new string[properties.Length];
            values = new string[properties.Length];
            for (int i = 0; i < properties.Length; i++) {
                columns[i] = properties[i].GetColumnName();
                values[i] = properties.GetValue(i);
            }
            this = new CommandParameter<T>(columns, values);
        }
        public CommandParameter(string[] columns, object[] values) {      
            PropertyInfo[] properties = typeof(T).GetProperties();
            this.columns = columns;
            this.values = values;
            paramText = string.Empty;
            equalText = string.Empty;
            valueText = string.Empty;
            for (int i = 0; i < columns.Length; i++) {
                paramText += $"{columns[i]},";
                equalText += $"{columns[i]} = @{columns[i]} AND";
                valueText += $"@{columns[i]},";
            }
            paramText = paramText.Remove(paramText.Length - 1);
            equalText = equalText.Remove(equalText.Length - 5, 5);
            valueText = valueText.Remove(valueText.Length - 1);
        }
    }

    public static class DatabaseUtility {

        public static readonly Dictionary<Type, string> typeMap = new Dictionary<Type, string>() {
            {typeof(int), "INTEGER"},
            {typeof(string), "TEXT"},
            {typeof(float), "REAL"},
            {typeof(bool), "BOOLEAN"},
            {typeof(byte), "BLOB"},
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

        public static void Create<T>(string databasePath) where T : new() {
            string connectionString = $"URI=file:{databasePath}";
            var properties = typeof(T).GetProperties();            

            var columnsBuilder = new StringBuilder();
            foreach (var p in properties) {
                columnsBuilder.Append($"{GetColumnName(p)} {typeMap[p.PropertyType]}");
                columnsBuilder.Append(IsPrimaryKey(p) ? " PRIMARY KEY," : ",");
            }

            columnsBuilder.Length--; // Remove the trailing comma
            string columns = columnsBuilder.ToString();

            Connect(databasePath, (command) => {
                command.CommandText = $"CREATE TABLE IF NOT EXISTS {GetTableName(typeof(T))} ({columns})";                
            });
        }


        public static void Update<T>(string databasePath, T target) where T : new() {
            string connectionString = $"URI=file:{databasePath}";

            var properties = typeof(T).GetProperties();            
            var primaryKey = typeof(T).FindPrimaryKey().GetColumnName();
            var equalsBuilder = new StringBuilder();
            foreach (var p in properties) {
                string columName = GetColumnName(p);
                equalsBuilder.Append($"{columName} = @{columName},");
            }
            equalsBuilder.Length--;

            Connect(databasePath, (command) => {
                command.CommandText = $"UPDATE {GetTableName(typeof(T))} SET {equalsBuilder} WHERE {primaryKey} = @{primaryKey}";
                foreach (var p in properties) {
                    string columnName = GetColumnName(p);
                    command.Parameters.Add(new SqliteParameter($"@{columnName}", p.GetValue(target)));
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
            string connectionString = $"URI=file:{databasePath}";
            var properties = typeof(T).GetProperties();
            var columnsBuilder = new StringBuilder();
            var keyColumn = typeof(T).FindPrimaryKey().GetColumnName();
            foreach (var p in properties) {
                columnsBuilder.Append($"{GetColumnName(p)},");
            }
            columnsBuilder.Length--;
            Connect(databasePath, (command) => {
                command.CommandText = $"DELETE FROM {GetTableName(typeof(T))} WHERE {keyColumn} = @{keyColumn}";
                command.Parameters.Add(new SqliteParameter($"@{keyColumn}", primaryKey));
                command.ExecuteNonQuery();
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
    }
}