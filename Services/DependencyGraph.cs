using ProtoBufferParser.Exceptions;
using ProtoBufferParser.Interfaces;
using ProtoBufferParser.Models;

namespace ProtoBufferParser.Services;

/// <summary>
/// Manages file dependencies and performs topological sorting for compilation ordering.
/// Uses Kahn's algorithm to detect circular dependencies and determine build order.
/// </summary>
public sealed class DependencyGraph
{
    private readonly Dictionary<string, ProtoFileInfo> _files = new();
    private readonly Dictionary<string, List<string>> _dependencies = new();
    private readonly ILogger _logger;

    public DependencyGraph(ILogger logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Registers a file in the dependency graph.
    /// </summary>
    public void AddFile(ProtoFileInfo file)
    {
        ArgumentNullException.ThrowIfNull(file);
        _files[file.FileName] = file;

        if (!_dependencies.ContainsKey(file.FileName))
        {
            _dependencies[file.FileName] = new List<string>();
        }
    }

    /// <summary>
    /// Adds a dependency from one file to another.
    /// <paramref name="from"/> depends on <paramref name="to"/> (i.e., from imports to).
    /// </summary>
    /// <param name="from">The file that has the import statement.</param>
    /// <param name="to">The file being imported.</param>
    public void AddDependency(string from, string to)
    {
        if (!_dependencies.ContainsKey(from))
        {
            _dependencies[from] = new List<string>();
        }

        _dependencies[from].Add(to);
    }

    /// <summary>
    /// Performs topological sort using Kahn's algorithm.
    /// Returns files in compilation order (dependencies first).
    /// </summary>
    /// <returns>Files sorted so that each file comes after its dependencies.</returns>
    /// <exception cref="CircularDependencyException">Thrown when a circular dependency is detected.</exception>
    public IReadOnlyList<ProtoFileInfo> TopologicalSort()
    {
        // Calculate in-degree for each file
        // in-degree = how many files depend on this file (i.e., this file is imported by how many)
        var inDegree = new Dictionary<string, int>();
        foreach (var file in _files.Keys)
        {
            inDegree[file] = 0;
        }

        // For each dependency edge "from depends on to", increment in-degree of "from"
        // because "from" cannot be compiled until "to" is done.
        // Actually, topological sort for compilation order:
        // If A imports B, then B must come before A.
        // So the edge is B -> A (B is a prerequisite of A).
        // in-degree of A increases when A depends on something.
        foreach (var kvp in _dependencies)
        {
            var from = kvp.Key;
            foreach (var dep in kvp.Value)
            {
                // Only count dependencies on files we know about
                if (inDegree.ContainsKey(dep))
                {
                    // 'from' depends on 'dep', so 'from' has higher in-degree
                    if (inDegree.ContainsKey(from))
                    {
                        inDegree[from]++;
                    }
                }
            }
        }

        // Start with files that have no dependencies (in-degree == 0)
        var queue = new Queue<string>();
        foreach (var kvp in inDegree)
        {
            if (kvp.Value == 0)
            {
                queue.Enqueue(kvp.Key);
            }
        }

        var result = new List<ProtoFileInfo>();

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();

            if (_files.TryGetValue(current, out var fileInfo))
            {
                result.Add(fileInfo);
            }

            // For each file that depends on 'current', reduce its in-degree
            foreach (var kvp in _dependencies)
            {
                if (kvp.Value.Contains(current))
                {
                    var dependent = kvp.Key;
                    if (inDegree.ContainsKey(dependent))
                    {
                        inDegree[dependent]--;
                        if (inDegree[dependent] == 0)
                        {
                            queue.Enqueue(dependent);
                        }
                    }
                }
            }
        }

        // Check for circular dependencies
        if (result.Count != _files.Count)
        {
            var remaining = _files.Keys.Where(f => !result.Any(r => r.FileName == f));
            var cycleFiles = string.Join(", ", remaining);
            throw new CircularDependencyException(
                $"Circular dependency detected among files: {cycleFiles}");
        }

        _logger.LogVerbose($"Compilation order: {string.Join(" -> ", result.Select(f => f.FileName))}");
        return result;
    }

    /// <summary>
    /// Builds the dependency graph from a set of parsed proto files.
    /// Extracts import paths and resolves them to file names.
    /// </summary>
    public static DependencyGraph BuildFromFiles(
        IEnumerable<ProtoFileInfo> files,
        IEnumerable<ProtoFileNode> parsedFiles,
        ILogger logger)
    {
        var graph = new DependencyGraph(logger);
        var fileInfoList = files.ToList();

        // Register all files
        foreach (var file in fileInfoList)
        {
            graph.AddFile(file);
        }

        // Add dependencies from import statements
        foreach (var parsed in parsedFiles)
        {
            var fileName = Path.GetFileNameWithoutExtension(parsed.FileName);

            foreach (var import in parsed.Imports)
            {
                // Import path is like "common.proto" or "subfolder/common.proto"
                var importedFileName = Path.GetFileNameWithoutExtension(import.Path);

                // Only add dependency if the imported file is in our set
                if (graph._files.ContainsKey(importedFileName))
                {
                    graph.AddDependency(fileName, importedFileName);
                    logger.LogVerbose($"  Dependency: {fileName} -> {importedFileName}");
                }
                else
                {
                    logger.LogWarning($"  Import '{import.Path}' in '{fileName}' not found in input directory (external dependency)");
                }
            }
        }

        return graph;
    }
}
