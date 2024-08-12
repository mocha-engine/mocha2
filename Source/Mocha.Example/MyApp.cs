using Apparatus.Core.Rendering;

public class MyApp : MochaApplication
{
	private Pipeline _pipeline = null!;
	private VertexBuffer _vertexBuffer = null!;
	private IndexBuffer _indexBuffer = null!;
	private Descriptor _descriptor = null!;
	private ImageTexture _imageTexture = null!;

	private readonly float[] _vertices = [
		// vec2 pos,	vec2 uv
		-1,-1,			0,0,
		1,-1,			1,0,
		1,1,			1,1,
		-1,1,			0,1
	];

	private readonly uint[] _indices = [0, 1, 2, 2, 3, 0];

	public MyApp()
	{
		Name = "Mocha Example";
	}

	protected override void OnBootstrap()
	{
		_vertexBuffer = new VertexBuffer( new BufferInfo()
		{
			Name = "Vertex Buffer",
			size = (uint)(_vertices.Length * sizeof( float ) * 7),
			Type = BufferType.VertexIndexData,
			Usage = BufferUsageFlags.VertexBuffer
		} );

		_indexBuffer = new IndexBuffer( new BufferInfo()
		{
			Name = "Index Buffer",
			size = (uint)(_indices.Length * sizeof( uint )),
			Type = BufferType.VertexIndexData,
			Usage = BufferUsageFlags.IndexBuffer
		} );

		_vertexBuffer.Upload( new BufferUploadInfo()
		{
			Data = _vertices.SelectMany( BitConverter.GetBytes ).ToArray()
		} );

		_indexBuffer.Upload( new BufferUploadInfo()
		{
			Data = _indices.SelectMany( BitConverter.GetBytes ).ToArray()
		} );

		var image = Mocha.TextureData.Load( "test.texture" );

		_imageTexture = new ImageTexture( new ImageTextureInfo()
		{
			Width = image.Width,
			Height = image.Height,
			MipCount = image.MipCount,
			Name = "test.texture"
		} );

		_imageTexture.SetData( new Apparatus.Core.Rendering.TextureData()
		{
			Width = image.Width,
			Height = image.Height,
			ImageFormat = image.Format,
			MipCount = image.MipCount,
			MipData = image.Data
		} );

		_descriptor = new Descriptor( new DescriptorInfo()
		{
			Bindings = new List<DescriptorBindingInfo> {
				new DescriptorBindingInfo()
				{
					Image = _imageTexture,
					Type = DescriptorBindingType.Image
				}
			},
			Name = "My Descriptor"
		} );

		var shaderData = ShaderData.Load( "/shaders/default.shader" );

		_pipeline = new Pipeline( new PipelineInfo
		{
			ShaderInfo = new()
			{
				Name = "My Shader",
				FragmentData = shaderData.FragmentData,
				VertexData = shaderData.VertexData
			},

			RenderToSwapchain = true,
			Descriptors = [_descriptor],
			VertexAttributes = [
				new VertexAttributeInfo() { Format = VertexAttributeFormat.Float2, Name = "Position" },
				new VertexAttributeInfo() { Format = VertexAttributeFormat.Float2, Name = "TexCoord" },
			]
		} );
	}

	protected override void OnRender()
	{
		Render.BeginRendering();

		Render.BindPipeline( _pipeline );
		Render.UpdateDescriptor( _descriptor, new DescriptorUpdateInfo() { Source = _imageTexture } );
		Render.BindDescriptor( _descriptor );
		Render.BindVertexBuffer( _vertexBuffer );
		Render.BindIndexBuffer( _indexBuffer );
		Render.Draw( _vertices.Length, _indices.Length, 1 );

		Render.EndRendering();
	}

	protected override void OnShutdown()
	{
	}
}
