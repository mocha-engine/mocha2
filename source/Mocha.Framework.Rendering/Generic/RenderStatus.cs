namespace Mocha.Rendering;

public enum RenderStatus
{
	Ok,
	NotInitialized,
	AlreadyInitialized,
	BeginEndMismatch,
	NoPipelineBound,
	NoVertexBufferBound,
	NoIndexBufferBound,
	InvalidHandle,
	ShaderCompileFailed,
	WindowSizeInvalid
}
