namespace Mocha.Rendering;

public class Handle
{
	public static Handle Invalid => new Handle( uint.MaxValue );

	public uint Value { get; private set; }

	public Handle( uint value )
	{
		Value = value;
	}

	public bool IsValid => Value != uint.MaxValue;
}

internal class RenderObject
{
	public Handle Handle = Handle.Invalid;

	public bool IsValid => Handle.IsValid;
}
