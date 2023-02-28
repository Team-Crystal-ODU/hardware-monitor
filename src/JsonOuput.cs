using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.IO;

public class Output
{
    public static void OutputDataAsJsonFile(object[] data, string fileName)
    {
        string json=JsonSerializer.Serialize(data);
        File.WriteAllText(fileName, json);
        Console.WriteLine("Data written to file: " + fileName);
    }
}