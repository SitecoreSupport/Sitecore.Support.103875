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

namespace Sitecore.Support.Data.Fields
{
    public class HtmlField : Sitecore.Data.Fields.HtmlField, IBrokenLinksRemove
    {
        public HtmlField(Field innerField) : base (innerField)
        {

        }
        public void RemoveBrokenLinks(Database db)
        {
            HtmlDocument htmlDocument = new HtmlDocument();
            htmlDocument.LoadHtml(base.Value);
            Assert.ArgumentNotNull(htmlDocument, "document");
            HtmlNodeCollection htmlNodeCollection = htmlDocument.DocumentNode.SelectNodes("//a[@href]");
            if (htmlNodeCollection == null)
            {
                return;
            }
            foreach (HtmlNode current in ((System.Collections.Generic.IEnumerable<HtmlNode>)htmlNodeCollection))
            {
                string attributeValue = current.GetAttributeValue("href", string.Empty);
                if (!string.IsNullOrEmpty(attributeValue))
                {
                    ID linkedItemID = this.GetLinkedItemID(attributeValue);
                   
                    if (!ID.IsNullOrEmpty(linkedItemID))
                    {
                        Item currItem = db.GetItem(linkedItemID);
                        if (currItem == null)
                        {
                            current.ParentNode.RemoveChild(current, true);
                        }
                    }
                }
            }

            HtmlNodeCollection htmlNodeMediaCollection = htmlDocument.DocumentNode.SelectNodes("//img");
            if (htmlNodeCollection == null)
            {
                return;
            }
            foreach (HtmlNode current in ((System.Collections.Generic.IEnumerable<HtmlNode>)htmlNodeCollection))
            {
                string attributeValue = current.GetAttributeValue("src", string.Empty);
                if (!string.IsNullOrEmpty(attributeValue))
                {
                    ID linkedItemID = this.GetLinkedItemID(attributeValue);

                    if (!ID.IsNullOrEmpty(linkedItemID))
                    {
                        Item currItem = db.GetItem(linkedItemID);
                        if (currItem == null)
                        {
                            current.ParentNode.RemoveChild(current, true);
                        }
                    }
                }
            }
            RuntimeHtml.FixBullets(htmlDocument);
            RuntimeHtml.FixSelectOptions(htmlDocument);
            base.Value = htmlDocument.DocumentNode.OuterHtml;
        }

        private ID GetLinkedItemID(string href)
        {
            Assert.ArgumentNotNull(href, "href");
            DynamicLink dynamicLink;
            try
            {
                dynamicLink = DynamicLink.Parse(href);
            }
            catch (InvalidLinkFormatException)
            {
                return null;
            }
            return dynamicLink.ItemId;
        }
    }
}