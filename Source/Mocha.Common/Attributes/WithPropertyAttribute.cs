namespace Mocha;

/// <summary>
/// <para>
/// <b>Note:</b> this structure must be marked as partial.
/// </para>
/// </summary>
[AttributeUsage( AttributeTargets.Field )]
public class WithPropertyAttribute : Attribute
{
	public string? Name { get; }

	public WithPropertyAttribute()
	{
			
	}

	public WithPropertyAttribute( string name )
	{
		Name = name;
	}
}
