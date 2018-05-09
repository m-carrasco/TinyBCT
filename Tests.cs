using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class Tests
{
    [TestMethod]
    void SimpleTest()
    {
        var source = @"
            ";
        TestUtils.CreateAssemblyDefinition(source, "SimpleTest");

    }
}
