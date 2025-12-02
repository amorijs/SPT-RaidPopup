using System;
using JsonType;

namespace RaidPopup.Models
{
    /// <summary>
    /// Represents an active raid that another player has started
    /// </summary>
    public class ActiveRaid
    {
        public string Id { get; private set; }
        public string Nickname { get; set; }
        public string Location { get; set; }
        public EDateTime RaidTime { get; set; }
        public DateTime ReceivedAt { get; set; }
        
        /// <summary>
        /// ServerId is the host's profile ID - needed to join the raid.
        /// Will be populated if we can look it up from the raids list.
        /// </summary>
        public string ServerId { get; set; }

        public ActiveRaid()
        {
            Id = Guid.NewGuid().ToString();
            ReceivedAt = DateTime.Now;
        }
        
        /// <summary>
        /// Whether we have enough info to attempt joining this raid
        /// </summary>
        public bool CanJoin
        {
            get { return !string.IsNullOrEmpty(ServerId); }
        }

        /// <summary>
        /// Get a formatted display string for the raid time
        /// </summary>
        public string GetFormattedTime()
        {
            // EDateTime.CURR = daytime, EDateTime.PAST = nighttime
            return RaidTime == EDateTime.CURR ? "DAY" : "NIGHT";
        }

        /// <summary>
        /// Get a friendly location name
        /// </summary>
        public string GetDisplayLocation()
        {
            // Map internal location IDs to friendly names
            switch (Location?.ToLower())
            {
                case "factory4_day":
                case "factory4_night":
                    return "Factory";
                case "bigmap":
                    return "Customs";
                case "woods":
                    return "Woods";
                case "shoreline":
                    return "Shoreline";
                case "interchange":
                    return "Interchange";
                case "rezervbase":
                    return "Reserve";
                case "laboratory":
                    return "Labs";
                case "lighthouse":
                    return "Lighthouse";
                case "tarkovstreets":
                    return "Streets";
                case "sandbox":
                case "sandbox_high":
                    return "Ground Zero";
                default:
                    return Location ?? "Unknown";
            }
        }
    }
}

