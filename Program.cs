﻿using ShellProgressBar;
using System.Text;

// Program for easy visualization and analysis of the file system structure

interface IDirectoryExplorer {
    Task<DirectoryNode> Explore(string path);
}

public class CustomSearcher {
    public static List<string> GetDirectories(string path, string searchPattern = "*", SearchOption searchOption = SearchOption.AllDirectories) {
        // track visited directories to avoid duplicates in rec
        var exploredDirectories = new Dictionary<string, bool>();
        var directories = new List<string>();

        if (searchOption == SearchOption.TopDirectoryOnly) 
            return Directory.GetDirectories(path, searchPattern).ToList();      

        var queue = new Queue<string>();
        queue.Enqueue(path);

        while (queue.Count > 0) { // breadth-first search through the directory tree
            var currentPath = queue.Dequeue();

            if (exploredDirectories.ContainsKey(currentPath)) continue;

            try { // get subdirectories and add full paths
                var directoryInfo = new DirectoryInfo(currentPath);
                var subdirectories = directoryInfo.GetDirectories(searchPattern);
                directories.AddRange(subdirectories.Select(d => d.FullName));

                foreach (var subdirectory in subdirectories) {
                    queue.Enqueue(subdirectory.FullName);
                    Console.WriteLine(subdirectory);
                }
            }
            catch (UnauthorizedAccessException) {
                directories.Add(currentPath);
            }
        }
        
        return directories;
    }
}

class DirectoryExplorer : IDirectoryExplorer {
    // Explores the directory at the specified path and returns a DirectoryNode representing the directory structure
    public async Task<DirectoryNode> Explore(string path) {
        var node = new DirectoryNode(path);
        try { // parallel exploration with error handling
            var subdirectoryTasks = CustomSearcher.GetDirectories(path)
                .Select(dir => Task.Run(() => Explore(dir)));

            var subdirectories = await Task.WhenAll(subdirectoryTasks);
            node.Children = subdirectories.ToList();

            node.Files = Directory.GetFiles(path).Select(f => new FileInfo(f)).ToList();
        }
        catch (UnauthorizedAccessException) { }

        return node;
    }
}

class DirectoryNode {
    // Represents a directory node in a directory structure
    public string Path { get; }
    public List<DirectoryNode> Children { get; set; }
    public List<FileInfo> Files { get; set; }

    public DirectoryNode(string path) => Path = path;
    // Flattens the directory structure by returning a sequence of all DirectoryNode instances
    // in a depth-first traversal of the directory tree return
    public IEnumerable<DirectoryNode> Flatten() {
        yield return this;
        foreach (var child in Children)
            foreach (var flattenedChild in child.Flatten())
                yield return flattenedChild;
    }
}


class DirectoryTreeBuilder {
    // to build a hierarchical representation of the file system
    // by exploring directories and creating a tree of DirectoryNode inst
    private readonly IDirectoryExplorer _explorer;

    public DirectoryTreeBuilder(IDirectoryExplorer explorer) {
        _explorer = explorer;
    }

    public async Task<DirectoryNode> BuildTreeAsync(string rootPath) {
        return await Task.Run(() => _explorer.Explore(rootPath));
    }
}

public class OutputFormatterDir {
    // formats console output for readability, add color, | symbols, byte to KB/MB/GB
    public static int GetLevelValueStProgress(string path) {
        // from 1000000 to 10 for progress speed by dir level
        string[] directories = path.Split(Path.DirectorySeparatorChar);
        int levels = directories.Length - 1;
        levels = Math.Min(levels, 5);
        double baseValue = 1000000;
        double result = baseValue / Math.Pow(10, levels);

        return (int)Math.Round(result);
    }

    internal static void PrintFromFlatlist(IOrderedEnumerable<DirectoryNode> flatlist) {
        // correctly outputs a ready sorted data structure with folders and files
        foreach (var node in flatlist) {
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.Write("┌─ ");
            Console.ResetColor();
            Console.WriteLine(node.Path);

            if (node.Files.Any()) {
                var sortedFiles = node.Files.OrderByDescending(f => f.Length);
                foreach (var file in sortedFiles) {
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

    internal static void PrintDirectoryTree(DirectoryNode node, int level = 0) {
        // Sort the files in the current directory by size
        var sortedFiles = node.Files.OrderByDescending(f => f.Length);

        // Print the current directory
        Console.ForegroundColor = ConsoleColor.Gray;
        Console.Write(new string(' ', level * 2) + "├─ ");
        Console.ResetColor();
        Console.WriteLine(Path.GetFileName(node.Path));

        // Print the files in the current directory
        foreach (var file in sortedFiles) {
            var sizeString = GetReadableFileSizeString(file.Length);
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Write(new string(' ', (level + 1) * 2) + "├─ ");
            Console.ResetColor();
            Console.WriteLine($"{file.Name} ({sizeString})");
        }

        // Recursively print the subdirectories
        foreach (var child in node.Children.OrderByDescending(c => c.Flatten().Count())) {
            PrintDirectoryTree(child, level + 1);
        }
    }
    internal static void PrintDirectoryTreeWithRectangles(DirectoryNode node, long maxDiskSize) {
        // FIXME: 
        const int minWidth = 2;

        void DrawRectangle(long size, string label, ConsoleColor color)
        {
            int width = (int)(size * Console.WindowWidth / maxDiskSize);
            width = Math.Max(width, minWidth);

            if (label.Length > width | label.Length < 0)
                label = label.Substring(0, width) + "...";
            

            Console.ForegroundColor = color;
            Console.Write(new string('█', width));

            // Set cursor position to the beginning of the rectangle
            Console.SetCursorPosition(Console.CursorLeft, Console.CursorTop);
            Console.Write(label);
            Console.ResetColor();
            Console.WriteLine();
        }
        // Get total size of the current directory and its children
        long totalSize = GetTotalSize(node);

        DrawRectangle(totalSize, Path.GetFileName(node.Path), ConsoleColor.Blue);

        foreach (var file in node.Files.OrderByDescending(f => f.Length))
            DrawRectangle(file.Length, file.Name, ConsoleColor.Green);
        foreach (var child in node.Children)
            PrintDirectoryTreeWithRectangles(child, maxDiskSize);

    }

    internal static long GetTotalSize(DirectoryNode node) {
        long size = node.Files.Sum(f => f.Length);
        foreach (var child in node.Children) size += GetTotalSize(child);
        return size;
    }
    public static long DirSize(DirectoryInfo d) {
        long size = 0; // FIXME:
        FileInfo[] fis = d.GetFiles(); // Add file sizes.
        foreach (FileInfo fi in fis) size += fi.Length;
        DirectoryInfo[] dis = d.GetDirectories(); // Add subdirectory sizes.
        foreach (DirectoryInfo di in dis) size += DirSize(di);
        return size;
    }
    private static string GetReadableFileSizeString(long size) {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = size;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1) {
            order++;
            len /= 1024;
        }

        return $"{Math.Round(len, 1)} {sizes[order]}";
    }
}

class Program {
    static async Task Main() {
        // Prompting the user => Building a hierarchical representation & Displaying a progress bar
        // Flattening the directory tree and printing the files sorted by descending size
        Console.Write("Enter starting directory: ");
        var rootPath = Console.ReadLine() ?? Environment.GetEnvironmentVariable("HOME");

        var builder = new DirectoryTreeBuilder(new DirectoryExplorer());
        var treeTask = builder.BuildTreeAsync(rootPath);
        
        // the total number of ticks for the progress bar based on path
        int totalTicks = OutputFormatterDir.GetLevelValueStProgress(rootPath);
        var options = new ProgressBarOptions { ProgressCharacter = '#', ProgressBarOnBottom = true };
        var progressTask = Task.Run(() => {
            using (var pbar = new ProgressBar(totalTicks, "progress bar to explore dir", options)) {
                for (int i = 0; i < totalTicks; i++) {
                    pbar.Tick(); // delay to make the progress bar animation smoother
                    Task.Delay(50).Wait(); // Adjust delay as needed
                }
            }
        });
        // for both the directory tree building and the progress bar tasks to complete
        await Task.WhenAll(treeTask, progressTask);
        var tree = treeTask.Result;

        Console.SetCursorPosition(0, 0); // crutch for cleaning up the progress bar bugs
        Console.Clear();
        // sort higher to lower size
        var flatList = tree.Flatten().OrderByDescending(n => n.Files.Sum(f => f.Length));
        // OutputFormatterDir.PrintFromFlatlist(flatList);
        //OutputFormatterDir.PrintDirectoryTree(tree);
        //OutputFormatterDir.SaveDirectoryTreeToHtml(tree, "out");
        Console.WindowWidth = Console.LargestWindowWidth;
        Console.WindowHeight = Console.LargestWindowHeight;
        OutputFormatterDir.PrintDirectoryTreeWithRectangles(
            tree, OutputFormatterDir.DirSize(new DirectoryInfo(rootPath)));
    }
}