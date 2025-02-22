using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Hs.JsonDb;


public class TestObject
{
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string ZipCode { get; set; } = string.Empty;
}
public class JsonDbContextTester : JsonDbContext
{
    public IList<TestObject> TestObjects { get; init; } = null!;
    public JsonDbContextTester() : base() { }
    public JsonDbContextTester(string path) : base(path) { }
}
