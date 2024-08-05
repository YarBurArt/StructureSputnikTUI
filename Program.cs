using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Drawing;
using System.Threading.Tasks;
using ShellProgressBar;


interface IDirectoryExplorer
{
    DirectoryNode Explore(string path);
}

public class CustomSearcher
{
    public static List<string> GetDirectories(string path, string searchPattern = "*",
        SearchOption searchOption = SearchOption.AllDirectories)
    {
        if (searchOption == SearchOption.TopDirectoryOnly)
            return Directory.GetDirectories(path, searchPattern).ToList();
        var directories = new List<string>(GetDirectories(path, searchPattern));
        for (var i = 0; i < directories.Count; i++)
            directories.AddRange(GetDirectories(directories[i], searchPattern));
        return directories;
    }
    private static List<string> GetDirectories(string path, string searchPattern)
    {
        try
        {
            return Directory.GetDirectories(path, searchPattern).ToList();
        }
        catch (UnauthorizedAccessException)
        {
            return new List<string>();
        }
    }
}

class DirectoryExplorer : IDirectoryExplorer
{
    public DirectoryNode Explore(string path)
    {
        var node = new DirectoryNode(path);
        try
        {
            var directories = CustomSearcher.GetDirectories(path);
            var files = Directory.GetFiles(path);
            node.Children = directories.Select(d => Explore(d)).ToList();
            node.Files = files.Select(f => new FileInfo(f)).ToList();
        }
        catch (UnauthorizedAccessException)
        { }
        return node;
    }
}

class DirectoryNode
{
    public string Path { get; }
    public List<DirectoryNode> Children { get; set; }
    public List<FileInfo> Files { get; set; }

    public DirectoryNode(string path) => Path = path;
    public IEnumerable<DirectoryNode> Flatten()
    {
        yield return this;
        foreach (var child in Children)   
            foreach (var flattenedChild in child.Flatten())
                yield return flattenedChild;
    }
}

class DirectoryTreeBuilder
{
    private readonly IDirectoryExplorer _explorer;

    public DirectoryTreeBuilder(IDirectoryExplorer explorer)
    {
        _explorer = explorer;
    }

    public async Task<DirectoryNode> BuildTreeAsync(string rootPath)
    {
        // Async implementation using Task.Run and Parallel.ForEach
        return await Task.Run(() => _explorer.Explore(rootPath));
    }
}

class Program
{
    static async Task Main()
    {
        Console.Write("Enter starting directory: ");
        var rootPath = Console.ReadLine();

        var builder = new DirectoryTreeBuilder(new DirectoryExplorer());
        var treeTask = builder.BuildTreeAsync(rootPath: rootPath);

        int totalTicks = GetLevelValue(rootPath);
        var options = new ProgressBarOptions { ProgressCharacter = '#', ProgressBarOnBottom = true };
        var progressTask = Task.Run(() =>
        {
            using (var pbar = new ProgressBar(totalTicks, "progress bar to explore dir", options))
            {
                for (int i = 0; i < totalTicks; i++)
                {
                    pbar.Tick();
                    Task.Delay(50).Wait(); // Adjust delay as needed
                }
            }
        });

        await Task.WhenAll(treeTask, progressTask);

        var tree = await treeTask;
        Console.SetCursorPosition(0, 0);
        Console.Clear();
        var flatList = tree.Flatten().OrderByDescending(n => n.Files.Sum(f => f.Length));
        PrintFromFlatlist(flatList);
    }
    static int GetLevelValue(string path)
    {// from 100000 to 10 for progress speed by dir level
        string[] directories = path.Split(Path.DirectorySeparatorChar);
        int levels = directories.Length - 1; 
        levels = Math.Min(levels, 5); 
        double baseValue = 100000; 
        double result = baseValue / Math.Pow(10, levels);

        return (int)Math.Round(result);
    }

    static void PrintFromFlatlist (IOrderedEnumerable<DirectoryNode> flatlist) 
    {
        foreach (var node in flatlist)
        {
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.Write("┌─ ");
            Console.ResetColor();
            Console.WriteLine(node.Path);

            if (node.Files.Any())
            {
                foreach (var file in node.Files)
                {
                    var sizeString = GetReadableFileSizeString(file.Length);
                    Console.ForegroundColor = ConsoleColor.Gray;
                    Console.Write("│  ├─ ");
                    Console.ResetColor();
                    Console.WriteLine($"{file.Name} ({sizeString})");
                }
            }
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine("└───────────────────────────");
            Console.ResetColor();
        }
    }
    static string GetReadableFileSizeString(long size)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = size;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }

        return $"{Math.Round(len, 1)} {sizes[order]}";
    }
}
