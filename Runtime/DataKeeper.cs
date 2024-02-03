using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace Toolkid.SqliteWrapper {

    public class DataKeeper : MonoBehaviour {
        public string databasePath = string.Empty;
        public string databaseName = "my_database";
        public DataSample[] dataSamples;
        private void OnValidate() {
            if (databasePath == string.Empty) {
                databasePath = Application.dataPath;
            }
        }
    }

    [Table(name: nameof(DataSample)), Serializable]
    public class DataSample {

        [PrimaryKey/*, AutoIncrement*/]
        public int Id {
            get; set;
        }
        
        public string Name {
            get; set;
        }

        public float Rate {
            get; set;
        }

        public bool Enable {
            get; set;
        }

        public DateTime DateTime {
            get; set;
        }
    }
}
