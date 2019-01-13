using System;
using System.Collections.Generic;
using System.Text;

namespace RpwsBlue.Entities
{
    public class AppstoreAppVote
    {
        public int _id { get; set; }

        /// <summary>
        /// The ID of the app.
        /// </summary>
        public string appId { get; set; }

        /// <summary>
        /// Total number of votes.
        /// </summary>
        public int total { get; set; }

        /// <summary>
        /// Users and their votes.
        /// </summary>
        public Dictionary<string, int> users { get; set; }
    }
}
