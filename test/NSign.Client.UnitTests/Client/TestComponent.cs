using NSign.Signatures;

namespace NSign.Client
{
    internal sealed class TestComponent : SignatureComponent
    {
        public TestComponent(SignatureComponentType type, string componentName) : base(type, componentName)
        {
        }
    }
}
