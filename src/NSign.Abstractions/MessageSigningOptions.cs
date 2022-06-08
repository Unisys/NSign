using NSign.Signatures;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;

namespace NSign
{
    /// <summary>
    /// Options class to control signature creation on HTTP messages.
    /// </summary>
    public class MessageSigningOptions
    {
        /// <summary>
        /// Gets or sets the name of the signature to add.
        /// </summary>
        /// <remarks>
        /// Valid names are per section 3.1.2 of RFC 8941.
        /// See also <see href="https://httpwg.org/specs/rfc8941.html#rfc.section.3.1.2"/>.
        /// </remarks>
        [RegularExpression(@"^[a-z*][a-z0-9_\-\.*]*$")]
        public string? SignatureName { get; set; }

        /// <summary>
        /// Gets an ICollection of <see cref="ComponentSpec"/> objects defining the components that should be included
        /// in the signature.
        /// </summary>
        public ICollection<ComponentSpec> ComponentsToInclude { get; } = new Collection<ComponentSpec>();

        /// <summary>
        /// Gets or sets an Action of <see cref="SignatureParamsComponent"/> that is used to set additional parameters
        /// of signature parameters before signing, if set. This action gets called <em>before</em>
        /// <see cref="ISigner.UpdateSignatureParams(SignatureParamsComponent)"/>.
        /// </summary>
        public Action<SignatureParamsComponent>? SetParameters { get; set; }

        /// <summary>
        /// Gets or sets a flag which indicates whether or not the <see cref="ISigner.UpdateSignatureParams(SignatureParamsComponent)"/>
        /// method should be called on the signer. Defaults to <c>true</c>.
        /// </summary>
        public bool UseUpdateSignatureParams { get; set; } = true;

        /// <summary>
        /// Adds the given component as a mandatory component for signature creation. If the component is missing on a
        /// message to sign, signing the message <b>will fail</b> and so will the request/response pipeline.
        /// </summary>
        /// <param name="component">
        /// The <see cref="SignatureComponent"/> object to add as a mandatory component.
        /// </param>
        /// <returns>
        /// The <see cref="MessageSigningOptions"/> instance.
        /// </returns>
        public MessageSigningOptions WithMandatoryComponent(SignatureComponent component)
        {
            ComponentsToInclude.Add(new ComponentSpec(component, mandatory: true));

            return this;
        }

        /// <summary>
        /// Adds the given component as an optional component for signature creation. If the component is missing on a
        /// message to sign, signing the message will <b>not</b> fail.
        /// </summary>
        /// <param name="component">
        /// The <see cref="SignatureComponent"/> object to add as an optional component.
        /// </param>
        /// <returns>
        /// The <see cref="MessageSigningOptions"/> instance.
        /// </returns>
        public MessageSigningOptions WithOptionalComponent(SignatureComponent component)
        {
            ComponentsToInclude.Add(new ComponentSpec(component, mandatory: false));

            return this;
        }

        /// <summary>
        /// Read-struct to track signature components and whether or not they are mandatory.
        /// </summary>
        public readonly struct ComponentSpec
        {
            /// <summary>
            /// Initializes a new instance of ComponentSpec.
            /// </summary>
            /// <param name="component">
            /// The <see cref="SignatureComponent"/> object to track.
            /// </param>
            /// <param name="mandatory">
            /// A flag which indicates whether or not the component is mandatory.
            /// </param>
            public ComponentSpec(SignatureComponent component, bool mandatory)
            {
                if (component is SignatureParamsComponent)
                {
                    throw new NotSupportedException(
                        "A SignatureParamsComponent cannot be added explicitly; it is always added automatically.");
                }

                Component = component;
                Mandatory = mandatory;
            }

            /// <summary>
            /// Gets the tracked SignatureComponent object.
            /// </summary>
            public SignatureComponent Component { get; }

            /// <summary>
            /// Gets a flag indicating whether or not the component is mandatory.
            /// </summary>
            public bool Mandatory { get; }
        }
    }
}
