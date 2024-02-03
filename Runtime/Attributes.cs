using System;

namespace Toolkid.SqliteWrapper {
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
