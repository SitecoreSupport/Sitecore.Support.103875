using System;
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
using Sitecore.Support.Links;

namespace Sitecore.Support.sitecore.admin
{
    public class RemoveBrokenLinks : Sitecore.sitecore.admin.RemoveBrokenLinks
    {
        protected new void Page_Load(object sender, EventArgs e)
        {
            if (!this.CheckSecurity())
            {
                return;
            }
            if (!this.Page.IsPostBack)
            {
                string[] databaseNames = Factory.GetDatabaseNames();
                this.Databases.DataSource = databaseNames;
                this.Databases.DataBind();
            }
            else
            {
                this.Results.Visible = true;
            }
            if (!string.IsNullOrEmpty(base.Request["databases"]))
            {
                string[] array = base.Request["databases"].Split(new char[]
                {
                    '|'
                });
                bool serializeItem;
                bool.TryParse(base.Request["serializeItem"], out serializeItem);
                string[] array2 = array;
                for (int i = 0; i < array2.Length; i++)
                {
                    string databaseName = array2[i];
                    this.FixBrokenLinksInDatabase(databaseName, serializeItem);
                }
                base.Response.Clear();
            }
        }

        protected new void FixBrokenLinksOnClick(object sender, EventArgs e)
        {
            foreach (System.Web.UI.WebControls.ListItem listItem in this.Databases.Items)
            {
                if (listItem.Selected)
                {
                    int num = this.FixBrokenLinksInDatabase(listItem.Value, this.ShouldSerializeItem.Checked);
                    string text = "<div><b>Database: {0}. {1}</b></div>".FormatWith(new object[]
                    {
                        listItem.Value,
                        (num > 0) ? "Links Removed: {0}".FormatWith(new object[]
                        {
                            num
                        }) : "No broken links were found."
                    });
                    this.Results.Controls.Add(new Sitecore.Web.UI.HtmlControls.Literal(text));
                }
            }
        }
        /// <summary>
		    /// Checks the security.
		    /// </summary>
		    /// <returns></returns>
		    private bool CheckSecurity()
        {
            User user = Sitecore.Context.User;
            if (user != null && user.IsAdministrator)
            {
                return true;
            }
            SiteContext site = Sitecore.Context.Site;
            string text = (site != null) ? site.LoginPage : "";
            if (text.Length > 0)
            {
                base.Response.Redirect(text, true);
            }
            return false;
        }
        private int FixBrokenLinksInDatabase(string databaseName, bool serializeItem)
        {
            Database database = Database.GetDatabase(databaseName);
            Assert.IsNotNull(database, "database");
            return this.FixBrokenLinksInDatabase(database, serializeItem);
        }

        private int FixBrokenLinksInDatabase(Database database, bool serializeItem)
        {
          Globals.LinkDatabase.Rebuild(database);
          
          // Begin of Sitecore.Support.103875
          var allBrokenLinks = Globals.LinkDatabase.GetBrokenLinks(database);

          var brokenLinks = new BrokenLinksFilter().ExcludeSystemItemLinks(database, allBrokenLinks);
          // End of Sitecore.Support.103875

          foreach (var brokenLink in brokenLinks)
          {
            var sourceItem = brokenLink.GetSourceItem();
            if (sourceItem == null)
            {
              continue;
            }

            var sourceField = sourceItem.Fields[brokenLink.SourceFieldID];
            var customField = FieldTypeManager.GetField(sourceField);
            Assert.IsNotNull(customField, "customField");

            using (new SecurityDisabler())
            {
              using (new EditContext(sourceItem))
              {
                this.LogLinkRemove(brokenLink);
                customField.RemoveLink(brokenLink);
                if (serializeItem)
                {
                  // For Shared and Unversioned fields version in Invariant Language will be created. We need to delete this version.
                  if (sourceItem.Language == Language.Invariant)
                  {
                    sourceItem.RecycleVersion();
                  }

                  Manager.DumpItem(sourceItem);
                }

                Log.Info("Done", this);
              }
            }
          }

          return brokenLinks.Length;
        }
  
        private void LogLinkRemove(ItemLink brokenLink)
        {
            Item sourceItem = brokenLink.GetSourceItem();
            Assert.IsNotNull(sourceItem, "sourceItem");
            Field field = sourceItem.Fields[brokenLink.SourceFieldID];
            string text = "Removing broken link- Database: {0}, Item: {1}, Field: {2}, Target item database: {3}, Target item path: {4}".FormatWith(new object[]
            {
                brokenLink.SourceDatabaseName,
                sourceItem.Paths.FullPath,
                field.Name,
                brokenLink.TargetDatabaseName,
                brokenLink.TargetPath
            });
            Log.Info(text, this);
            this.Results.Controls.Add(new System.Web.UI.LiteralControl("<div>{0}</div>".FormatWith(new object[]
            {
                text
            })));
        }
    }
}