using System.Runtime.InteropServices;

namespace Mocha.SceneSystem;

[StructLayout( LayoutKind.Sequential )]
public record struct Transform
{
	private Vector4 _positionScale;
	private Rotation _rotation;

	public Vector3 Position
	{
		get => new Vector3( _positionScale.X, _positionScale.Y, _positionScale.Z );
		set => _positionScale = new Vector4( value.X, value.Y, value.Z, _positionScale.W );
	}

	public float Scale
	{
		get => _positionScale.W;
		set => _positionScale.W = value;
	}

	public Rotation Rotation
	{
		get => _rotation;
		set => _rotation = value;
	}

	public static Transform Zero = new Transform()
	{
		Position = Vector3.Zero,
		Scale = 1.0f,
		Rotation = Rotation.Identity
	};
}
