namespace Mocha.Rendering;

public class Descriptor : RenderObject
{
	public Descriptor( DescriptorInfo info )
	{
		IRenderContext.Current.CreateDescriptor( info, out Handle );
	}
}
