using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Toolkid.SqliteToolkits;
using Mono.Data.Sqlite;
using Unity.VisualScripting.Dependencies.Sqlite;
using UnityEngine;
using System.Data;
using System.Reflection;
using System.Data.Common;

namespace Toolkid.SqliteToolkits {

    public static class DatabaseUtility {

        static readonly Dictionary<Type, string> typeMap = new Dictionary<Type, string>() {
            {typeof(int), "INTEGER"},
            {typeof(string), "TEXT"},
            {typeof(float), "REAL"},
            {typeof(bool), "BOOLEAN"},
            {typeof(byte), "BLOB"},
            {typeof(DateTime), "DATETIME"}
        };

        private static string GetTableName(Type type) {
            Attribute attribute = type.GetCustomAttributes(typeof(TableAttribute)).FirstOrDefault();
            return ((TableAttribute)attribute)?.Name ?? type.Name;
        }
        private static string GetColumnName(PropertyInfo property) {
            Attribute attribute = property.GetCustomAttribute(typeof(ColumnAttribute));
            return ((TableAttribute)attribute)?.Name ?? property.Name;
        }

        private static bool IsPrimaryKey(PropertyInfo property) {
            Attribute attribute = property.GetCustomAttribute(typeof(PrimaryKeyAttribute)) ?? null;
            return attribute != null;
        }


        public static void Create<T>(string databasePath) where T : new() {
            string connectionString = $"URI=file:{databasePath}";

            using (IDbConnection dbConnection = new SqliteConnection(connectionString)) {
                dbConnection.Open();

                var properties = typeof(T).GetProperties();
                var columns = string.Empty;
                foreach (var p in properties) {
                    columns += GetColumnName(p) + " " + typeMap[p.PropertyType];
                    columns += IsPrimaryKey(p) ? " PRIMARY KEY," : ",";
                }
                columns = columns.Remove(columns.Length - 1);

                using (IDbCommand cmd = dbConnection.CreateCommand()) {
                    string cmdText = $"CREATE TABLE IF NOT EXISTS {GetTableName(typeof(T))} ({columns})";
                    Debug.LogFormat(cmdText);
                    cmd.CommandText = cmdText;
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private static string FindPrimaryKey(Type type) {
            var properties = type.GetProperties();
            foreach (var p in properties) {
                if (IsPrimaryKey(p)) {
                    return p.Name;
                }
            }
            return null;
        }

        public static void Update<T>(string databasePath, T target) where T : new() {
            string connectionString = $"URI=file:{databasePath}";

            var properties = typeof(T).GetProperties();
            var columns = string.Empty;            
            var primaryKey = string.Empty;
            foreach (var p in properties) {
                string colName = GetColumnName(p);
                if (IsPrimaryKey(p)) {
                    primaryKey = colName;                    
                }
                columns += colName + " = @" + colName + ",";                
            }
            columns = columns.Remove(columns.Length - 1);
            

            using (IDbConnection dbConnection = new SqliteConnection(connectionString)) {
                dbConnection.Open();

                using (IDbCommand cmd = dbConnection.CreateCommand()) {
                    string cmdText = $"UPDATE {GetTableName(typeof(T))} SET {columns} WHERE {primaryKey} = @{primaryKey}";                    
                    Debug.LogFormat(cmdText);

                    foreach (var p in properties) {
                        IDbDataParameter paramName = cmd.CreateParameter();
                        paramName.ParameterName = "@" + GetColumnName(p);
                        paramName.Value = p.GetValue(target);
                        cmd.Parameters.Add(paramName);
                    }

                    cmd.ExecuteNonQuery();
                }
            }
        }

        public static void Insert<T>(string databasePath, T target) where T : new() {
            string connectionString = $"URI=file:{databasePath}";

            var properties = typeof(T).GetProperties();
            var columns = string.Empty;
            var values = string.Empty;
            foreach (var p in properties) {
                columns += GetColumnName(p) + ",";
                values += "@" + GetColumnName(p) + ",";
            }
            columns = columns.Remove(columns.Length - 1);
            values = values.Remove(values.Length - 1);

            using (IDbConnection dbConnection = new SqliteConnection(connectionString)) {
                dbConnection.Open();

                using (IDbCommand cmd = dbConnection.CreateCommand()) {
                    string cmdText = $"INSERT INTO {GetTableName(typeof(T))} ({columns}) VALUES ({values})";
                    Debug.LogFormat(cmdText);                    

                    foreach (var p in properties) {
                        IDbDataParameter paramName = cmd.CreateParameter();
                        paramName.ParameterName = "@" + GetColumnName(p);
                        paramName.Value = p.GetValue(target);
                        cmd.Parameters.Add(paramName);
                    }

                    cmd.ExecuteNonQuery();
                }
            }
        }

        public static void Insert<T>(string databasePath, T[] array) where T : new() {
            using (var db = new SQLiteConnection(databasePath, SQLiteOpenFlags.ReadWrite)) {
                var count = db.InsertAll(array, typeof(T));
            }
        }

        public static void Delete<T>(string databasePath, string targetProperty, string targetValue) where T : new() {
            var props = typeof(T).GetProperties();
            string select = string.Empty;
            foreach (var prop in props) {
                select += prop.Name + ",";
            }
            select = select.Remove(select.Length - 1);
            using (var db = new SQLiteConnection(databasePath, SQLiteOpenFlags.ReadWrite)) {
                string sql = $"SELECT {select} FROM {GetTableName(typeof(T))} WHERE {targetProperty} = {targetValue}";
                var data = select.Split(',');
                var deleteData = db.Query<T>(sql, data).FirstOrDefault();
                if (deleteData != null) {
                    var count = db.Delete(deleteData);
                }                
            }
        }

        public static void Delete<T>(string databasePath, int index) where T : new() {
            using (var db = new SQLiteConnection(databasePath, SQLiteOpenFlags.ReadWrite)) {
                var count = db.Delete<T>(index);
            }
        }


        public static List<T> Query<T>(string databasePath) where T : new() {
            List<T> datas = new List<T>();
            var props = typeof(T).GetProperties();
            string select = string.Empty;
            foreach (var prop in props) {
                select += prop.Name + ",";
            }
            select = select.Remove(select.Length - 1);
            using (var db = new SQLiteConnection(databasePath, SQLiteOpenFlags.ReadWrite)) {
                string sql = $"SELECT {select} FROM {GetTableName(typeof(T))}";
                var data = select.Split(',');
                datas.AddRange(db.Query<T>(sql, data));
            }
            return datas;
        }

        public static T Query<T>(string databasePath, int index) where T : new() {
            T data;
            using (var db = new SQLiteConnection(databasePath, SQLiteOpenFlags.ReadWrite)) {                
                data = db.Get<T>(index);
            }
            return data;
        }
    }
}