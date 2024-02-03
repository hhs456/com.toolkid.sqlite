using System.Collections;
using System.Collections.Generic;
using Toolkid.SqliteWrapper;
using UnityEditor;
using UnityEditor.TerrainTools;
using UnityEngine;
using System;
using System.IO;
using Unity.VisualScripting;

[CustomEditor(typeof(DataKeeper))]
public class DataKeeperEditor : Editor
{
    protected DataKeeper script;
    protected SerializedProperty databasePath;
    protected SerializedProperty databaseName;
    protected SerializedProperty dataSamples;
    private void OnEnable() {
        databasePath = serializedObject.FindProperty("databasePath");
        databaseName = serializedObject.FindProperty("databaseName");
        dataSamples = serializedObject.FindProperty("dataSamples");
        script = (DataKeeper)target;
    }
    public override void OnInspectorGUI() {

        serializedObject.Update();
        
        EditorGUILayout.PropertyField(databasePath);
        EditorGUILayout.PropertyField(databaseName);
        EditorGUILayout.PropertyField(dataSamples);
        string filePath = databasePath.stringValue + "/" + databaseName.stringValue + ".db";
        if (GUILayout.Button("Create Database")) {
            databasePath.stringValue = EditorUtility.OpenFolderPanel("�п�ܸ�Ʈw�x�s��m", Application.dataPath, "");
            filePath = databasePath.stringValue + "/" + databaseName.stringValue + ".db";            
            DatabaseUtility.CreateTable<DataSample>(filePath);
        }
        if (GUILayout.Button("Insert Database")) {
            DatabaseUtility.Insert(filePath, new DataSample() {
                Id = 0,
                Name = "TEST",
                Rate = 0.2f,
                Enable = false,
                DateTime = DateTime.Now,
            });            
            if (!File.Exists(filePath)) {
                File.Create(filePath).Close();
            }
            DatabaseUtility.CreateTable<DataSample>(filePath);
        }
        if (GUILayout.Button("Query Database")) {
            //script.dataSamples = DatabaseUtility.Query<DataSample>(filePath).ToArray();
            serializedObject.Update();
        }

        serializedObject.ApplyModifiedProperties();
    }

    

    
}
