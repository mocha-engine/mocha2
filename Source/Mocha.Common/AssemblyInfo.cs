using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// In SDK-style projects such as this one, several assembly attributes that were historically
// defined in this file are now automatically added during build and populated with
// values defined in project properties. For details of which attributes are included
// and how to customise this process see: https://aka.ms/assembly-info-properties


// Setting ComVisible to false makes the types in this assembly not visible to COM
// components.  If you need to access a type in this assembly from COM, set the ComVisible
// attribute to true on that type.

[assembly: ComVisible( false )]

// The following GUID is for the ID of the typelib if this project is exposed to COM.

[assembly: Guid( "b162587c-2030-43f3-ae89-1160c8a692ae" )]

[assembly: InternalsVisibleTo( "Apparatus.Core.AppFramework" )]
[assembly: InternalsVisibleTo( "Apparatus.Core.Genreators" )]
[assembly: InternalsVisibleTo( "Apparatus.Core.Rendering" )]
[assembly: InternalsVisibleTo( "Mocha.ResourceCompiler" )]
[assembly: InternalsVisibleTo( "Mocha" )]
