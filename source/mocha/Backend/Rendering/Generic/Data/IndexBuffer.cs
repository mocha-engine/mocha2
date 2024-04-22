namespace Mocha.Rendering;

internal class IndexBuffer : BaseBuffer
{
	public IndexBuffer( BufferInfo info ) : base( info )
	{
		IRenderContext.Current.CreateIndexBuffer( info, out Handle );
	}
}
