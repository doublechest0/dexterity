﻿using System;
using System.Collections.Immutable;
using System.Net;
using Robust.Shared.Network;

namespace Content.Server.Database
{
    public sealed class PlayerRecord
    {
        public NetUserId UserId { get; }
        public ImmutableArray<byte>? HWId { get; }
        public DateTimeOffset FirstSeenTime { get; }
        public string LastSeenUserName { get; }
        public DateTimeOffset LastSeenTime { get; }
        public IPAddress LastSeenAddress { get; }

        public int TotalPlaytime { get; }

        public int LivingPlaytime { get; }

        public PlayerRecord(
            NetUserId userId,
            DateTimeOffset firstSeenTime,
            string lastSeenUserName,
            DateTimeOffset lastSeenTime,
            IPAddress lastSeenAddress,
            ImmutableArray<byte>? hwId,
            int totalPlaytime,
            int livingPlaytime)
        {
            UserId = userId;
            FirstSeenTime = firstSeenTime;
            LastSeenUserName = lastSeenUserName;
            LastSeenTime = lastSeenTime;
            LastSeenAddress = lastSeenAddress;
            HWId = hwId;
            TotalPlaytime = totalPlaytime;
            LivingPlaytime = livingPlaytime;
        }
    }
}
