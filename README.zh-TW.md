# SQLite 資料庫封裝器

一個簡單的 C# SQLite 資料庫封裝器，提供基本的 CRUD 操作和事務支援。

## 目錄
- [安裝](#安裝)
- [使用](#使用)
  - [連接到資料庫](#連接到資料庫)
  - [執行事務](#執行事務)
  - [插入資料](#插入資料)
  - [更新資料](#更新資料)
  - [刪除資料](#刪除資料)
  - [查詢資料](#查詢資料)
- [貢獻](#貢獻)
- [授權](#授權)

## 安裝

複製存儲庫或下載源代碼。將提供的文件包含到你的 C# 專案中。

## 使用

### 連接到資料庫

```csharp
string databasePath = "你的資料庫路徑.db";
```

### 執行事務

```csharp
try
{
    DatabaseUtility.ExecuteTransaction(databasePath, (transaction) =>
    {
        // Your database operations within the transaction
    });

    Console.WriteLine("Transaction completed successfully.");
}
catch (Exception ex)
{
    Console.WriteLine($"Transaction failed: {ex.Message}");
}
```

### 插入資料

```csharp
Person newPerson = new Person { Name = "John Doe", Age = 30 };

DatabaseUtility.Insert(databasePath, newPerson);
```

### 更新資料

```csharp
newPerson.Age = 31;

DatabaseUtility.Update(databasePath, newPerson.Id, newPerson);
```

### 刪除資料

```csharp
DatabaseUtility.Delete<Person>(databasePath, newPerson.Id);
```

### 查詢資料

```csharp
Dictionary<string, object> conditions = new Dictionary<string, object> { { "Age", 30 } };

List<Person> result = DatabaseUtility.Select<Person>(databasePath, conditions);
```

## 貢獻

歡迎通過提出問題或提交拉取請求來貢獻到這個專案。

## 授權

本專案根據 MIT 授權條款許可使用。