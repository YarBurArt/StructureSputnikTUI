# StructureSputnikTUI

Console application to see what disk space is occupied, displays destination folders and files with their size, requires only normal user rights for security purposes by bypassing access errors.  Runs asynchronously in multiple threads from a folder given to it, then sorts and outputs to the console. (This is something that will need to be fixed in the future)

# How it works 
> my little cheat sheet to avoid getting lost ...

> docs made via several GPT agents base on source code and goal to see what's going on inside line-by-line

## `static async Task Main() { ... }`: Asynchronous static method that is the entry point to the program. Returns Task for asynchronous operations /
When executing a `static async Task Main()` in C# CLR (Common Language Runtime), it creates an asynchronous task (`Task`), which is a unit of asynchronous execution for the execution machine. Within this task, various asynchronous operations are executed and dispatched to available threads so as not to block the main thread of the application. When await is encountered, execution of the current task is suspended until the expected asynchronous operation is completed, at which point execution resumes and control is eventually returned from `Main()`.


## `Console.Write("Enter starting directory: ");`: Outputs a prompt to enter the starting directory /
Within the Console.Write() method, low-level operating system functions are called that are responsible for writing data to the console buffer, from where it is then output to the screen. This process involves converting the string argument into a sequence of bytes, determining the encoding, and passing the data to the appropriate operating system I/O functions for the console.

## `var rootPath = Console.ReadLine() ?? Environment.GetEnvironmentVariable("HOME");`: Reads the entered directory or uses the default home directory /
inside, low-level operating system functions are called to read user input from the console buffer, and if no user input has been made, operating system functions are called to retrieve the value of the "HOME" environment variable, and then return the retrieved value and assign it to the rootPath variable.

## `var builder = new DirectoryTreeBuilder(new DirectoryExplorer());`: Creates an object to build a hierarchical representation of the directory /

### loaded code in new DirectoryExplorer /
`public async Task<DirectoryNode> Explore(string path) { ... }`: Asynchronous method that explores a directory and returns a DirectoryNode representing its structure.

`var node = new DirectoryNode(path);`: Creates a new DirectoryNode object for the current directory: Memory is allocated for the new DirectoryNode object.
The DirectoryNode constructor is called, which initializes the object's properties, such as Path


`var subdirectoryTasks = ...`: Gets a list of subdirectory paths using CustomSearcher.GetDirectories and creates asynchronous tasks to explore each one (Explore(dir)) in parallel: For each subdirectory path, a new asynchronous Task<DirectoryNode> task is created that calls the Explore(dir) method recursively.
These tasks are placed in the subdirectoryTasks collection /
`CustomSearcher.GetDirectories` /

- Memory allocation:
A `Dictionary<string, bool>` object is created to keep track of visited directories.
A `List<string>` object is created to store the directories found.
A `Queue<string>` object is created to store paths to directories yet to be processed. 
- Read from persistent memory:
The `Directory.GetDirectories()` method is called to retrieve the list of directories in the specified path.
`DirectoryInfo` objects are created to retrieve information about the directories.
- Working with RAM:
Items are retrieved from `Queue<string>` and processed.
The found directories are added to `List<string>`.
Information about the visited directories is stored in Dictionary`<string, bool>`.
Reading from persistent memory:
The `DirectoryInfo.GetDirectories()` method is called to get a list of subdirectories


`var subdirectories = await Task.WhenAll(subdirectoryTasks);`: Waits for all subdirectory exploration tasks to finish and stores the results in subdirectories.

`node.Children = subdirectories.ToList();`: Assigns the explored subdirectories to the Children property of the current DirectoryNode.

`node.Files = ...`: Gets a list of file paths within the current directory and creates a list of FileInfo objects for each file.

`try...catch`: Handles UnauthorizedAccessException (permission errors) silently (no action taken).

`return node;`: Returns the populated DirectoryNode representing the explored directory structure.

### its `DirectoryTreeBuilder` wrapper to use /
`class DirectoryTreeBuilder`:
for building a hierarchical representation of the file system as a tree of DirectoryNode objects.
It encapsulates the logic for exploring directories and creating a directory tree.

`private readonly IDirectoryExplorer _explorer;`:
This field stores a reference to an object that implements the IDirectoryExplorer interface.
This interface defines an Explore(string path) method that returns a DirectoryNode object representing the directory structure.

`public DirectoryTreeBuilder(IDirectoryExplorer explorer)`:
The constructor of the DirectoryTreeBuilder class accepts an object that implements the IDirectoryExplorer interface.
This object is stored in the _explorer field for later use.

`public async Task<DirectoryNode> BuildTreeAsync(string rootPath)`:
This method is the entry point for building a directory tree.
It accepts the path to the directory to be explored.
Inside the method, `_explorer.Explore(rootPath)` is called asynchronously using `Task.Run()`.
The result of this call (a `DirectoryNode` object) is returned as the result of the method.

## `var treeTask = builder.BuildTreeAsync(rootPath);`: Asynchronously builds a directory tree starting from the specified path /
Calls the `BuildTreeAsync(string rootPath)` method on the builder object.
This method starts an asynchronous operation that examines the directory specified in rootPath and builds a `DirectoryNode` object that represents the file system structure.
The runtime machine returns a `Task<DirectoryNode>` object that represents this asynchronous operation and stores it in the treeTask variable. See above for their operation.

---

>code then just progress bar parallel
---

## `int totalTicks = OutputFormatterDir.GetLevelValueStProgress(rootPath);`: Calculates the total number of iterations for the progress bar /
### `GetLevelValueStProgress(string path)` /
`string[] directories = path.Split(Path.DirectorySeparatorChar);`:
The `Split()` method splits the input string path into an array of strings using the Path.DirectorySeparatorChar delimiter character.
The result is an array of directories containing all the directory levels in path.

`int levels = directories.Length - 1;`:
Calculates the number of directory levels in path by subtracting 1 from the length of the directories array.
This is necessary because the directories array contains N+1 elements for N directory levels.

`levels = Math.Min(levels, 5);`:
Limits the maximum number of directory levels to 5.
This is to avoid progress values being too small for very deep directories.
`double baseValue = 1000000;`:
Specifies a base value equal to 1000000.
This value will be used to calculate the progress.

`double result = baseValue / Math.Pow(10, levels);`:
Calculates the progress value based on the number of directory levels.
The formula baseValue / Math.Pow(10, levels) reduces the base value by a factor of 10 for each directory level.
Thus, the more directory levels there are, the smaller the progress value will be.

`return (int)Math.Round(result);`:
The result of the calculation is rounded to the nearest integer.
This progress value is returned as the result of the method.

## `var options = new ProgressBarOptions { ... };`: Customizes the progress bar options /
`new ProgressBarOptions();`:
Allocates memory for the new ProgressBarOptions object.
The default constructor is called, initializing the object's properties.

`options.ProgressCharacter = '#';`:
Sets the value of the ProgressCharacter property of the options object to '#'.
`options.ProgressBarOnBottom = true;`:
Sets the value of the ProgressBarOnBottom property of the options object to true.

## `var progressTask = Task.Run(() => { ... });`:: Runs an asynchronous task to display the progress bar. /
Creates a new `Task` object that will perform the asynchronous operation defined in the anonymous function.
Starts the execution of this asynchronous operation in a separate thread.
Returns a `Task` object representing this asynchronous operation and stores it in the `progressTask` variable

## `await Task.WhenAll(treeTask, progressTask);`: Waits for both asynchronous tasks (tree building and progress bar display) to complete.
Creates a new `Task` object that will wait for the completion of two other tasks: `treeTask` and `progressTask`.
Suspends execution of the current thread and switches to other available threads until the new `Task` object is completed.
When both tasks (`treeTask` and `progressTask`) are completed, the runtime machine resumes execution of the current thread and continues code execution.

### `var tree = treeTask.Result;`: Gets the result of building the directory tree.

### `Console.SetCursorPosition(0, 0); Console.Clear();`: Clears the console.
---
>The code below actively needs reworking, it's a stub for the output
---

## `var flatList = tree.Flatten().OrderByDescending(n => n.Files.Sum(f => f.Length));`: Converts the tree to a flat list and sorts the files by size /
### `public IEnumerable<DirectoryNode> Flatten() { ... }` /
`yield return this;`:
Returns the current DirectoryNode object as the first element of the sequence.

`foreach (var child in Children)`:
Starts looping through all child DirectoryNode objects in the Children property.

`foreach (var flattenedChild in child.Flatten())`:
For each child object, calls the Flatten() method recursively.
This allows to "flatten" the entire directory hierarchy.

`yield return flattenedChild;`:
Returns each "flattened" child DirectoryNode object as the next element in the sequence.

### `.OrderByDescending()` /
The `OrderByDescending()` extension method is called on a sequence of DirectoryNode objects.
For each DirectoryNode object, a value is calculated using the lambda expression `n => n.Files.Sum(f => f.Length)`.

This is a lambda expression / 
Gets the collection of files (`n.Files`) for each `DirectoryNode` object.
Sums the sizes of all files (`f.Length`) in this collection.
Returns the resulting sum as a sort value.
The runtime machine sorts the sequence of `DirectoryNode` objects in descending order (`.OrderByDescending()`) by the calculated values 
## `OutputFormatterDir.PrintFromFlatlist(flatList);`: Outputs a formatted list of files /
### internal static void PrintFromFlatlist(IOrderedEnumerable<DirectoryNode> flatlist) {...} /
`foreach (var node in flatlist)`:
Searches each DirectoryNode object in the sorted flatlist sequence.

`Console.ForegroundColor = ConsoleColor.DarkCyan; ... Console.ResetColor();`:
Sets the console text color to dark blue and then resets it.

`if (node.Files.Any())`:
Checks if there are files in the current directory.

`foreach (var file in node.Files)`:
Searches every file in the current directory.

`var sizeString = GetReadableFileSizeString(file.Length);`:
Calls the GetReadableFileSizeString() helper method to get a string representation of the file size / 

Creates an array of units of measure.
Reduces and indexes until the size is less than 1024.
Formats a string with the size and unit.

`Console.WriteLine($"{file.Name} ({sizeString})");`:
Outputs the file name and size to the console. Outputs the delimiter when the files are complete.
