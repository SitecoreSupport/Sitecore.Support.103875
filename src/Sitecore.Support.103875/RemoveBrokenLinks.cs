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

        private void RemoveLink(CustomField field)
        {

        }

        private int FixBrokenLinksInDatabase(Database database, bool serializeItem)
        {
            //Support FIX 103875
              Globals.LinkDatabase.Rebuild(database);
              //ItemLink[] brokenLinks = Globals.LinkDatabase.GetBrokenLinks(database);
            //Support FIX 103875
            ItemLink[] brokenLinks = this.GetBrokenLinks(database);
            ItemLink[] array = brokenLinks;
            for (int i = 0; i < array.Length; i++)
            {
                ItemLink itemLink = array[i];
                Item sourceItem = itemLink.GetSourceItem();
                if (sourceItem != null)
                {
                    Field field = sourceItem.Fields[itemLink.SourceFieldID];
                    //Support FIX 103875
                    //CustomField field2 = FieldTypeManager.GetField(field);
                    //Assert.IsNotNull(field2, "customField");
                    using (new SecurityDisabler())
                    {
                        using (new EditContext(sourceItem))
                        {
                            this.LogLinkRemove(itemLink);
                            IBrokenLinksRemove iLinkRemove = FieldTypeManager.GetFieldType(field.Type).GetField(field) as IBrokenLinksRemove;
                            if ( iLinkRemove != null)
                            {
                                iLinkRemove.RemoveBrokenLinks(database);
                                //IBrokenLinksRemove iBroken = field2 as IBrokenLinksRemove;
                                //iBroken.RemoveBrokenLinks(database);
                            }
                            else
                            {
                                CustomField field2 = FieldTypeManager.GetField(field);
                                Assert.IsNotNull(field2, "customField");
                                field2.Value = String.Empty;
                            }
                            if (serializeItem)
                            {
                                if (sourceItem.Language == Language.Invariant)
                                {
                                    sourceItem.RecycleVersion();
                                }
                                Manager.DumpItem(sourceItem);
                            }
                            Log.Info("Done", this);
                        }
                    }
                    //Support FIX 103875
                }
            }
            return brokenLinks.Length;
        }

        public ItemLink[] GetBrokenLinks(Database database)
        {
            //Get the DataApi from LinkDatabase using reflection
            Type linkdbtype = Globals.LinkDatabase.GetType();
            PropertyInfo dataApiInf = linkdbtype.GetProperty("DataApi", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            object linkDb = Globals.LinkDatabase as object;
            SqlDataApi api = dataApiInf.GetValue(linkDb) as SqlDataApi;
            Assert.ArgumentNotNull(api, "DataApi from reflection");
            //Get the DataApi from LinkDatabase using reflection

            Assert.ArgumentNotNull(database, "database");
            string sql = " SELECT {0}SourceItemID{1}, {0}SourceLanguage{1}, {0}SourceVersion{1}, {0}SourceFieldID{1}, {0}TargetDatabase{1}, {0}TargetItemID{1}, {0}TargetLanguage{1}, {0}TargetVersion{1}, {0}TargetPath{1}\r\n                      FROM {0}Links{1}\r\n                      WHERE {0}SourceDatabase{1} = {2}database{3}\r\n                      ORDER BY {0}SourceItemID{1}, {0}SourceFieldID{1}";
            MethodInfo getStringMethod = linkdbtype.GetMethod("GetString", BindingFlags.Instance | BindingFlags.NonPublic);
            ItemLink[] result;
            using (DataProviderReader dataProviderReader = api.CreateReader(sql, new object[]
            {
        "database",
       getStringMethod.Invoke(linkDb, new object [] {database.Name,150})
            }))
            {
                DataCount.LinksDataRead.Increment(1L);
                List<ItemLink> list = new List<ItemLink>();
                this.AddBrokenLinks(dataProviderReader.InnerReader, list, database);
                result = list.ToArray();
            }
            return result;
        }

        private void AddBrokenLinks(IDataReader reader, List<ItemLink> links, Database database)
        {
            Assert.ArgumentNotNull(reader, "reader");
            Assert.ArgumentNotNull(links, "links");
            Assert.ArgumentNotNull(database, "database");
            Type linkdbtype = Globals.LinkDatabase.GetType();
            MethodInfo getItemExistsMethod = linkdbtype.GetMethod("ItemExists", BindingFlags.Instance | BindingFlags.NonPublic);
            using (new SecurityDisabler())
            {
                string name = database.Name;
                while (reader.Read())
                {
                    ID sourceItemID = Sitecore.Data.ID.Parse(reader.GetGuid(0));
                    Language sourceItemLanguage = Language.Parse(reader.GetString(1));
                    Sitecore.Data.Version sourceItemVersion = Sitecore.Data.Version.Parse(reader.GetInt32(2));
                    ID sourceFieldID = Sitecore.Data.ID.Parse(reader.GetGuid(3));
                    string @string = reader.GetString(4);
                    ID iD = Sitecore.Data.ID.Parse(reader.GetGuid(5));
                    Language language = Language.Parse(reader.GetString(6));
                    Sitecore.Data.Version version = Sitecore.Data.Version.Parse(reader.GetInt32(7));
                    string string2 = reader.GetString(8);
                    Database database2 = Factory.GetDatabase(@string);
                    //Support FIX 103875. Check if the TargetItem Exists. Than receives list of the valid
                    // links from the field and if the link from Db is not in the valid list, it will be considered 
                    // as broken.
                    bool flag = (bool)getItemExistsMethod.Invoke(Globals.LinkDatabase as object, new object[] { iD, string2, language, version, database2 });
                    if (!flag && !Sitecore.Data.ID.IsNullOrEmpty(sourceFieldID))
                    {
                   
                        if (database.GetItem(sourceItemID).Paths.IsContentItem)
                        {
                            ItemLink currentLink = new ItemLink(name, sourceItemID, sourceItemLanguage, sourceItemVersion, sourceFieldID, name, iD, language, version, string2);
                            links.Add(currentLink);
                           
                        }
                    }
                    //Support FIX 103875
                    Job job = Sitecore.Context.Job;
                    if (job != null && job.Category == "GetBrokenLinks")
                    {
                        job.Status.Processed += 1L;
                    }
                    DataCount.LinksDataRead.Increment(1L);
                    DataCount.DataPhysicalReads.Increment(1L);
                }
            }
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