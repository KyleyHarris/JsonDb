using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Hs.JsonDb.Test;

[TestClass]
public class JsonDbContextTests
{
    private JsonDbContextTester _tester = null!;
    private string _testPath = null!;

    [TestInitialize]
    public void Setup()
    {
        _testPath = Path.Combine(Path.GetTempPath(), "JsonDbTests");
        Directory.CreateDirectory(_testPath);
        _tester = new JsonDbContextTester();
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_testPath))
        {
            Directory.Delete(_testPath, true);
        }
    }

    [TestMethod]
    public void InMemory_AddAndRetrieveObjects()
    {
        var testObj = new TestObject { Name = "Test1", City = "TestCity" };
        _tester.TestObjects.Add(testObj);

        Assert.AreEqual(1, _tester.TestObjects.Count);
        Assert.AreEqual("Test1", _tester.TestObjects[0].Name);
    }

    [TestMethod]
    public void InMemory_CommitAndRollback()
    {
        // Add initial data and commit
        _tester.TestObjects.Add(new TestObject { Name = "Original" });
        _tester.Commit();

        // Add more data
        _tester.TestObjects.Add(new TestObject { Name = "New" });
        Assert.AreEqual(2, _tester.TestObjects.Count);

        // Rollback
        _tester.Rollback();
        Assert.AreEqual(1, _tester.TestObjects.Count);
        Assert.AreEqual("Original", _tester.TestObjects[0].Name);
    }

    [TestMethod]
    public void FileSystem_SaveAndLoadData()
    {
        var fileSystemTester = new JsonDbContextTester(_testPath);

        // Add and commit data
        fileSystemTester.TestObjects.Add(new TestObject { Name = "Persisted" });
        fileSystemTester.Commit();

        // Create new context to load data
        var newTester = new JsonDbContextTester(_testPath);
        Assert.AreEqual(1, newTester.TestObjects.Count);
        Assert.AreEqual("Persisted", newTester.TestObjects[0].Name);
    }

    [TestMethod]
    public void FileSystem_RollbackChanges()
    {
        var fileSystemTester = new JsonDbContextTester(_testPath);

        // Initial state
        fileSystemTester.TestObjects.Add(new TestObject { Name = "Initial" });
        fileSystemTester.Commit();

        // Modify data
        fileSystemTester.TestObjects.Add(new TestObject { Name = "Modified" });
        Assert.AreEqual(2, fileSystemTester.TestObjects.Count);

        // Rollback
        fileSystemTester.Rollback();
        Assert.AreEqual(1, fileSystemTester.TestObjects.Count);
        Assert.AreEqual("Initial", fileSystemTester.TestObjects[0].Name);
    }

    [TestMethod]
    public void InMemory_LinqQueryTest()
    {
        // Arrange
        var testObjects = new[]
        {
            new TestObject { Name = "John", City = "New York" },
            new TestObject { Name = "Jane", City = "Boston" },
            new TestObject { Name = "Bob", City = "New York" },
            new TestObject { Name = "Alice", City = "Chicago" },
            new TestObject { Name = "Charlie", City = "Boston" }
        };

        foreach (var obj in testObjects)
        {
            _tester.TestObjects.Add(obj);
        }

        // Act
        var result = _tester.TestObjects
            .Where(x => x.City == "Boston" && x.Name.StartsWith("J"))
            .FirstOrDefault();

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("Jane", result.Name);
        Assert.AreEqual("Boston", result.City);
    }
}
