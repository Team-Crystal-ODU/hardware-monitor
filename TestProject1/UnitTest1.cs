using Microsoft.VisualStudio.TestPlatform.Utilities;

namespace TestProject1
{
    [TestClass]
    public class TestJsonOutput
    {
        [TestMethod]
        public void TestMethod1()
        {
            var data = new object[] {
            new { Id = 1, Name = "John Doe", Age = 30 },
            new { Id = 2, Name = "Jane Doe", Age = 25 }
        };
            var fileName = "test.json";
            // Act
            Output.OutputDataAsJsonFile(data, fileName);

            // Assert
            Assert.IsTrue(File.Exists(fileName), "Output file was not created.");
            var json = File.ReadAllText(fileName);
            var expectedJson = "[{\"Id\":1,\"Name\":\"John Doe\",\"Age\":30},{\"Id\":2,\"Name\":\"Jane Doe\",\"Age\":25}]";
            Assert.AreEqual(expectedJson, json, "Output JSON does not match the expected value.");
        }
    }
  }
