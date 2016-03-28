using System.Reflection;
using System.Runtime.InteropServices;

// General Information about an assembly is controlled through the following
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyCompany("Syncromatics Corporation")]
[assembly: AssemblyCopyright("Copyright © Syncromatics Corporation 2016")]
// Setting ComVisible to false makes the types in this assembly not visible
// to COM components.  If you need to access a type in this assembly from
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version
//      Build Number
//      Revision
//
// The AssemblyVersion is updated as part of the CI process via a rake script
[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]
[assembly: AssemblyDescription("")]

#if DEBUG
[assembly: AssemblyConfiguration("Debug")]// a.k.a. "Comments"
#else
[assembly: AssemblyConfiguration("Release")]// a.k.a. "Comments"
#endif