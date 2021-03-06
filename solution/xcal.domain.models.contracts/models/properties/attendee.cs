﻿using reexjungle.xcal.core.domain.contracts.models.parameters;
using reexjungle.xcal.core.domain.contracts.models.values;

namespace reexjungle.xcal.core.domain.contracts.models.properties
{
    public interface IATTENDEE
    {
        CAL_ADDRESS Address { get; }
        CUTYPE CalendarUserType { get; }
        IMEMBER Member { get; }
        ROLE Role { get; }
        PARTSTAT Participation { get; }
        BOOLEAN Rsvp { get; }
        IDELEGATE_TO Delegatee { get; }
        IDELEGATE_FROM Delegator { get; }
        ISENT_BY SentBy { get; }
        string CN { get; }
        IURI Directory { get; }
        ILANGUAGE Language { get; }
    }
}
