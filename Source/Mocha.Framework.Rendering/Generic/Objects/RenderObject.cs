namespace Apparatus.Core.Rendering;

public class RenderObject
{
	public Handle Handle = Handle.Invalid;

	public bool IsValid => Handle.IsValid;
}
