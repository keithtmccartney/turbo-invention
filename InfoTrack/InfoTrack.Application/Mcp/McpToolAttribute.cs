namespace InfoTrack.Application.Mcp;

/// <summary>
/// Marks an <see cref="IMcpToolProvider"/> for automatic discovery and registration at startup.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class McpToolAttribute(string name, string description) : Attribute
{
    public string Name { get; } = name;

    public string Description { get; } = description;
}
