﻿using System;
using ServiceStack.OrmLite;
using ServiceStack.DataAnnotations;
using reexmonkey.xcal.domain.models;

namespace reexmonkey.xcal.service.repositories.concretes
{
    public class REL_CALENDARS_EVENTS : IEquatable<REL_CALENDARS_EVENTS>
    {
        /// <summary>
        /// Gets or sets the unique identifier of the calendar-event relation
        /// </summary>
        [Index(true)]
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the unique identifier of the related calendar entity
        /// </summary>
        [ForeignKey(typeof(VCALENDAR), OnDelete = "CASCADE", OnUpdate = "CASCADE")]
        public string CalendarId { get; set; }

        /// <summary>
        /// Gets or sets the unique identifier of the related event entity
        /// </summary>
        [ForeignKey(typeof(VEVENT), OnDelete = "CASCADE", OnUpdate = "CASCADE")]
        public string EventId { get; set; }

        public bool Equals(REL_CALENDARS_EVENTS other)
        {
            if (other == null) return false;
            return (this.CalendarId.Equals(other.CalendarId, StringComparison.OrdinalIgnoreCase) &&
                this.EventId.Equals(other.EventId, StringComparison.OrdinalIgnoreCase));
        }

        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            var rel = obj as REL_CALENDARS_EVENTS;
            if (rel == null) return false;
            return this.Equals(rel);
        }

        public override int GetHashCode()
        {
            return this.CalendarId.GetHashCode() ^ this.EventId.GetHashCode();
        }

        public static bool operator ==(REL_CALENDARS_EVENTS x, REL_CALENDARS_EVENTS y)
        {
            if ((object)x == null || (object)y == null) return object.Equals(x, y);
            return x.Equals(y);
        }

        public static bool operator !=(REL_CALENDARS_EVENTS x, REL_CALENDARS_EVENTS y)
        {
            if (x == null || y == null) return !object.Equals(x, y);
            return !x.Equals(y);
        }
    }
}