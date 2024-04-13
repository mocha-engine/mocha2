using Mocha;
using Mocha.SceneSystem;

using ( var app = new MochaApplication( "Mocha Example" ) )
{
	//
	// Set properties
	//
	app.Icon = "icon.png";

	//
	// Callbacks
	//
	app.PreBootstrap = () =>
	{
	};

	app.Bootstrap = () =>
	{
		//
		// Scene setup
		//
		var texture = new Texture( "/test.texture" );

		//var suzanne = new SceneModel( Scene.Main, "suzanne.model" ) with
		//{
		//	Position = new Vector3( 10, 10, 10 )
		//};
	};
}
