namespace Mocha.Rendering;

internal enum RenderStatus
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
