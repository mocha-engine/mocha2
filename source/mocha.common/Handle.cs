namespace Mocha;

public class Handle
{
	public static Handle Invalid => new Handle(uint.MaxValue);

	public uint Value { get; private set; }

	public Handle(uint value)
	{
		Value = value;
	}

	public bool IsValid => Value != uint.MaxValue;
}
