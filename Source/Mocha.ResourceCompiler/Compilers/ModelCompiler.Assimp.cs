using Assimp;

namespace Mocha.ResourceCompiler;

partial class ModelCompiler
{
	private class AssimpProcessor
	{
		private List<MeshData> OutMeshes = new();
		private ModelAsset InModel = null!;

		public static MeshData[] Process( ModelAsset model )
		{
			var p = new AssimpProcessor();
			p.InModel = model;
			p.GenerateMochaModel();
			return p.OutMeshes.ToArray();
		}

		// todo: This is all running sync when we can totally make use of async now
		public List<MeshData> GenerateMochaModel()
		{
			var models = new List<MeshData>();
			var logStream = new LogStream( ( msg, _ ) => Console.WriteLine( msg ) );

			if ( InModel.Meshes != null )
			{
				foreach ( var mesh in InModel.Meshes )
				{
					GenerateMochaMesh( mesh ).Wait();
				}
			}

			return models;
		}

		private async Task GenerateMochaMesh( ModelAsset.Mesh mesh )
		{
			var fileHandle = FileSystem.ContentSrc.GetHandle( mesh.Path );
			var sourceData = await fileHandle.ReadDataAsync();
			using var memoryStream = new MemoryStream( sourceData );

			var assimpContext = new AssimpContext();
			var scene = assimpContext.ImportFileFromStream( memoryStream,
				PostProcessSteps.Triangulate
				| PostProcessSteps.RemoveRedundantMaterials
				| PostProcessSteps.CalculateTangentSpace
				| PostProcessSteps.OptimizeMeshes
				| PostProcessSteps.OptimizeGraph
				| PostProcessSteps.ValidateDataStructure
				| PostProcessSteps.GenerateNormals
				| PostProcessSteps.FlipWindingOrder
				| PostProcessSteps.FlipUVs, 
				Path.GetExtension( mesh.Path ) 
			);

			await ProcessAssimpNode( scene.RootNode, scene );
		}

		private async Task ProcessAssimpNode( Node node, Scene scene )
		{
			for ( int i = 0; i < node.MeshCount; ++i )
			{
				var mesh = scene.Meshes[node.MeshIndices[i]];
				await Task.Run( () => ProcessAssimpMesh( mesh, scene, node.Transform ) );
			}

			foreach ( var child in node.Children )
			{
				await ProcessAssimpNode( child, scene );
			}
		}

		private void ProcessAssimpMesh( Mesh mesh, Scene scene, Matrix4x4 transform )
		{
			List<Vertex> vertices = new List<Vertex>();
			List<uint> indices = new List<uint>();

			for ( int i = 0; i < mesh.VertexCount; ++i )
			{
				var vertex = new Vertex()
				{
					Position = new Vector3( mesh.Vertices[i].X, mesh.Vertices[i].Y, mesh.Vertices[i].Z ),
					Normal = new Vector3( mesh.Normals[i].X, mesh.Normals[i].Y, mesh.Normals[i].Z )
				};

				if ( mesh.HasTextureCoords( 0 ) )
				{
					var texCoords = new Vector2( mesh.TextureCoordinateChannels[0][i].X, mesh.TextureCoordinateChannels[0][i].Y );
					vertex.TexCoord = texCoords;
				}
				else
				{
					vertex.TexCoord = new Vector2( 0, 0 );
				}

				if ( mesh.HasTangentBasis )
				{
					vertex.Tangent = new Vector3( mesh.Tangents[i].X, mesh.Tangents[i].Y, mesh.Tangents[i].Z );
					vertex.Bitangent = new Vector3( mesh.BiTangents[i].X, mesh.BiTangents[i].Y, mesh.BiTangents[i].Z );
				}

				vertices.Add( vertex );
			}

			for ( int i = 0; i < mesh.FaceCount; ++i )
			{
				var face = mesh.Faces[i];
				for ( int f = 0; f < face.IndexCount; ++f )
				{
					indices.Add( (uint)face.Indices[f] );
				}
			}

			var materialPath = "internal:missing";
			var materials = InModel.Materials;

			if ( materials is not null )
			{
				if ( mesh.MaterialIndex >= 0 && mesh.MaterialIndex < materials.Count )
					materialPath = materials[mesh.MaterialIndex].Path;
			}

			OutMeshes.Add( new MeshData( [.. vertices], [.. indices], materialPath ) );
		}
	}
}
