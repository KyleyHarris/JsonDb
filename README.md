# JsonDb

A lightweight C# JSON-based persistent database with LINQ support, perfect for mocking and small file-based storage requirements.

## Installation

```shell
dotnet add package Hs.JsonDb
```

## Features

- Simple JSON file-based persistence
- LINQ query support
- Lightweight and fast
- Perfect for mocking and testing
- No external database required

## Usage

```csharp
using Hs.JsonDb;

// Create a new database
var db = new JsonDbContext("mydata.json");

// Add data
db.Store(new Person { Id = 1, Name = "John" });

// Query with LINQ
var results = db.Query<Person>()
    .Where(p => p.Name.StartsWith("J"))
    .ToList();
```

## License

MIT License
