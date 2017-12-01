using System;
using System.Configuration;
using System.Web.UI;
using MongoDB.Driver;
using Sitecore.Analytics;
using Sitecore.Analytics.Model.Entities;
using Sitecore.Exceptions;

namespace SitecoreXDBHelper
{
    /// <summary>
    /// Standalone page helping to debug xDB issues by showing details of the current contact and visit. Also allows flushing of session to enforce xDB processing.
    /// </summary>
    public partial class XDbHelper : Page
    {
        /// <summary>
        /// Set your random access key here. You will only be able to access the page if you have defined a key.
        /// </summary>
        public const string AccessKey = "";

        protected override void OnInit(EventArgs e)
        {
            var keyParam = Request.QueryString.Get("key");
            if (string.IsNullOrEmpty(keyParam) || AccessKey != keyParam)
            {
                throw new AccessDeniedException("Invalid access key");
            }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            // Try to get number of interactions and contacts from mongodb
            GetInteractions();

            if (!Tracker.IsActive)
            {
                InfoBox.Text = "TRACKER IS NOT ACTIVE! Check your mongoDB connection and log files.";
                return;
            }

            InteractionLabel.Text = Tracker.Current.Interaction.InteractionId.ToString();
            ContactLabel.Text = Tracker.Current.Contact.ContactId.ToString();
            var browserInfo = Tracker.Current.Interaction.BrowserInfo;
            BrowserVersionLabel.Text = string.Format("{0} - {1}, {2}", browserInfo.BrowserMajorName,
                browserInfo.BrowserMinorName, browserInfo.BrowserVersion);
            ClientIp.Text = string.Format("IP: {0} / X-Forwarded-For: {1}", Request.UserHostAddress, Server.HtmlEncode(Request.Headers["X-Forwarded-For"]));

            GeoIpCity.Text = Tracker.Current.Interaction.GeoData.City;
            GeoIpCountry.Text = Tracker.Current.Interaction.GeoData.Country;

            IdentificationLevel.Text = Tracker.Current.Contact.Identifiers.IdentificationLevel.ToString();
            Identifier.Text = Tracker.Current.Contact.Identifiers.Identifier;

            if (!IsPostBack)
            {
                var personalInfo = Tracker.Current.Contact.GetFacet<IContactPersonalInfo>("Personal");
                ContactFirstName.Text = personalInfo.FirstName;
                ContactSurname.Text = personalInfo.Surname;
            }
        }

        /// <summary>
        /// Tries to get number of interactions and contacts from the monodb analytics database.
        /// </summary>
        private void GetInteractions()
        {
            try
            { 
                string connectionString = ConfigurationManager.ConnectionStrings["analytics"].ConnectionString;
                MongoUrl mongoUrl = new MongoUrl(connectionString);
                MongoServer server = (new MongoClient(connectionString)).GetServer();
                MongoDatabase database = server.GetDatabase(mongoUrl.DatabaseName);

                var collection = database.GetCollection("Interactions");
                LastInteractions.Text = collection.Count().ToString();

                collection = database.GetCollection("Contacts");
                LatestContacts.Text = collection.Count().ToString();
            }
            catch (Exception ex)
            {
                LastInteractions.Text = string.Concat("ERROR:", ex.Message);
            }
        }

        protected void SessionAbandonClick(object sender, EventArgs e)
        {
            Session.Abandon();
            InfoBox.Text = "Session abandaned";
        }

        protected void IdentifyClick(object sender, EventArgs e)
        {
            Tracker.Current.Session.Identify(ContactIdentity.Text);

            InfoBox.Text = "Contact identified";
        }

        protected void SaveDataClick(object sender, EventArgs e)
        {
            var personalInfo = Tracker.Current.Contact.GetFacet<IContactPersonalInfo>("Personal");
            personalInfo.FirstName = ContactFirstName.Text;
            personalInfo.Surname = ContactSurname.Text;

            InfoBox.Text = "Contact data saved";
        }
    }
}