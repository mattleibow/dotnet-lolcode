using System;

using var library = new InteropSamples.LolcatExports();
Console.WriteLine(library.WELCOME("DOTNET", 3));
Console.WriteLine(library.MEOWLEN());
Console.WriteLine(library.GREETING());
