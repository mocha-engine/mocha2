global using Mocha;

global using System;
global using System.Collections.Generic;
global using System.IO;
global using System.Linq;
global using System.Threading;
global using System.Threading.Tasks;

global using static Globals;

global using Apparatus.Core.Common;

public static class Globals
{
	public static Logger Log { get; } = new();
}
