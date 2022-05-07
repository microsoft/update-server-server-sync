// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.XPath;

namespace Microsoft.PackageGraph.MicrosoftUpdate.Metadata.Applicability
{
    /// <summary>
    /// Represents an individual expression that is part of a rule or rule group
    /// </summary>
    public class Expression
    {
        /// <summary>
        /// List expression attributes; attributes are key-value pairs
        /// </summary>
        [JsonProperty]
        public List<ExpressionToken> Attributes { get; private set; }

        /// <summary>
        /// List of sub-expressions of this expression
        /// </summary>
        [JsonProperty]
        public List<Expression> SubExpressions { get; private set; }

        /// <summary>
        /// List of sub-groups in this expression
        /// </summary>
        [JsonProperty]
        public List<ExpressionGroup> SubGroups { get; private set; }

        /// <summary>
        /// Expression type
        /// </summary>
        [JsonProperty]
        [JsonConverter(typeof(StringEnumConverter))]
        public ExpressionType ExpressionType { get; private set; }

        [JsonConstructor]
        private Expression()
        {

        }

        internal Expression(XPathNavigator expressionNavigator, XmlNamespaceManager namespaceManager)
        {
            if (!KnownExpressionDefinitions.NameToDefinitionMap.ContainsKey(expressionNavigator.Name))
            {
                throw new Exception("Unknown expression type: " + expressionNavigator.Name);
            }

            this.ExpressionType = KnownExpressionDefinitions.NameToTypeMap[expressionNavigator.Name];

            Attributes = new List<ExpressionToken>();

            XPathExpression attributesQuery = expressionNavigator.Compile("@*");
            attributesQuery.SetContext(namespaceManager);
            var attributesQueryResult = expressionNavigator.Evaluate(attributesQuery) as XPathNodeIterator;

            var tokens = KnownExpressionDefinitions.NameToDefinitionMap[expressionNavigator.Name];
            while (attributesQueryResult.MoveNext())
            {
                var matchingToken = tokens.FirstOrDefault(t => t.Key.Equals(attributesQueryResult.Current.Name));
                if (matchingToken == null)
                {
                    throw new Exception("Unknown attribute found: " + attributesQueryResult.Current.Name);
                }

                Attributes.Add(
                    new ExpressionToken(
                        matchingToken.Key, attributesQueryResult.Current.Value, matchingToken.TargetType));
            }

            bool innerXmlCaptured = false;
            if (tokens.Any(t => t.Key.Equals("*")))
            {
                var valueToken = tokens.First(t => t.Key.Equals("*"));
                Attributes.Add(
                    new ExpressionToken(valueToken.Key, expressionNavigator.Value, valueToken.TargetType));
            }
            else if (tokens.Any(t => t.Key.Equals("+")))
            {
                var valueToken = tokens.First(t => t.Key.Equals("+"));
                Attributes.Add(
                    new ExpressionToken(valueToken.Key, expressionNavigator.InnerXml, valueToken.TargetType));
                innerXmlCaptured = true;
            }

            if (tokens.Any(t => t.Requirement == TokenRequirements.Required && !Attributes.Any(a => a.Name == t.Key)))
            {
                throw new Exception("Required attribute not found");
            }

            SubGroups = new List<ExpressionGroup>();
            SubExpressions = new List<Expression>();

            if (!innerXmlCaptured)
            {
                var subExpressionsNavigator = expressionNavigator.SelectChildren(XPathNodeType.Element);

                while (subExpressionsNavigator.MoveNext())
                {
                    if (ExpressionGroup.IsGroupElement(subExpressionsNavigator.Current))
                    {
                        SubGroups.Add(ExpressionGroup.FromXml(subExpressionsNavigator.Current, namespaceManager));
                    }
                    else
                    {
                        SubExpressions.Add(new Expression(subExpressionsNavigator.Current, namespaceManager));
                    }
                }
            }
        }
    }
}
