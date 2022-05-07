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
    /// Represents a group of expressions evaluated with a specified boolean operator
    /// </summary>
    public class ExpressionGroup
    {
        /// <summary>
        /// List of expressions in the group
        /// </summary>
        [JsonProperty]
        public List<Expression> Expressions { get; private set; }

        /// <summary>
        /// Sub-groups of this group
        /// </summary>
        [JsonProperty]
        public List<ExpressionGroup> SubGroups { get; private set; }

        /// <summary>
        /// The type of boolean operator to apply between expressions in this group
        /// </summary>
        [JsonProperty]
        [JsonConverter(typeof(StringEnumConverter))]
        public ExpressionGroupType GroupType { get; private set; }

        private ExpressionGroup(ExpressionGroupType groupType)
        {
            Expressions = new List<Expression>();
            SubGroups = new List<ExpressionGroup>();
            GroupType = groupType;
        }

        private static readonly string[] GroupOperators = { "lar:Not", "lar:Or", "lar:And" };

        internal static bool IsGroupElement(XPathNavigator ruleMetadataNavigator) => GroupOperators.Contains(ruleMetadataNavigator.Name);

        private static readonly Dictionary<string, ExpressionGroupType> GroupNameToTypeMap = new()
        {
            { "lar:Not", ExpressionGroupType.Not },
            { "lar:Or", ExpressionGroupType.Or },
            { "lar:And", ExpressionGroupType.And },
        };

        internal static ExpressionGroup FromXml(XPathNavigator ruleMetadataNavigator, XmlNamespaceManager namespaceManager)
        {
            ExpressionGroup newGroup = new(GroupNameToTypeMap[ruleMetadataNavigator.Name]);

            var groupExpressions = ruleMetadataNavigator.SelectChildren(XPathNodeType.Element);
            while (groupExpressions.MoveNext())
            {
                if (GroupOperators.Contains(groupExpressions.Current.Name))
                {
                    newGroup.SubGroups.Add(FromXml(groupExpressions.Current, namespaceManager));
                }
                else
                {
                    newGroup.Expressions.Add(new Expression(groupExpressions.Current, namespaceManager));
                }
            }

            if ((newGroup.GroupType != ExpressionGroupType.And && newGroup.GroupType != ExpressionGroupType.Or)
                && (newGroup.Expressions.Count > 1 || newGroup.SubGroups.Count > 1))
            {
                throw new Exception("Logical operator not supported with multiple expressions or groups");
            }

            return newGroup;
        }
    }
}
