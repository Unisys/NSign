using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace NSign.Signatures
{
    /// <summary>
    /// Ref struct that helps parse signature input specs and values of the '@signature-params' component.
    /// </summary>
    internal ref partial struct SignatureInputParser
    {
        /// <summary>
        /// The SignatureParamsComponent object to update with the results from the parser.
        /// </summary>
        private readonly SignatureParamsComponent signatureParams;

        /// <summary>
        /// Initializes a new instance of SignatureInputParser.
        /// </summary>
        /// <param name="signatureParams">
        /// The SignatureParamsComponent object to update with the results from the parser.
        /// </param>
        private SignatureInputParser(SignatureParamsComponent signatureParams)
        {
            Debug.Assert(null != signatureParams, "The signature params must not be null.");

            this.signatureParams = signatureParams;
        }

        /// <summary>
        /// Parses the given input string and updates the specified SignatureParamsComponent object with the results.
        /// </summary>
        /// <param name="input">
        /// The input string to parse.
        /// </param>
        /// <param name="signatureParams">
        /// The SignatureParamsComponent object to update with the results from the parser.
        /// </param>
        public static void ParseAndUpdate(string input, SignatureParamsComponent signatureParams)
        {
            if (String.IsNullOrWhiteSpace(input))
            {
                throw new ArgumentNullException(nameof(input));
            }
            if (null == signatureParams)
            {
                throw new ArgumentNullException(nameof(signatureParams));
            }

            Tokenizer tokenizer = new Tokenizer(input);
            new SignatureInputParser(signatureParams).ParseAndUpdate(ref tokenizer);
        }

        #region Parsing

        /// <summary>
        /// Starts parsing the input and updating the signatureParams object.
        /// </summary>
        /// <param name="tokenizer">
        /// A reference to the Tokenizer on the current input.
        /// </param>
        private void ParseAndUpdate(ref Tokenizer tokenizer)
        {
            // We expect the component list first ...
            ParseAndUpdateComponentList(ref tokenizer);
            Debug.Assert(TokenType.CloseParenthesis == tokenizer.Token.Type, "The current token must be a closing parenthesis.");

            // ... followed by the parameters.
            ParseAndUpdateParameters(ref tokenizer);
        }

        /// <summary>
        /// Parses the component list from the input and updates the signatureParams object.
        /// </summary>
        /// <param name="tokenizer">
        /// A reference to the Tokenizer on the current input.
        /// </param>
        private void ParseAndUpdateComponentList(ref Tokenizer tokenizer)
        {
            // The component list must start with an open parenthesis.
            EnsureNextToken(ref tokenizer, TokenType.OpenParenthesis);

            // The next token must either be the close parenthesis token yielding an empty component list, or a quoted string.
            EnsureNextToken(ref tokenizer, TokenType.CloseParenthesis | TokenType.QuotedString);

            while (TokenType.CloseParenthesis != tokenizer.Token.Type)
            {
                ParseAndUpdateComponent(ref tokenizer);
            }
        }

        /// <summary>
        /// Parses and updates a single component in the component list from the input and updates the signatureParams
        /// object accordingly.
        /// </summary>
        /// <param name="tokenizer">
        /// A reference to the Tokenizer on the current input.
        /// </param>
        private void ParseAndUpdateComponent(ref Tokenizer tokenizer)
        {
            if (signatureParams.Components.Count > 0)
            {
                // We've already verified that we have a whitespace at the end of the previous component parsing.
                // After the whitespace we must get another quoted string for the component name.
                EnsureNextToken(ref tokenizer, TokenType.QuotedString);
            }
            else
            {
                tokenizer.EnsureTokenOneOfOrThrow(TokenType.QuotedString);
            }

            ReadOnlySpan<char> componentNameSpan = tokenizer.Token.Value;
            if (componentNameSpan.Length <= 0)
            {
                throw new SignatureInputException("Component names must not be empty.");
            }

            // We now need either a close parenthesis token (to indicate the end of the component list), a semicolon
            // (to indicate a parameter to a component) or a whitespace (to indicate more components to follow).
            EnsureNextToken(ref tokenizer, TokenType.CloseParenthesis | TokenType.Semicolon | TokenType.Whitespace);

            string componentName = new String(componentNameSpan);
            KeyValuePair<string, string>? componentParam = ParseOptionalComponentParameter(ref tokenizer);

            if (componentNameSpan[0] == '@')
            {
                AddDerivedComponent(componentName, componentParam);
            }
            else
            {
                AddHttpHeaderComponent(componentName, componentParam);
            }
        }

        /// <summary>
        /// Parses the optional parameter to a component in the component list.
        /// </summary>
        /// <param name="tokenizer">
        /// A reference to the Tokenizer on the current input.
        /// </param>
        /// <returns>
        /// A KeyValuePair of string and string representing parameter name and value if a parameter is present or null
        /// if no parameter is present.
        /// </returns>
        private KeyValuePair<string, string>? ParseOptionalComponentParameter(ref Tokenizer tokenizer)
        {
            if (TokenType.Semicolon != tokenizer.Token.Type)
            {
                return null;
            }

            // The semicolon must be followed by an identifier representing the name of the parameter.
            EnsureNextToken(ref tokenizer, TokenType.Identifier);
            string name = new String(tokenizer.Token.Value);

            // The parameter name must be followed by the equal sign ...
            EnsureNextToken(ref tokenizer, TokenType.Equal);

            // ... which in turn must be followed by a quoted string representing the parameter value.
            EnsureNextToken(ref tokenizer, TokenType.QuotedString);
            string value = new String(tokenizer.Token.Value);

            // We need to move to the next token because that's expected by the caller and it's necessary to then allow
            // parsing the next component in the list or the end of the list.
            EnsureNextToken(ref tokenizer, TokenType.CloseParenthesis | TokenType.Whitespace);

            return new KeyValuePair<string, string>(name, value);
        }

        /// <summary>
        /// Adds the given derived component to the component list of the signatureParams.
        /// </summary>
        /// <param name="componentName">
        /// The name of the component to add.
        /// </param>
        /// <param name="componentParam">
        /// The optional parameter of the component to add.
        /// </param>
        private void AddDerivedComponent(string componentName, KeyValuePair<string, string>? componentParam)
        {
            switch (componentName)
            {
                case Constants.DerivedComponents.QueryParams:
                    if (!componentParam.HasValue ||
                        !StringComparer.Ordinal.Equals(Constants.ComponentParameters.Name, componentParam.Value.Key))
                    {
                        throw new SignatureInputException("The @query-params component requires the 'name' parameter.");
                    }
                    signatureParams.AddComponent(new QueryParamsComponent(componentParam.Value.Value));
                    break;

                case Constants.DerivedComponents.RequestResponse:
                    if (!componentParam.HasValue ||
                        !StringComparer.Ordinal.Equals(Constants.ComponentParameters.Key, componentParam.Value.Key))
                    {
                        throw new SignatureInputException("The @request-response component requires the 'key' parameter.");
                    }
                    signatureParams.AddComponent(new RequestResponseComponent(componentParam.Value.Value));
                    break;

                // Handle known cases without parameters too, so we can reuse existing components rather than creating more.
                case Constants.DerivedComponents.Authority:
                    signatureParams.AddComponent(SignatureComponent.Authority);
                    break;
                case Constants.DerivedComponents.Method:
                    signatureParams.AddComponent(SignatureComponent.Method);
                    break;
                case Constants.DerivedComponents.Path:
                    signatureParams.AddComponent(SignatureComponent.Path);
                    break;
                case Constants.DerivedComponents.Query:
                    signatureParams.AddComponent(SignatureComponent.Query);
                    break;
                case Constants.DerivedComponents.RequestTarget:
                    signatureParams.AddComponent(SignatureComponent.RequestTarget);
                    break;
                case Constants.DerivedComponents.Scheme:
                    signatureParams.AddComponent(SignatureComponent.Scheme);
                    break;
                case Constants.DerivedComponents.Status:
                    signatureParams.AddComponent(SignatureComponent.Status);
                    break;
                case Constants.DerivedComponents.TargetUri:
                    signatureParams.AddComponent(SignatureComponent.RequestTargetUri);
                    break;

                case Constants.DerivedComponents.SignatureParams:
                    throw new SignatureInputException("The @signature-params component is not allowed.");

                default:
                    signatureParams.AddComponent(new DerivedComponent(componentName));
                    break;
            }
        }

        /// <summary>
        /// Adds the given HTTP message header component to the component list of the signatureParams.
        /// </summary>
        /// <param name="componentName">
        /// The name of the HTTP message header component to add.
        /// </param>
        /// <param name="componentParam">
        /// The optional parameter of the component to add.
        /// </param>
        private void AddHttpHeaderComponent(string componentName, KeyValuePair<string, string>? componentParam)
        {
            if (componentParam.HasValue)
            {
                if (!StringComparer.Ordinal.Equals(Constants.ComponentParameters.Key, componentParam.Value.Key))
                {
                    throw new SignatureInputException(
                        "Dictionary structured HTTP message header components require the 'key' parameter.");
                }
                else
                {
                    signatureParams.AddComponent(new HttpHeaderDictionaryStructuredComponent(componentName, componentParam.Value.Value));
                }
            }
            else
            {
                signatureParams.AddComponent(new HttpHeaderComponent(componentName));
            }
        }

        /// <summary>
        /// Parses and updates the signature input's parameters and updates the signatureParams accordingly.
        /// </summary>
        /// <param name="tokenizer">
        /// A reference to the Tokenizer on the current input.
        /// </param>
        private void ParseAndUpdateParameters(ref Tokenizer tokenizer)
        {
            while (tokenizer.Next())
            {
                tokenizer.EnsureTokenOneOfOrThrow(TokenType.Semicolon);

                ParseAndUpdateParameter(ref tokenizer);
            }
        }

        /// <summary>
        /// Parses and updates a single signature input parameter and udpates the signatureParams accordingly.
        /// </summary>
        /// <param name="tokenizer">
        /// A reference to the Tokenizer on the current input.
        /// </param>
        private void ParseAndUpdateParameter(ref Tokenizer tokenizer)
        {
            // The parameter name must be an identifier ...
            EnsureNextToken(ref tokenizer, TokenType.Identifier);
            string paramName = new String(tokenizer.Token.Value);

            // ... and must be followed by the equal sign and a quoted string or integer token.
            EnsureNextToken(ref tokenizer, TokenType.Equal);
            EnsureNextToken(ref tokenizer, TokenType.QuotedString | TokenType.Integer);

            switch (paramName)
            {
                case Constants.SignatureParams.Created:
                    tokenizer.EnsureTokenOneOfOrThrow(TokenType.Integer);
                    signatureParams.Created = DateTimeOffset.FromUnixTimeSeconds(Int64.Parse(tokenizer.Token.Value));
                    break;

                case Constants.SignatureParams.Expires:
                    tokenizer.EnsureTokenOneOfOrThrow(TokenType.Integer);
                    signatureParams.Expires = DateTimeOffset.FromUnixTimeSeconds(Int64.Parse(tokenizer.Token.Value));
                    break;

                case Constants.SignatureParams.Nonce:
                    tokenizer.EnsureTokenOneOfOrThrow(TokenType.QuotedString);
                    signatureParams.Nonce = new String(tokenizer.Token.Value);
                    break;

                case Constants.SignatureParams.Alg:
                    tokenizer.EnsureTokenOneOfOrThrow(TokenType.QuotedString);
                    signatureParams.Algorithm = new String(tokenizer.Token.Value);
                    break;

                case Constants.SignatureParams.KeyId:
                    tokenizer.EnsureTokenOneOfOrThrow(TokenType.QuotedString);
                    signatureParams.KeyId = new String(tokenizer.Token.Value);
                    break;

                default:
                    throw new SignatureInputException(
                        $"Unsupported signature input parameter: {paramName} with value '{new String(tokenizer.Token.Value)}'.");
            }
        }

        #endregion

        /// <summary>
        /// Ensures that there is a next token and that it is one of the expectedTypes.
        /// </summary>
        /// <param name="tokenizer">
        /// A reference to the Tokenizer on the current input.
        /// </param>
        /// <param name="expectedTypes">
        /// A TokenType value that defines the expected / allowed token types.
        /// </param>
        /// <exception cref="SignatureInputParserException">
        /// A SignatureInputParserException is thrown if the tokenizer cannot present any tokens anymore or if it presents
        /// an unexpected token.
        /// </exception>
        private static void EnsureNextToken(ref Tokenizer tokenizer, TokenType expectedTypes)
        {
            if (!tokenizer.Next() || !tokenizer.Token.IsTypeOneOf(expectedTypes))
            {
                throw new SignatureInputParserException(expectedTypes, tokenizer);
            }
        }
    }
}
