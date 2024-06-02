namespace Mocha;

public interface IWindow
{
	global::Silk.NET.Core.Contexts.IVkSurface? VkSurface { get; }
	global::Silk.NET.Maths.Vector2D<int>? FramebufferSize { get; }

	event Action? OnResize;

	void OnRendererInit();
}
