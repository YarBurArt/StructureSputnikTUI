using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

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
        {
            // Handle access denied
        }
        return node;
    }
}

class DirectoryNode
{
    public string Path { get; }
    public List<DirectoryNode> Children { get; set; }
    public List<FileInfo> Files { get; set; }

    public DirectoryNode(string path)
    {
        Path = path;
    }
    public IEnumerable<DirectoryNode> Flatten()
    {
        yield return this;
        foreach (var child in Children)
        {
            foreach (var flattenedChild in child.Flatten())
            {
                yield return flattenedChild;
            }
        }
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
        Console.WriteLine("Enter starting directory:");
        var rootPath = Console.ReadLine();

        var builder = new DirectoryTreeBuilder(new DirectoryExplorer());
        var tree = await builder.BuildTreeAsync(rootPath);

        var flatList = tree.Flatten().OrderByDescending(n => n.Files.Sum(f => f.Length));

        foreach (var node in flatList)
        {
            if (node.Files.Any())
            {
                Console.WriteLine($"{node.Path} (Directory):");
                foreach (var file in node.Files)
                {
                    Console.WriteLine($"\t- {file.Name} ({file.Length} bytes)");
                }
            }
            else
            {
                Console.WriteLine($"{node.Path} (Empty Directory)");
            }
        }

    }
}
