using System;
using System.IO;

namespace ViewsSourceGenerator.Tools
{
    public class OutputRedirector : IDisposable
    {
        private readonly bool _isConsoleOutputRedirected;
        private readonly StreamWriter? _fileStream;

        public OutputRedirector(string targetFilePath)
        {
            _isConsoleOutputRedirected = !string.IsNullOrEmpty(targetFilePath);

            if (!_isConsoleOutputRedirected)
            {
                return;
            }

            try
            {
                var folderPath = Path.GetDirectoryName(targetFilePath);
                if (!string.IsNullOrEmpty(folderPath))
                {
                    if (!Directory.Exists(folderPath))
                    {
                        Directory.CreateDirectory(folderPath);
                    }

                    _fileStream = new StreamWriter(targetFilePath, true);

                    Console.SetOut(_fileStream);
                    Console.SetError(_fileStream);
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }
        }

        public void Dispose()
        {
            try
            {
                if (_isConsoleOutputRedirected)
                {
                    Console.Out.Flush();
                    Console.Error.Flush();

                    if (_fileStream != null)
                    {
                        _fileStream.Close();

                        Console.SetOut(new StreamWriter(Console.OpenStandardOutput()));
                    }
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }
        }
    }
}