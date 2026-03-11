using System.Text;
using System.Text.RegularExpressions;

namespace ProtoBufferParser.Utilities;

/// <summary>
/// Provides naming convention utilities for converting Proto3 names
/// to Unreal Engine C++ naming conventions.
/// </summary>
public static partial class NamingHelper
{
    /// <summary>
    /// Converts a Proto3 name to PascalCase.
    /// Supports snake_case, camelCase, UPPER_CASE, and mixed inputs.
    /// </summary>
    /// <example>
    /// "player_name" → "PlayerName"
    /// "playerName" → "PlayerName"
    /// "PLAYER_NAME" → "PlayerName"
    /// "player" → "Player"
    /// "HPMax" → "HpMax"
    /// </example>
    public static string ToPascalCase(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return input;
        }

        // Split by underscores, then handle camelCase boundaries within each part
        var parts = input.Split('_', StringSplitOptions.RemoveEmptyEntries);
        var sb = new StringBuilder();

        foreach (var part in parts)
        {
            // Split camelCase/PascalCase boundaries within the part
            var subParts = SplitCamelCase(part);
            foreach (var sub in subParts)
            {
                if (sub.Length == 0) continue;
                sb.Append(char.ToUpper(sub[0]));
                if (sub.Length > 1)
                {
                    sb.Append(sub[1..].ToLower());
                }
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Converts a Proto3 field name (snake_case) to Unreal C++ property name (PascalCase).
    /// </summary>
    /// <example>
    /// "player_name" → "PlayerName"
    /// "id" → "Id"
    /// "is_active" → "IsActive"
    /// </example>
    public static string ToUnrealFieldName(string protoFieldName)
    {
        return ToPascalCase(protoFieldName);
    }

    /// <summary>
    /// Converts a Proto3 enum value name (UPPER_SNAKE_CASE) to Unreal C++ enum value name (PascalCase).
    /// </summary>
    /// <example>
    /// "UNKNOWN" → "Unknown"
    /// "ACTIVE_STATE" → "ActiveState"
    /// "STATUS_UNKNOWN" → "StatusUnknown"
    /// </example>
    public static string ToUnrealEnumValueName(string protoEnumValue)
    {
        return ToPascalCase(protoEnumValue);
    }

    /// <summary>
    /// Converts a Proto3 message name to Unreal C++ struct name.
    /// Applies F prefix and Proto suffix.
    /// </summary>
    /// <example>
    /// "Player" → "FPlayerProto"
    /// "GameSession" → "FGameSessionProto"
    /// "player_info" → "FPlayerInfoProto"
    /// </example>
    public static string ToUnrealStructName(string protoMessageName)
    {
        var pascalName = ToPascalCase(protoMessageName);
        return $"F{pascalName}Proto";
    }

    /// <summary>
    /// Converts a Proto3 enum name to Unreal C++ enum name.
    /// Applies E prefix and Proto suffix.
    /// </summary>
    /// <example>
    /// "Status" → "EStatusProto"
    /// "PlayerState" → "EPlayerStateProto"
    /// "GAME_MODE" → "EGameModeProto"
    /// </example>
    public static string ToUnrealEnumName(string protoEnumName)
    {
        var pascalName = ToPascalCase(protoEnumName);
        return $"E{pascalName}Proto";
    }

    /// <summary>
    /// Converts a Proto3 type name to Unreal C++ type name with appropriate prefix.
    /// </summary>
    /// <param name="protoTypeName">The original Proto3 type name.</param>
    /// <param name="isEnum">Whether the type is an enum (E prefix) or message (F prefix).</param>
    public static string ToUnrealTypeName(string protoTypeName, bool isEnum = false)
    {
        return isEnum
            ? ToUnrealEnumName(protoTypeName)
            : ToUnrealStructName(protoTypeName);
    }

    /// <summary>
    /// Generates the output file name (without extension) for a given type name.
    /// Applies ME prefix and Proto suffix.
    /// </summary>
    /// <example>
    /// "Player" → "MEPlayerProto"
    /// "GameSession" → "MEGameSessionProto"
    /// "Player_Inventory" → "MEPlayerInventoryProto"
    /// </example>
    public static string ToOutputFileName(string typeName)
    {
        var pascalName = ToPascalCase(typeName);
        return $"ME{pascalName}Proto";
    }

    /// <summary>
    /// Generates the flattened name for a nested type by combining parent and child names.
    /// </summary>
    /// <example>
    /// ("Player", "Inventory") → "PlayerInventory"
    /// ("Player", "Inventory", "Item") → "PlayerInventoryItem"
    /// </example>
    public static string FlattenNestedName(params string[] nameParts)
    {
        return string.Join("", nameParts.Select(ToPascalCase));
    }

    /// <summary>
    /// Converts a Proto3 .proto file name to its protoc-generated header name.
    /// </summary>
    /// <example>
    /// "player.proto" → "player.pb.h"
    /// "game_data.proto" → "game_data.pb.h"
    /// </example>
    public static string ToProtocHeaderName(string protoFileName)
    {
        ArgumentNullException.ThrowIfNull(protoFileName);

        var baseName = protoFileName.EndsWith(".proto", StringComparison.OrdinalIgnoreCase)
            ? protoFileName[..^".proto".Length]
            : protoFileName;

        return $"{baseName}.pb.h";
    }

    /// <summary>
    /// Converts a Proto3 import path to the corresponding Unreal include path.
    /// </summary>
    /// <example>
    /// "common.proto" → "MECommonProto.h"
    /// "game/player.proto" → "MEPlayerProto.h"
    /// </example>
    public static string ImportPathToInclude(string importPath)
    {
        ArgumentNullException.ThrowIfNull(importPath);

        // Extract just the file name from the import path
        var fileName = Path.GetFileNameWithoutExtension(importPath);
        var pascalName = ToPascalCase(fileName);
        return $"ME{pascalName}Proto.h";
    }

    /// <summary>
    /// Splits a string at camelCase/PascalCase boundaries.
    /// </summary>
    /// <example>
    /// "playerName" → ["player", "Name"]
    /// "HPMax" → ["HP", "Max"]
    /// "XMLParser" → ["XML", "Parser"]
    /// </example>
    private static string[] SplitCamelCase(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return [];
        }

        // Use regex to split on camelCase boundaries
        // Handles: "playerName" → "player|Name", "HPMax" → "HP|Max"
        var result = CamelCaseSplitRegex().Split(input);
        return result.Where(s => s.Length > 0).ToArray();
    }

    [GeneratedRegex(@"(?<=[a-z])(?=[A-Z])|(?<=[A-Z])(?=[A-Z][a-z])")]
    private static partial Regex CamelCaseSplitRegex();
}
