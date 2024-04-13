using Mocha.Formats;

namespace Mocha.SceneSystem;

public sealed record class SceneModel : SceneObject
{
	public SceneModel( Scene parent, string modelPath ) : base( parent )
	{
		var model = ModelData.Load( modelPath );
	}

	public SceneModel( Scene parent, string modelPath, Transform transform ) : this( parent, modelPath )
	{
		Transform = transform;
	}
}
