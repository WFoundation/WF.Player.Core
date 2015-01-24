using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
#if __IOS__
	using ObjCRuntime;
#endif

// Allgemeine Informationen über eine Assembly werden über die folgenden 
// Attribute gesteuert. Ändern Sie diese Attributwerte, um die Informationen zu ändern,
// die mit einer Assembly verknüpft sind.
[assembly: AssemblyTitle("WF.Player.Core")]
[assembly: AssemblyDescription("Core of Wherigo players")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("Wherigo Foundation")]
[assembly: AssemblyProduct("Wherigo")]
[assembly: AssemblyCopyright("Wherigo Foundation, 2012-2014")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

#if __IOS__
	// Attributes only valid for iOS builds
	[assembly: CLSCompliantAttribute (false)]
	[assembly: LinkWith("liblua5.1.a", LinkTarget.Simulator | LinkTarget.ArmV6 | LinkTarget.ArmV7 | LinkTarget.ArmV7s, Frameworks = "Foundation", ForceLoad = true, IsCxx = true, LinkerFlags = "-lstdc++")]
#endif

// Durch Festlegen von ComVisible auf "false" werden die Typen in dieser Assembly unsichtbar 
// für COM-Komponenten. Wenn Sie auf einen Typ in dieser Assembly von 
// COM zugreifen müssen, legen Sie das ComVisible-Attribut für diesen Typ auf "true" fest.
[assembly: ComVisible(false)]

// Die folgende GUID bestimmt die ID der Typbibliothek, wenn dieses Projekt für COM verfügbar gemacht wird
[assembly: Guid("7db488db-56b5-4529-99eb-d66b1778ddab")]

// Versionsinformationen für eine Assembly bestehen aus den folgenden vier Werten:
//
//      Hauptversion
//      Nebenversion 
//      Buildnummer
//      Revision
//
[assembly: AssemblyVersion("0.3.0.0")]
[assembly: AssemblyFileVersion("0.3.0.0")]
