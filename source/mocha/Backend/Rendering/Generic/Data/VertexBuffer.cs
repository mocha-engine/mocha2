namespace Mocha.Rendering;

internal class VertexBuffer : BaseBuffer
{
	public VertexBuffer( BufferInfo info ) : base( info )
	{
		IRenderContext.Current.CreateVertexBuffer( info, out Handle );
	}
}
