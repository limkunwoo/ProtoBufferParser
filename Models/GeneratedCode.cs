namespace ProtoBufferParser.Models;

/// <summary>
/// Represents a single generated output file (.h or .cpp).
/// </summary>
public class GeneratedFile
{
    /// <summary>
    /// The file name including extension (e.g., "MEPlayerProto.h").
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// The generated source code content.
    /// </summary>
    public string Content { get; set; } = string.Empty;
}

/// <summary>
/// Represents the complete generated code output for a single type (struct or enum).
/// Includes both the header (.h) and implementation (.cpp) files.
/// </summary>
public class GeneratedTypeOutput
{
    /// <summary>
    /// The Unreal type name (e.g., "FPlayerProto" or "EStatusProto").
    /// </summary>
    public string TypeName { get; set; } = string.Empty;

    /// <summary>
    /// The header file (.h) containing the type declaration.
    /// </summary>
    public GeneratedFile HeaderFile { get; set; } = new();

    /// <summary>
    /// The implementation file (.cpp) containing the marshaling constructor.
    /// May be null/empty for enums that don't need a .cpp file.
    /// </summary>
    public GeneratedFile? CppFile { get; set; }
}

/// <summary>
/// Represents the complete code generation result for one or more .proto files.
/// </summary>
public class GeneratedCodeResult
{
    /// <summary>
    /// All generated type outputs (structs and enums).
    /// </summary>
    public List<GeneratedTypeOutput> Types { get; set; } = new();

    /// <summary>
    /// All generated files (headers + implementations) as a flat list.
    /// </summary>
    public IEnumerable<GeneratedFile> AllFiles =>
        Types.SelectMany(t =>
        {
            var files = new List<GeneratedFile> { t.HeaderFile };
            if (t.CppFile != null)
            {
                files.Add(t.CppFile);
            }
            return files;
        });
}

/// <summary>
/// Configuration options for code generation.
/// </summary>
public class CodeGeneratorOptions
{
    /// <summary>
    /// UPROPERTY specifier (default: "EditAnywhere").
    /// </summary>
    public string PropertySpecifier { get; set; } = "EditAnywhere";

    /// <summary>
    /// Whether to include BlueprintReadWrite in UPROPERTY (default: true).
    /// </summary>
    public bool GenerateBlueprintReadWrite { get; set; } = true;

    /// <summary>
    /// Category name for UPROPERTY (default: "Proto").
    /// </summary>
    public string CategoryName { get; set; } = "Proto";

    /// <summary>
    /// Whether to include auto-generated file comment header (default: true).
    /// </summary>
    public bool GenerateFileHeader { get; set; } = true;

    /// <summary>
    /// The source .proto file name (for auto-generated comments).
    /// </summary>
    public string SourceProtoFile { get; set; } = string.Empty;
}
