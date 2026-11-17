// C# record/init metadata marker absent from the Mono netstandard2.1 reference API.
// Modern .NET supplies its own definition.
#if NETSTANDARD2_1
namespace System.Runtime.CompilerServices
{
	internal static class IsExternalInit { }
}
#endif
