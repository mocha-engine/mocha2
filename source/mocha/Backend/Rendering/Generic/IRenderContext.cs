namespace Mocha.Rendering;

internal interface IRenderContext
{
	protected bool HasInitialized { get; set; }
	protected bool RenderingActive { get; set; }

	RenderStatus Startup();
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
}
