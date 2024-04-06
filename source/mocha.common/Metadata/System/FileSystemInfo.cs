namespace Mocha;

public class MountPathInfo
{
	public string Content { get; set; }
	public string Source { get; set; }
}

public class FileSystemInfo
{
	public string[] ExcludeDirs { get; set; }
	public MountPathInfo MountPaths { get; set; }
}
