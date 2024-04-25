using System.Security.Cryptography;
using Newtonsoft.Json.Linq;

namespace MineCopilot;

class CorePrograss
{
    static readonly HttpClient client = new();

    static string FileCheck(string file)
    {
        // 确保文件存在
        if (!File.Exists(file))
        {
            // 创建文件并写入提示信息
            File.WriteAllText(file, "在新的一行中填入要下载的分支版本(release|snapshot):\n");
            Console.WriteLine("请在version.txt文件中填入需要下载或更新的分支的，release或snapshot");
            return "Failed";
        }
        // 读取文件所有行
        var lines = File.ReadAllLines(file);
        if (lines.Length == 0 || lines.Last() != "release" && lines.Last() != "snapshot")
        {
            // 文件存在但是最后一行不是有效的分支名称
            Console.WriteLine("请在version.txt文件中填入需要下载或更新的分支的，release或snapshot");
            return "Failed";
        }
        // 返回文件中的最后一行（有效分支名称）
        return lines.Last();
    }

    private static string ArgsCheck(string[] args)
    {
        if (args.Length != 0)
        {
            Console.WriteLine(args[0]);
            if (args[0] == "release" || args[0] == "snapshot")
            {
                return args[0];
            }
        }
        return "Failed";
    }
    public static async Task ServerCoreUpdate(string[] args)
    {

        string? version = ArgsCheck(args);
        if (version == "Failed")
        {
            version = FileCheck("version.txt");
            if (version == "Failed")
            {
                Console.WriteLine("未检查到合法传参或配置文件，将使用默认release分支");
                version = "release";
            }
        }
        string minecraftVersionManifest = "http://launchermeta.mojang.com/mc/game/version_manifest.json";

        // 获取Minecraft版本清单JSON
        var manifestResponse = await client.GetStringAsync(minecraftVersionManifest);
        var manifest = JObject.Parse(manifestResponse);
        Console.WriteLine(version);
        var currentVersion = manifest["latest"]![version]!.ToString();
        string? url = null;

        foreach (var v in manifest["versions"]!)
        {
            if ((string)v["id"]! == currentVersion)
            {
                url = (string)v["url"]!;
                break;
            }
        }

        if (url == null)
        {
            Console.WriteLine("版本有问题");
            Environment.Exit(1);
        }

        // 获取版本元数据
        var versionMetaResponse = await client.GetStringAsync(url);
        var versionMeta = JObject.Parse(versionMetaResponse);
        var server = versionMeta["downloads"]!["server"]!;

        string fileName = "server.jar";
        int NeedtoDelete = 0;

        if (File.Exists(fileName))
        {
            using var currentFile = File.OpenRead(fileName);
            using var sha1 = SHA1.Create();
            string fileSha1Hex = BitConverter.ToString(sha1.ComputeHash(currentFile)).Replace("-", "").ToLower();
            if (fileSha1Hex == (string)server["sha1"]!)
            {
                Console.WriteLine($"无需更新,当前版本为{version}分支下的{currentVersion}版本");
                Environment.Exit(1);
            }
            else
            {
                NeedtoDelete = 1;
            }
        }
        if (NeedtoDelete != 0) File.Delete(fileName);

        byte[] tmp;
        string tmpSha1Hex;
        do
        {
            tmp = await client.GetByteArrayAsync((string)server["url"]!);
            tmpSha1Hex = BitConverter.ToString(SHA1.HashData(tmp)).Replace("-", "").ToLower();
        } while (tmpSha1Hex != (string)server["sha1"]!);

        await File.WriteAllBytesAsync(fileName, tmp);
        Console.WriteLine($"已更新到{version}分支的{currentVersion}版本");
    }
}