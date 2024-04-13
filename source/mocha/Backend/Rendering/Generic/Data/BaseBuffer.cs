namespace Mocha.Rendering;

internal class BaseBuffer : RenderObject
{
	public BaseBuffer( BufferInfo info )
	{
		IRenderContext.Current.CreateBuffer( info, out Handle );
	}

	public void Upload( BufferUploadInfo uploadInfo )
	{

	}
}
