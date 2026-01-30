namespace DnsmasqWebUI.Tests;

/// <summary>
/// Resolves paths to testdata files. Files are copied to output (testdata/) when the project builds.
/// </summary>
public static class TestDataHelper
{
    public static string TestDataPath => Path.Combine(AppContext.BaseDirectory, "testdata");

    public static string GetPath(string relativePath) =>
        Path.Combine(TestDataPath, relativePath);

    public static string ReadAllText(string relativePath) =>
        File.ReadAllText(GetPath(relativePath));

    public static string[] ReadAllLines(string relativePath) =>
        File.ReadAllLines(GetPath(relativePath));

    public static bool Exists(string relativePath) =>
        File.Exists(GetPath(relativePath));
}
