namespace Mocha;

/// <summary>
/// <para>
/// Specifies that this can be (de-)serialized through JSON.
/// </para>
/// <para>
/// Will codegen a static <c>Load( string filePath )</c> function, which loads using <see cref="FileSystem.Content" />.
/// </para>
/// <para>
/// <b>Note:</b> this structure must be marked as partial.
/// </para>
/// </summary>
[AttributeUsage( AttributeTargets.Struct, AllowMultiple = false, Inherited = false )]
public class ResourceAttribute : Attribute
{
}
