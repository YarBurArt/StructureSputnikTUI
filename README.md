# StructureSputnikTUI

Console application to see what disk space is occupied, displays destination folders and files with their size, requires only normal user rights for security purposes by bypassing access errors.  Runs asynchronously in multiple threads from a folder given to it, then sorts and outputs to the console. (This is something that will need to be fixed in the future)

## How it works 
> my little cheat sheet to avoid getting lost ...

> docs made via several GPT agents base on source code and goal to see what's going on inside

### `static async Task Main()`: Asynchronous static method that is the entry point to the program. Returns Task for asynchronous operations. /
>When executing a static async Task Main() in C# CLR (Common Language Runtime), it creates an asynchronous task (Task), which is a unit of asynchronous execution for the execution machine. Within this task, various asynchronous operations are executed and dispatched to available threads so as not to block the main thread of the application. When await is encountered, execution of the current task is suspended until the expected asynchronous operation is completed, at which point execution resumes and control is eventually returned from Main().


### `Console.Write("Enter starting directory: ");`: Outputs a prompt to enter the starting directory. /
>Within the Console.Write() method, low-level operating system functions are called that are responsible for writing data to the console buffer, from where it is then output to the screen. This process involves converting the string argument into a sequence of bytes, determining the encoding, and passing the data to the appropriate operating system I/O functions for the console.

`var rootPath = Console.ReadLine() ?? Environment.GetEnvironmentVariable("HOME");`: Reads the entered directory or uses the default home directory.

`var builder = new DirectoryTreeBuilder(new DirectoryExplorer());`: Creates an object to build a hierarchical representation of the directory.

`var treeTask = builder.BuildTreeAsync(rootPath);`: Asynchronously builds a directory tree starting from the specified path.

`int totalTicks = OutputFormatterDir.GetLevelValueStProgress(rootPath);`: Calculates the total number of iterations for the progress bar.

`var options = new ProgressBarOptions { ... };`: Customizes the progress bar options.

`var progressTask = Task.Run(() => { ... });`:: Runs an asynchronous task to display the progress bar.

`await Task.WhenAll(treeTask, progressTask);`: Waits for both asynchronous tasks (tree building and progress bar display) to complete.

`var tree = treeTask.Result;`: Gets the result of building the directory tree.

`Console.SetCursorPosition(0, 0); Console.Clear();`: Clears the console.

`var flatList = tree.Flatten().OrderByDescending(n => n.Files.Sum(f => f.Length));`: Converts the tree to a flat list and sorts the files by size.

`OutputFormatterDir.PrintFromFlatlist(flatList);`: Outputs a formatted list of files.

