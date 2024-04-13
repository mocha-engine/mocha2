namespace Mocha.SceneSystem;

public record class SceneObject
{
	public Guid Ident { get; init; }

	public Transform Transform { get; set; }
	public string? Name { get; set; }

	public Vector3 Position
	{
		get => Transform.Position;
		set => Transform = Transform with { Position = value };
	}

	public float Scale
	{
		get => Transform.Scale;
		set => Transform = Transform with { Scale = value };
	}

	public Rotation Rotation
	{
		get => Transform.Rotation;
		set => Transform = Transform with { Rotation = value };
	}

	public SceneObject( Scene parent )
	{
		Transform = Transform.Zero;
		Name ??= $"SceneObject {Ident}";
		
		if ( !Debug.Assert( parent != null, "SceneObject parent was null. Marking as invalid." ) )
		{
			Ident = Guid.Empty;
			return;
		}

		Ident = parent!.CreateGuid();
	}

	public SceneObject( Scene parent, Transform transform ) : this( parent )
	{
		Transform = transform;
	}
}
