// Helper class to allow compiling C# 9.0 for a NetStandard 2.1 target.
// See also
//   https://developercommunity.visualstudio.com/t/error-cs0518-predefined-type-systemruntimecompiler/1244809#TPIN-N1249582
namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit { }
}