namespace Mocha.SceneSystem;

public class Scene
{
	public static Scene Main { get; private set; }
	public List<SceneObject> SceneObjects { get; init; } = new();

	internal Scene()
	{
		Main ??= this;
	}

	internal Guid CreateGuid()
	{
		return Guid.NewGuid();
	}
}
