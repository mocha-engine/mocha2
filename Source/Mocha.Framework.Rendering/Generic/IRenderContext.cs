using Mocha.Rendering.Vulkan;

namespace Mocha.Rendering;

public interface IRenderContext
{
	public static IRenderContext Current { get; internal set; } = null!;

	public static void CreateVulkanRenderContext()
	{
		Current = new VulkanRenderContext();
	}

	bool HasInitialized { get; }
	bool RenderingActive { get; }

	RenderStatus Startup( IWindow window );
	RenderStatus Shutdown();

	/// <summary>
	/// Begins rendering. Call this before invoking any render functions.
	/// This should be matched with a call to EndRendering.
	/// </summary>
	/// <returns>
	/// <list type="bullet">
	/// <item><see cref="RenderStatus.Ok"/> if successful</item>
	/// <item><see cref="RenderStatus.WindowSizeInvalid"/> if the window has been minimized or made too small. Do not call EndRendering if this is returned.</item>
	/// </list>
	/// </returns>
	RenderStatus BeginRendering();

	/// <summary>
	/// Ends rendering.
	/// This should be matched with a call to BeginRendering.
	/// </summary>
	/// <returns>
	/// <list type="bullet">
	/// <item><see cref="RenderStatus.Ok"/> if successful</item>
	/// <item><see cref="RenderStatus.WindowSizeInvalid"/> if the window has been minimized or made too small.</item>
	/// </list>
	/// </returns>
	RenderStatus EndRendering();


	RenderStatus CreateImageTexture( ImageTextureInfo info, out Handle handle );
	RenderStatus CreateRenderTexture( RenderTextureInfo info, out Handle handle );
	RenderStatus SetImageTextureData( Handle handle, TextureData textureData );
	RenderStatus CopyImageTexture( Handle handle, TextureCopyData textureCopyData );

	RenderStatus CreateBuffer( BufferInfo info, out Handle handle );
	RenderStatus CreateVertexBuffer( BufferInfo info, out Handle handle );
	RenderStatus CreateIndexBuffer( BufferInfo info, out Handle handle );
	RenderStatus UploadBuffer( Handle handle, BufferUploadInfo uploadInfo );

	RenderStatus CreatePipeline( PipelineInfo info, out Handle handle );
	RenderStatus CreateDescriptor( DescriptorInfo info, out Handle handle );
	RenderStatus CreateShader( ShaderInfo info, out Handle handle );

	RenderStatus BindPipeline( Pipeline p );
	RenderStatus BindDescriptor( Descriptor d );

	RenderStatus UpdateDescriptor( Descriptor d, DescriptorUpdateInfo updateInfo );
	RenderStatus BindVertexBuffer( VertexBuffer vb );
	RenderStatus BindIndexBuffer( IndexBuffer ib );

	// RenderStatus BindConstants( RenderPushConstants p );

	RenderStatus Draw( int vertexCount, int indexCount, int instanceCount );
	RenderStatus BindRenderTarget( RenderTexture rt );
}
