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
	MacOs,
	Other
}

public enum ArchitectureTypes
{
	X86,
	X64,
	Arm,
	Arm64,
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
			return OperatingSystems.MacOs;

		return OperatingSystems.Other;
	}

	public static ArchitectureTypes GetArchitecture()
	{
		if ( RuntimeInformation.ProcessArchitecture == System.Runtime.InteropServices.Architecture.X86 )
			return ArchitectureTypes.X86;
		else if ( RuntimeInformation.ProcessArchitecture == System.Runtime.InteropServices.Architecture.X64 )
			return ArchitectureTypes.X64;
		else if ( RuntimeInformation.ProcessArchitecture == System.Runtime.InteropServices.Architecture.Arm )
			return ArchitectureTypes.Arm;
		else if ( RuntimeInformation.ProcessArchitecture == System.Runtime.InteropServices.Architecture.Arm64 )
			return ArchitectureTypes.Arm64;

		return ArchitectureTypes.Other;
	}

	public static CurrentPlatformInfo Current()
	{
		return new CurrentPlatformInfo( GetRenderer(), GetOperatingSystem(), GetArchitecture() );
	}
}
