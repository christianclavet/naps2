using NAPS2.Sdk.Tests;
using Xunit;

namespace NAPS2.App.Tests;

public class ConsoleAppTests : ContextualTexts
{
    [Fact]
    public void ConvertsImportedFile()
    {
        var importPath = Path.Combine(FolderPath, "in.png");
        SharedData.color_image.Save(importPath);
        var outputPath = Path.Combine(FolderPath, "out.jpg");
        var args = $"-n 0 -i \"{importPath}\" -o \"{outputPath}\"";

        var process = AppTestHelper.StartProcess("NAPS2.Console.exe", FolderPath, args);
        try
        {
            Assert.True(process.WaitForExit(2000));
            Assert.Equal(0, process.ExitCode);
            Assert.Empty(process.StandardOutput.ReadToEnd());
            AppTestHelper.AssertNoErrorLog(FolderPath);
            Assert.True(File.Exists(outputPath));
        }
        finally
        {
            AppTestHelper.Cleanup(process);
        }
    }

    [Fact]
    public void NonZeroExitCodeForError()
    {
        var importPath = Path.Combine(FolderPath, "doesnotexist.png");
        var outputPath = Path.Combine(FolderPath, "out.jpg");
        var args = $"-n 0 -i \"{importPath}\" -o \"{outputPath}\"";

        var process = AppTestHelper.StartProcess("NAPS2.Console.exe", FolderPath, args);
        try
        {
            Assert.True(process.WaitForExit(2000));
            var stdout = process.StandardOutput.ReadToEnd();
            Assert.NotEqual(0, process.ExitCode);
            Assert.NotEmpty(stdout);
            AppTestHelper.AssertErrorLog(FolderPath);
            Assert.False(File.Exists(outputPath));
        }
        finally
        {
            AppTestHelper.Cleanup(process);
        }
    }
}