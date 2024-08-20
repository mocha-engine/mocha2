namespace Mocha;

[Resource]
public partial struct MeshData
{
	public Vertex[] Vertices { get; set; }
	public uint[]? Indices { get; set; }
	public string Material { get; private set; }
	public bool IsIndexed { get; set; }

	public MeshData( Vertex[] vertices, uint[] indices, string material )
	{
		Vertices = vertices;
		Indices = indices;
		Material = material;
		IsIndexed = true;
	}

	public MeshData( Vertex[] vertices, string material )
	{
		Vertices = vertices;
		Material = material;
		IsIndexed = false;
	}

	public MeshData() { }
}
