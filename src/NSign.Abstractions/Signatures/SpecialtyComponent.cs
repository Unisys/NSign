using System;

namespace NSign.Signatures
{
    /// <summary>
    /// Represents a signature component based on a specialty field as per the standard.
    /// </summary>
    public class SpecialtyComponent : SignatureComponent
    {
        /// <summary>
        /// Initializes a new instance of SpecialtyComponent.
        /// </summary>
        /// <param name="name">
        /// The name of the specialty component.
        /// </param>
        public SpecialtyComponent(string name) : base(SignatureComponentType.Specialty, ValidateNameOrThrow(name)) { }

        /// <inheritdoc/>
        public override void Accept(ISignatureComponentVisitor visitor)
        {
            visitor.Visit(this);
        }

        /// <summary>
        /// Validates the given component name and throws if it doesn't follow the standard format.
        /// </summary>
        /// <param name="name">
        /// The name to validate.
        /// </param>
        /// <returns>
        /// The validated name.
        /// </returns>
        /// <remarks>
        /// This does NOT validate that the name is supported, in order to support extensions.
        /// </remarks>
        private static string ValidateNameOrThrow(string name)
        {
            if (!String.IsNullOrEmpty(name) && name[0] != '@')
            {
                throw new ArgumentOutOfRangeException(nameof(name));
            }

            return name;
        }
    }
}
