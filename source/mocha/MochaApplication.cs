using Mocha.Rendering;
using Mocha.Rendering.Vulkan;

namespace Mocha;

public class MochaApplication : IDisposable
{
	private int _width = 1280;
	private int _height = 720;
	private string _name = "My Mocha App";

	public string Icon;

	public Action? PreBootstrap;
	public Action? Bootstrap;

	[Obsolete( "Currently unused" )]
	public Action? MainLoop;

	[Obsolete( "Currently unused" )]
	public Action? Shutdown;

	public MochaApplication( string name )
	{
		_name = name;
	}

	public void Dispose()
	{
		Run();
	}

	private void Run()
	{
		if ( !Veldrid.RenderDoc.Load( out var renderDoc ) )
		{
			Log.Error( "Failed to initialize renderdoc" );
		}

		var platformInfo = CurrentPlatformInfo.Current();
		IRenderContext.Current = new VulkanRenderContext();

		using var window = new Window( platformInfo, _name, _width, _height );
		Render.Startup( window );

		var vertices = new float[] {
			// vec2 pos,	vec3 color
			-1, -1,			0, 0, 1,
			1, -1,			0, 1, 1,
			1, 1,			1, 1, 0,
			-1, 1,			1, 0, 0
		};
		var indices = new uint[] { 0, 1, 2, 2, 3, 0 };

		var vertexBuffer = new VertexBuffer( new BufferInfo()
		{
			Name = "Vertex Buffer",
			size = (uint)(vertices.Length * sizeof( float ) * 3),
			Type = BufferType.VertexIndexData,
			Usage = BufferUsageFlags.VertexBuffer
		} );

		var indexBuffer = new IndexBuffer( new BufferInfo()
		{
			Name = "Index Buffer",
			size = (uint)(indices.Length * sizeof( uint )),
			Type = BufferType.VertexIndexData,
			Usage = BufferUsageFlags.IndexBuffer
		} );

		vertexBuffer.Upload( new BufferUploadInfo()
		{
			Data = vertices.SelectMany( BitConverter.GetBytes ).ToArray()
		} );

		indexBuffer.Upload( new BufferUploadInfo()
		{
			Data = indices.SelectMany( BitConverter.GetBytes ).ToArray()
		} );

		//var descriptor = new Descriptor( new DescriptorInfo()
		//{
		//	Bindings = new List<DescriptorBindingInfo>(),
		//	Name = "My Descriptor"
		//} );

		var pipeline = new Pipeline( new PipelineInfo
		{
			ShaderInfo = new()
			{
				Name = "My Shader",
				FragmentData = ShaderData.Load( "/shaders/default.shader" ).FragmentData,
				VertexData = ShaderData.Load( "/shaders/default.shader" ).VertexData
			},

			RenderToSwapchain = true,
			// Descriptors = [descriptor],
			VertexAttributes = [new VertexAttributeInfo() { Format = VertexAttributeFormat.Float2, Name = "Position" }, new VertexAttributeInfo() { Format = VertexAttributeFormat.Float3, Name = "Color" }]
		} );

		window.Render += ( dt ) =>
		{
			Render.BeginRendering();

			Render.BindPipeline( pipeline );
			// Render.BindDescriptor( descriptor );
			Render.BindVertexBuffer( vertexBuffer );
			Render.BindIndexBuffer( indexBuffer );
			Render.Draw( vertices.Length, indices.Length, 1 );

			Render.EndRendering();
		};

		window.Run();
		Render.Shutdown();
	}
}
