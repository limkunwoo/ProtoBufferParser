using ProtoBufferParser.Models;

namespace ProtoBufferParser.Interfaces;

/// <summary>
/// Defines the contract for generating target platform code from a flattened Proto3 AST.
/// </summary>
public interface ICodeGenerator
{
    /// <summary>
    /// Generates all output files (headers and implementations) for a single .proto file AST.
    /// The AST should already have nested types flattened by MessageFlattener.
    /// </summary>
    /// <param name="ast">The flattened Proto3 file AST.</param>
    /// <returns>A result containing all generated type outputs.</returns>
    GeneratedCodeResult Generate(ProtoFileNode ast);
}
