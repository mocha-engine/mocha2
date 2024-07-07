namespace Mocha.Rendering;

public enum RenderStatus
{
	NotInitialized = -9,
	AlreadyInitialized = -8,
	BeginEndMismatch = -7,
	NoPipelineBound = -6,
	NoVertexBufferBound = -5,
	NoIndexBufferBound = -4,
	InvalidHandle = -3,
	ShaderCompileFailed = -2,
	WindowSizeInvalid = -1,
	Ok = 0,
	WindowMinimized = 1,
}
