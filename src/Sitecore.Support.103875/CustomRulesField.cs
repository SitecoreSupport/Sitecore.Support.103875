using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Sitecore;
using Sitecore.Configuration;
using Sitecore.Security.Accounts;
using Sitecore.Sites;
using Sitecore.Data;
using Sitecore.Links;
using Sitecore.Diagnostics;
using Sitecore.Data.Items;
using Sitecore.Data.Fields;
using Sitecore.SecurityModel;
using Sitecore.Data.Serialization;
using Sitecore.Globalization;
using Sitecore.StringExtensions;
using Sitecore.Data.DataProviders.Sql;
using Sitecore.Reflection;
using System.Reflection;
using Sitecore.Diagnostics.PerformanceCounters;
using System.Data;
using Sitecore.Jobs;
using System.Text.RegularExpressions;
using Sitecore.Text;
using HtmlAgilityPack;
using Sitecore.Web;
using Sitecore.Layouts;
using Sitecore.Rules;
using System.Xml.Linq;

namespace Sitecore.Support.Data.Fields
{
    public class RulesField : Sitecore.Data.Fields.RulesField, IBrokenLinksRemove
    {
        private XDocument rulesDefinitionDocument;

        public RulesField(Field innerField, string runtimeValue) : base(innerField, runtimeValue)
        {

        }
        public RulesField(Field innerField) : base(innerField)
        {

        }

        public void RemoveBrokenLinks(Database db)
        {
            Assert.ArgumentNotNull(db, "db");
            if (this.RulesDefinitionDocument != null)
            {
                RulesDefinition rulesDefinition = new RulesDefinition(this.RulesDefinitionDocument.ToString());

                foreach (ID id in rulesDefinition.GetReferencedActions())
                {
                    if (db.GetItem(id) == null)
                    {
                        rulesDefinition.RemoveActionReferences(id);
                    }
                }

                foreach (ID id in rulesDefinition.GetReferencedConditions())
                {
                    if (db.GetItem(id) == null)
                    {
                        rulesDefinition.RemoveConditionReferences(id);
                    }
                }

                foreach (ID id in rulesDefinition.GetReferencedItems())
                {
                    if (db.GetItem(id) == null)
                    {
                        rulesDefinition.RemoveItemReferences(id);
                    }
                }
               
                this.rulesDefinitionDocument = rulesDefinition.Document;
                base.Value = ((this.RulesDefinitionDocument != null) ? this.RulesDefinitionDocument.ToString() : string.Empty);
            }
        }

        private XDocument RulesDefinitionDocument
        {
            get
            {
                if (this.rulesDefinitionDocument == null && !string.IsNullOrEmpty(base.Value))
                {
                    try
                    {
                        this.rulesDefinitionDocument = XDocument.Parse(base.Value);
                    }
                    catch (System.Exception owner)
                    {
                        Log.Error(string.Concat(new string[]
                        {
                    "Invalid value in the Rules field type. Item: ",
                    AuditFormatter.FormatItem(base.InnerField.Item),
                    ". Field: ",
                    AuditFormatter.FormatField(base.InnerField),
                    "."
                        }), owner);
                    }
                }
                return this.rulesDefinitionDocument;
            }
        }
    }
}