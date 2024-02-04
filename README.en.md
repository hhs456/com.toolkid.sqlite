# SQLite Database Wrapper

A simple C# SQLite database wrapper with basic CRUD operations and transaction support.

## Table of Contents
- [Installation](#installation)
- [Usage](#usage)
  - [Connecting to a Database](#connecting-to-a-database)
  - [Executing Transactions](#executing-transactions)
  - [Inserting Data](#inserting-data)
  - [Updating Data](#updating-data)
  - [Deleting Data](#deleting-data)
  - [Querying Data](#querying-data)
- [Contributing](#contributing)
- [License](#license)

## Installation

Clone the repository or download the source code. Include the provided files in your C# project.

## Usage

### Connecting to a Database

```csharp
string databasePath = "your_database_path.db";
```

### Executing Transactions

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

### Inserting Data

```csharp
Person newPerson = new Person { Name = "John Doe", Age = 30 };

DatabaseUtility.Insert(databasePath, newPerson);
```

### Updating Data

```csharp
newPerson.Age = 31;

DatabaseUtility.Update(databasePath, newPerson.Id, newPerson);
```

### Deleting Data

```csharp
DatabaseUtility.Delete<Person>(databasePath, newPerson.Id);
```

### Querying Data

```csharp
Dictionary<string, object> conditions = new Dictionary<string, object> { { "Age", 30 } };

List<Person> result = DatabaseUtility.Select<Person>(databasePath, conditions);
```

## Contributing

Feel free to contribute to this project by opening issues or submitting pull requests.

## License

This project is licensed under the MIT License.