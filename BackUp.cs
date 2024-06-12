using System.IO.Compression;

namespace MineCopilot;

class FileProcess{
    public static async Task backup(string[] args) {
        string targetPath = args[0];
        string backupPath = @"..\backup";
        ZipFile.CreateFromDirectory(targetPath,backupPath);
    }
}