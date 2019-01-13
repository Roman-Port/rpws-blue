using System;
using System.Collections.Generic;
using System.Text;
using LiteDB;

namespace RpwsBlue.Entities
{
    /// <summary>
    /// Claim request.
    /// </summary>
    public class AppstoreClaimRequest
    {
        /// <summary>
        /// Status
        /// </summary>
        public AppstoreClaimRequestStatus status { get; set; }

        /// <summary>
        /// UUID of the user capturing this.
        /// </summary>
        public string userUuid { get; set; }

        /// <summary>
        /// App IDs trying to claim
        /// </summary>
        public string[] appIds { get; set; }

        /// <summary>
        /// Reasoning the user used.
        /// </summary>
        public string reasoning { get; set; }

        /// <summary>
        /// Claim types the user used.
        /// </summary>
        public AppstoreClaimRequestReasons[] claimTypes { get; set; }

        /// <summary>
        /// Time this was opened.
        /// </summary>
        public long open_time { get; set; }

        /// <summary>
        /// Unique claim ID
        /// </summary>
        public string uuid { get; set; }

        /// <summary>
        /// E-Mail log
        /// </summary>
        public List<AppstoreClaimRequestMessage> messageLog { get; set; }

        /// <summary>
        /// ID
        /// </summary>
        public int _id { get; set; }

        public static LiteCollection<AppstoreClaimRequest> GetCollection()
        {
            return LibRpws.LibRpwsCore.lite_database.GetCollection<AppstoreClaimRequest>("publishing_claim_requests");
        }

        public static LiteCollection<AppstoreClaimRequest> GetArchiveCollection()
        {
            return LibRpws.LibRpwsCore.lite_database.GetCollection<AppstoreClaimRequest>("publishing_claim_requests_archive");
        }

        /// <summary>
        /// Send a email to the client 
        /// </summary>
        /// <param name="header"></param>
        /// <param name="contents"></param>
        public void SendClientEmail(string header, string contents)
        {
            string content = GenerateEmailContents(contents);
            LibRpws.LibRpwsCore.SendUserEmail(LibRpws.Users.LibRpwsUsers.GetUserByUuid(userUuid), $"RPWS App Claim Request Update - {uuid}", content, null);
        }

        /// <summary>
        /// Send myself an email.
        /// </summary>
        /// <param name="header"></param>
        /// <param name="contents"></param>
        public void SendWebmasterEmail(string header, string contents)
        {
            string content = GenerateEmailContents(contents);
            LibRpws.LibRpwsCore.SendUserEmail(LibRpws.Users.LibRpwsUsers.GetUserByUuid(userUuid), $"RPWS App Claim Request - {header}", content, null);
        }

        private string GenerateEmailContents(string contents)
        {
            string appIdsString = "";
            foreach (string s in appIds)
                appIdsString += s + ", ";
            appIdsString = appIdsString.Substring(0, appIdsString.Length - 2);

            var open_time = new DateTime(this.open_time);

            string content = $"App IDs : {appIdsString}\nStatus : {status.ToString()}\nTime Started : {open_time.ToLongDateString()} at {open_time.ToLongTimeString()}\nSupport request UUID : {uuid}\n\n{contents}";
            return content;
        }
    }

    public enum AppstoreClaimRequestStatus
    {
        Open,
        Dismissed,
        Accepted,
        AwaitingReply
    }

    public enum AppstoreClaimRequestReasons
    {
        Offering_source_code = 0,
        Verifying_website = 1,
        Own_email_address_on_source_page = 2,
        Own_support_email_address = 3,
        Own_API_endpoint = 4,
        Other = 5
    }

    public class AppstoreClaimRequestMessage
    {
        public bool fromClient { get; set; }
        public string contents { get; set; }
        public long time { get; set; }
        public string time_string { get; set; }
    }
}
