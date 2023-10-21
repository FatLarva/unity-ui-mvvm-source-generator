if (args.Length != 1 && args.Length != 2)
{
    Console.Error.WriteLine("Should be executed with one or two argument.");
    return;
}

var mode = args[0];
switch (mode)
{
    case "-r":
        RestoreDefaultConstants();
        break;
    case "-m":
    {
        var consoleOutputFile = args.Length == 2 ? args[1] : string.Empty;
        if (string.IsNullOrEmpty(consoleOutputFile))
        {
            return;
        }

        FulfillConstants(consoleOutputFile);
        break;
    }
}

return;


void FulfillConstants(string consoleOutputFile)
{
    string constantsFileContent = $@"
public static class BuildTimeConstants
{{
    public const string OutputFile = @""{consoleOutputFile}"";
}}
        ";

    WriteInFile(constantsFileContent);
}

void RestoreDefaultConstants()
{
    string constantsFileContent = @"
public static class BuildTimeConstants
{
    public const string OutputFile = """";
}
        ";

    WriteInFile(constantsFileContent);
}

void WriteInFile(string constantsFileContent)
{
    File.WriteAllText("BuildTimeConstants.cs", constantsFileContent);
}