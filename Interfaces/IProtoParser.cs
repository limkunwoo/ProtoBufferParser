using ProtoBufferParser.Models;

namespace ProtoBufferParser.Interfaces;

/// <summary>
/// Interface for parsing .proto file content into an AST.
/// </summary>
public interface IProtoParser
{
    /// <summary>
    /// Parses the content of a .proto file and returns the AST root node.
    /// </summary>
    /// <param name="content">The raw text content of the .proto file.</param>
    /// <param name="fileName">The file name for error reporting.</param>
    /// <returns>The AST root node representing the parsed .proto file.</returns>
    ProtoFileNode Parse(string content, string fileName);
}
