﻿using reexjungle.xcal.domain.contracts;
using reexjungle.xcal.domain.models;
using reexjungle.xmisc.infrastructure.contracts;
using ServiceStack.ServiceHost;
using System;
using System.Collections.Generic;

namespace reexjungle.xcal.service.operations.concretes.live
{
    #region Search-Create-Update-Patch-Delete(SCRUPD) operations

    [Route("/calendars/{CalendarId}/events/add", "POST")]
    public class AddEvent : IReturnVoid
    {
        public Guid CalendarId { get; set; }

        public VEVENT Event { get; set; }
    }

    [Route("/calendars/{CalendarId}/events/batch/add", "POST")]
    public class AddEvents : IReturnVoid
    {
        public Guid CalendarId { get; set; }

        public List<VEVENT> Events { get; set; }
    }

    [Route("/calendars/events/update", "PUT")]
    public class UpdateEvent : IReturnVoid
    {
        public VEVENT Event { get; set; }
    }

    [Route("/calendars/events/batch/update", "PUT")]
    public class UpdateEvents : IReturnVoid
    {
        public List<VEVENT> Events { get; set; }
    }

    [Route("/calendars/events/{EventId}/patch", "POST")]
    public class PatchEvent : IReturnVoid
    {
        public Guid EventId { get; set; }

        public DATE_TIME Datestamp { get; set; }

        public DATE_TIME Start { get; set; }

        public CLASS Classification { get; set; }

        public DATE_TIME Created { get; set; }

        public DESCRIPTION Description { get; set; }

        public GEO Position { get; set; }

        public DATE_TIME LastModified { get; set; }

        public LOCATION Location { get; set; }

        public ORGANIZER Organizer { get; set; }

        public PRIORITY Priority { get; set; }

        public int Sequence { get; set; }

        public STATUS Status { get; set; }

        public SUMMARY Summary { get; set; }

        public TRANSP Transparency { get; set; }

        public URL Url { get; set; }

        public RECUR RecurrenceRule { get; set; }

        public DATE_TIME End { get; set; }

        public DURATION Duration { get; set; }

        public List<ATTACH> Attachments { get; set; }

        public List<ATTENDEE> Attendees { get; set; }

        public CATEGORIES Categories { get; set; }

        public List<COMMENT> Comments { get; set; }

        public List<CONTACT> Contacts { get; set; }

        public List<EXDATE> ExceptionDates { get; set; }

        public List<REQUEST_STATUS> RequestStatuses { get; set; }

        public List<RESOURCES> Resources { get; set; }

        public List<RELATEDTO> RelatedTos { get; set; }

        public List<RDATE> RecurrenceDates { get; set; }

        public List<VALARM> Alarms { get; set; }

    }

    [Route("/calendars/events/batch/patch", "POST")]
    public class PatchEvents : IReturnVoid
    {
        public List<Guid> EventIds { get; set; }

        public DATE_TIME Datestamp { get; set; }

        public DATE_TIME Start { get; set; }

        public CLASS Classification { get; set; }

        public DATE_TIME Created { get; set; }

        public DESCRIPTION Description { get; set; }

        public GEO Position { get; set; }

        public DATE_TIME LastModified { get; set; }

        public LOCATION Location { get; set; }

        public ORGANIZER Organizer { get; set; }

        public PRIORITY Priority { get; set; }

        public int Sequence { get; set; }

        public STATUS Status { get; set; }

        public SUMMARY Summary { get; set; }

        public TRANSP Transparency { get; set; }

        public URL Url { get; set; }

        public RECUR RecurrenceRule { get; set; }

        public DATE_TIME End { get; set; }

        public DURATION Duration { get; set; }

        public List<ATTACH> Attachments { get; set; }

        public List<ATTENDEE> Attendees { get; set; }

        public CATEGORIES Categories { get; set; }

        public List<COMMENT> Comments { get; set; }

        public List<CONTACT> Contacts { get; set; }

        public List<EXDATE> ExceptionDates { get; set; }

        public List<REQUEST_STATUS> RequestStatuses { get; set; }

        public List<RESOURCES> Resources { get; set; }

        public List<RELATEDTO> RelatedTos { get; set; }

        public List<RDATE> RecurrenceDates { get; set; }

        public List<VALARM> Alarms { get; set; }
    }

    [Route("/calendars/events/{EventId}/delete", "DELETE")]
    public class DeleteEvent : IReturnVoid
    {
        public Guid EventId { get; set; }
    }

    [Route("/calendars/events/batch/delete", "POST")]
    public class DeleteEvents : IReturnVoid
    {
        public List<Guid> EventIds { get; set; }
    }

    [Route("/calendars/events/{EventId}/find", "GET")]
    public class FindEvent : IReturn<VEVENT>
    {
        public Guid EventId { get; set; }
    }

    [Route("/calendars/events/batch/find", "POST")]
    [Route("/calendars/events/batch/find/{Page}/{Size}", "POST")]
    [Route("/calendars/events/batch/find/page/{Page}/{Size}", "POST")]
    [Route("/calendars/events/batch/find/page/{Page}/size/{Size}", "POST")]
    public class FindEvents : IReturn<List<VEVENT>>, IPaginated<int>
    {
        public List<Guid> EventIds { get; set; }

        public int? Page { get; set; }

        public int? Size { get; set; }
    }

    [Route("/calendars/events/{Page}/{Size}", "GET")]
    [Route("/calendars/events/page/{Page}/size/{Size}", "GET")]
    public class GetEvents : IReturn<List<VEVENT>>, IPaginated<int>
    {
        public int? Page { get; set; }

        public int? Size { get; set; }
    }

    [Route("/calendars/events/keys/{Page}/{Size}", "GET")]
    [Route("/calendars/events/keys/page/{Page}/size/{Size}", "GET")]
    public class GetEventKeys : IReturn<List<Guid>>, IPaginated<int>
    {
        public int? Page { get; set; }

        public int? Size { get; set; }
    }

    #endregion Search-Create-Update-Patch-Delete(SCRUPD) operations
}