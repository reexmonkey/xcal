﻿using FizzWare.NBuilder;
using reexjungle.xcal.domain.contracts;
using reexjungle.xcal.domain.models;
using reexjungle.xcal.test.units.contracts;
using reexjungle.xmisc.infrastructure.concretes.operations;
using reexjungle.xmisc.infrastructure.contracts;
using System;
using System.Collections.Generic;

namespace reexjungle.xcal.test.units.concretes
{
    public class PropertyTester : IPropertyTester
    {
        private readonly IKeyGenerator<Guid> keyGenerator;

        public PropertyTester()
        {
            keyGenerator = new SequentialGuidKeyGenerator();
        }

        public IEnumerable<ATTENDEE> GenerateAttendeesOfSize(int n)
        {
            return Builder<ATTENDEE>.CreateListOfSize(n)
                .All()
                .With(x => x.Id = keyGenerator.GetNext())
                .And(x => x.CN = Pick<string>.RandomItemFrom(new[] { "Caesar", "Koba", "Cornelia", "Blue Eyes", "Grey", "Ash" }))
                .And(x => x.Address = new URI(string.Format("{0}@apes.je", x.CN.Replace(" ", ".").ToLower())))
                .And(x => x.Role = Pick<ROLE>.RandomItemFrom(new List<ROLE> { ROLE.CHAIR, ROLE.NON_PARTICIPANT, ROLE.OPT_PARTICIPANT, ROLE.REQ_PARTICIPANT }))
                .And(x => x.Participation = Pick<PARTSTAT>.RandomItemFrom(new List<PARTSTAT> { PARTSTAT.ACCEPTED, PARTSTAT.COMPLETED, PARTSTAT.DECLINED, PARTSTAT.NEEDS_ACTION, PARTSTAT.TENTATIVE }))
                .And(x => x.CalendarUserType = Pick<CUTYPE>.RandomItemFrom(new List<CUTYPE> { CUTYPE.GROUP, CUTYPE.INDIVIDUAL, CUTYPE.RESOURCE, CUTYPE.ROOM }))
                .And(x => x.Language = new LANGUAGE(Pick<string>.RandomItemFrom(new List<string> { "en", "fr", "de" })))
                .Build();
        }
    }
}