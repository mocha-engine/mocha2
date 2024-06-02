namespace Mocha;

[Resource]
public partial struct ModelData
{
	public Vertex[] Vertices { get; set; }
	public uint[]? Indices { get; set; }
	public string Material { get; private set; }
	public bool IsIndexed { get; set; }

	public ModelData( Vertex[] vertices, uint[] indices, string material )
	{
		Vertices = vertices;
		Indices = indices;
		Material = material;
		IsIndexed = true;
	}

	public ModelData( Vertex[] vertices, string material )
	{
		Vertices = vertices;
		Material = material;
		IsIndexed = false;
	}
}
