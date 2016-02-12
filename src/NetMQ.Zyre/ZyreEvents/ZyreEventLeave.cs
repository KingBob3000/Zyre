﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NetMQ.Zyre.ZyreEvents
{
    public class ZyreEventLeave : EventArgs, IZyreEventHeader
    {
        /// <summary>
        /// The name of the group the sending peer is leaving
        /// </summary>
        public string GroupName { get; set; }

        /// <summary>
        /// The sending peer's identity.
        /// </summary>
        public Guid SenderUuid => _header.SenderUuid;


        /// <summary>
        /// The sending peer's public name.
        /// </summary>
        public string SenderName => _header.SenderName;

        /// <summary>
        /// The sending peer's headers.
        /// </summary>
        public Dictionary<string, string> Headers { get; set; }

        /// <summary>
        /// The sending peer's EndPoint.
        /// </summary>
        public string Address { get; set; }

        private readonly ZyreEventHeader _header;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="senderUuid">The sending peer's identity</param>
        /// <param name="senderName">The sending peer's public name</param>
        /// <param name="groupName">The name of the group the sending peer is leaving</param>
        public ZyreEventLeave(Guid senderUuid, string senderName, string groupName)
        {
            GroupName = groupName;
            _header = new ZyreEventHeader(senderUuid, senderName);
        }
    }
}
