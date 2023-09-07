using System;
using Mono.Data.Sqlite;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Toolkid.SqliteToolkits {
    [AttributeUsage(AttributeTargets.Class)]
    public class TableAttribute : Attribute {
        public string Name { get; }
        public TableAttribute(string name) {
            Name = name;
        }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class ColumnAttribute : Attribute {
        public string Name { get; }
        public ColumnAttribute(string name) {
            Name = name;
        }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class PrimaryKeyAttribute : Attribute {     
        public PrimaryKeyAttribute() {
            
        }
    }
}
