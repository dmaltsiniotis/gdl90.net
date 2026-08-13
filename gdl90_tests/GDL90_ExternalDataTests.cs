using GDL90;
using System;
using System.IO;
using Xunit;

namespace GDL90.Tests;

public class ExternalDataTests
{
    //[Fact] // TODO We'll implement this later.
    public void External_Data_Files_From_Stratux_Are_Parseable()
    {
        string externalDataDirectory = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "gdl90_testdata"));
        if (!Directory.Exists(externalDataDirectory))
        {
            // This data directory is optional and may not exist on CI/target systems.
            Console.WriteLine("External test data directory not found: {0}. Skipping test.", externalDataDirectory);
            return;
        }

        foreach (string binFilePath in Directory.EnumerateFiles(externalDataDirectory, "*.bin", SearchOption.TopDirectoryOnly))
        {
            using Stream fileStream = File.OpenRead(binFilePath);
            Console.WriteLine("Found external test data file: {0}, parsing...", binFilePath);
            Assert.True(fileStream.CanRead);
        }
    }
}