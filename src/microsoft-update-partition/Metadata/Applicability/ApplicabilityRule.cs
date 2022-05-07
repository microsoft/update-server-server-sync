// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.XPath;

namespace Microsoft.PackageGraph.MicrosoftUpdate.Metadata.Applicability
{
    /// <summary>
    /// Describes an applicability expression that gets evaluate by a Microsoft Update agent on a device during an update operation
    /// </summary>
    public class ApplicabilityRule
    {
        /// <summary>
        /// The type of applicability rule
        /// </summary>
        [JsonProperty]
        [JsonConverter(typeof(StringEnumConverter))]
        public ApplicabilityRuleType RuleType { get; private set; }

        /// <summary>
        /// List of expression groups. Rules are joined together by boolean operators in a group; a rule can have multiple groups.
        /// </summary>
        [JsonProperty]
        public List<ExpressionGroup> ExpressionGroups { get; private set; }

        /// <summary>
        /// A single expression to evaluate.
        /// </summary>
        [JsonProperty]
        public Expression Expression { get; private set; }

        /// <summary>
        /// Set to true if this rule only stores metadata for other rules
        /// </summary>
        [JsonProperty]
        public bool IsMetadataOnlyRule { get; private set; }

        [JsonConstructor]
        private ApplicabilityRule()
        { 
        }

        internal static List<ApplicabilityRule> FromXml(XPathNavigator metadataNavigator, XmlNamespaceManager namespaceManager)
        {
            var returnList = new List<ApplicabilityRule>();

            XPathExpression applicabilityRulesQuery = metadataNavigator.Compile("upd:Update/upd:ApplicabilityRules/*");
            applicabilityRulesQuery.SetContext(namespaceManager);
            var applicabilityRulesQueryResult = metadataNavigator.Evaluate(applicabilityRulesQuery) as XPathNodeIterator;

            while (applicabilityRulesQueryResult.MoveNext())
            {
                switch (applicabilityRulesQueryResult.Current.Name)
                {
                    case "upd:IsInstalled":
                        returnList.Add(
                            new ApplicabilityRule(applicabilityRulesQueryResult.Current, namespaceManager, IsMetadataRule.No)
                            { 
                                RuleType = ApplicabilityRuleType.IsInstalled
                            });
                        break;

                    case "upd:IsInstallable":
                        returnList.Add(
                            new ApplicabilityRule(applicabilityRulesQueryResult.Current, namespaceManager, IsMetadataRule.No) 
                            { 
                                RuleType = ApplicabilityRuleType.IsInstallable 
                            });
                        break;

                    case "upd:Metadata":
                        returnList.AddRange(MetadataFromXml(applicabilityRulesQueryResult.Current, namespaceManager));
                        break;

                    case "b.WindowsVersion":
                        returnList.Add(
                            new ApplicabilityRule(applicabilityRulesQueryResult.Current, namespaceManager, IsMetadataRule.No)
                            { 
                                RuleType = ApplicabilityRuleType.WindowsVersion 
                            });
                        break;

                    case "upd:IsSuperseded":
                        returnList.Add(
                            new ApplicabilityRule(applicabilityRulesQueryResult.Current, namespaceManager, IsMetadataRule.No)
                            {
                                RuleType = ApplicabilityRuleType.IsSuperseded
                            });
                        break;


                    default:
                        throw new Exception("Unknown rule: " + applicabilityRulesQueryResult.Current.Name);
                }
            }

            return returnList;
        }

        internal static List<ApplicabilityRule> MetadataFromXml(XPathNavigator metadataNavigator, XmlNamespaceManager namespaceManager)
        {
            var returnList = new List<ApplicabilityRule>();
            if (!metadataNavigator.HasChildren)
            {
                throw new Exception("Expected child nodes with metadata; got none");
            }

            var applicabilityRulesQueryResult = metadataNavigator.SelectChildren(XPathNodeType.Element);

            while (applicabilityRulesQueryResult.MoveNext())
            {
                switch (applicabilityRulesQueryResult.Current.Name)
                {
                    case "cbsar:CbsPackageApplicabilityMetadata":
                        {
                            var parent = applicabilityRulesQueryResult.Current.Clone();
                            parent.MoveToParent();

                            returnList.Add(
                                new ApplicabilityRule(parent, namespaceManager, IsMetadataRule.Yes)
                                {
                                    RuleType = ApplicabilityRuleType.CbsPackageApplicabilityMetadata
                                });
                        }
                        
                        break;

                    case "mar:MsiPatchMetadata":
                        {
                            var parent = applicabilityRulesQueryResult.Current.Clone();
                            parent.MoveToParent();

                            returnList.Add(
                                new ApplicabilityRule(parent, namespaceManager, IsMetadataRule.Yes)
                                {
                                    RuleType = ApplicabilityRuleType.MsiPatchMetadata
                                });
                        }

                        break;

                    case "mar:MsiApplicationMetadata":
                        {
                            var parent = applicabilityRulesQueryResult.Current.Clone();
                            parent.MoveToParent();

                            returnList.Add(
                                new ApplicabilityRule(parent, namespaceManager, IsMetadataRule.Yes)
                                {
                                    RuleType = ApplicabilityRuleType.MsiApplicationMetadata
                                });
                        }

                        break;

                    case "drv:WindowsDriverMetaData":
                        {
                            var parent = applicabilityRulesQueryResult.Current.Clone();
                            parent.MoveToParent();

                            returnList.Add(
                                new ApplicabilityRule(parent, namespaceManager, IsMetadataRule.Yes)
                                {
                                    RuleType = ApplicabilityRuleType.WindowsDriverMetadata
                                });
                        }
                        
                        break;

                    case "drv:WindowsDriver":
                        {
                            var parent = applicabilityRulesQueryResult.Current.Clone();
                            parent.MoveToParent();

                            returnList.Add(
                                new ApplicabilityRule(parent, namespaceManager, IsMetadataRule.Yes)
                                {
                                    RuleType = ApplicabilityRuleType.WindowsDriver
                                });
                        }
                        break;

                    default:
                        throw new Exception("Unknown metadata rule: " + applicabilityRulesQueryResult.Current.Name);
                }
            }

            return returnList;
        }

        enum IsMetadataRule
        {
            No,
            Yes
        }

        private ApplicabilityRule(XPathNavigator ruleMetadataNavigator, XmlNamespaceManager namespaceManager, IsMetadataRule isMetadataRule)
        {
            ExpressionGroups = new List<ExpressionGroup>();

            var expressionElements = ruleMetadataNavigator.SelectChildren(XPathNodeType.Element);

            if (isMetadataRule == IsMetadataRule.No && expressionElements.Count != 1)
            {
                throw new NotImplementedException($"Exactly 1 expression expected for an applicability rule; got {expressionElements.Count}");
            }

            while (expressionElements.MoveNext())
            {
                if (ExpressionGroup.IsGroupElement(expressionElements.Current))
                {
                    ExpressionGroups.Add(ExpressionGroup.FromXml(expressionElements.Current, namespaceManager));
                }
                else
                {
                    this.Expression = new Expression(expressionElements.Current, namespaceManager);
                }
            }
        }
    }
}
