using System.Runtime.InteropServices;

namespace Mocha;

public enum Renderers
{
	Vulkan,
	Null
};

public enum OperatingSystems
{
	Windows,
	Linux,
	MacOS,
	Other
}

public enum ArchitectureTypes
{
	x86,
	x64,
	ARM,
	ARM64,
	Other
}

public record struct CurrentPlatformInfo( Renderers Renderer, OperatingSystems OperatingSystem, ArchitectureTypes Architecture )
{
	public static Renderers GetRenderer()
	{
		// TODO: Fetch best rendering backend for OS / architecture combo
		return Renderers.Vulkan;
	}

	public static OperatingSystems GetOperatingSystem()
	{
		if ( RuntimeInformation.IsOSPlatform( OSPlatform.Windows ) )
			return OperatingSystems.Windows;
		else if ( RuntimeInformation.IsOSPlatform( OSPlatform.Linux ) )
			return OperatingSystems.Linux;
		else if ( RuntimeInformation.IsOSPlatform( OSPlatform.OSX ) )
			return OperatingSystems.MacOS;

		return OperatingSystems.Other;
	}

	public static ArchitectureTypes GetArchitecture()
	{
		if ( RuntimeInformation.ProcessArchitecture == System.Runtime.InteropServices.Architecture.X86 )
			return ArchitectureTypes.x86;
		else if ( RuntimeInformation.ProcessArchitecture == System.Runtime.InteropServices.Architecture.X64 )
			return ArchitectureTypes.x64;
		else if ( RuntimeInformation.ProcessArchitecture == System.Runtime.InteropServices.Architecture.Arm )
			return ArchitectureTypes.ARM;
		else if ( RuntimeInformation.ProcessArchitecture == System.Runtime.InteropServices.Architecture.Arm64 )
			return ArchitectureTypes.ARM64;

		return ArchitectureTypes.Other;
	}

	public static CurrentPlatformInfo Current()
	{
		return new CurrentPlatformInfo( GetRenderer(), GetOperatingSystem(), GetArchitecture() );
	}
}
