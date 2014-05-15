﻿using System;
using System.Linq;
using System.Linq.Expressions;
using System.Collections.Generic;
using ServiceStack.Redis;
using ServiceStack.Redis.Generic;
using ServiceStack.Common;
using reexmonkey.xcal.domain.models;
using reexmonkey.foundation.essentials.contracts;
using reexmonkey.foundation.essentials.concretes;
using reexmonkey.xcal.service.repositories.contracts;
using reexmonkey.infrastructure.operations.contracts;
using reexmonkey.xcal.service.repositories.concretes.relations;


namespace reexmonkey.xcal.service.repositories.concretes.redis
{
    public class AudioAlarmRedisRepository: IAudioAlarmRedisRepository
    {
        private IRedisClientsManager manager;
        private IKeyGenerator<string> keygen;
        private IRedisClient client = null;
        private IRedisClient redis
        {
            get
            {
                return (client != null) ? client : this.manager.GetClient();
            }
        }

        public IRedisClientsManager RedisClientsManager
        {
            get { return this.manager; }
            set
            {
                if (value == null) throw new ArgumentNullException("RedisClientsManager");
                this.manager = value;
                this.client = manager.GetClient();
            }
        }

        public IKeyGenerator<string> KeyGenerator
        {
            get { return this.keygen; }
            set
            {
                if (value == null) throw new ArgumentNullException("KeyGenerator");
                this.keygen = value;
            }
        }
               
        public AudioAlarmRedisRepository() { }

        public AudioAlarmRedisRepository(IRedisClientsManager manager)
        {
            this.RedisClientsManager = manager;
        }

        public AudioAlarmRedisRepository(IRedisClient client)
        {
            if (client == null) throw new ArgumentNullException("IRedisClient");
            this.client = client;
        }
 
        public AUDIO_ALARM Find(string key)
        {
            try
            {
                return this.redis.As<AUDIO_ALARM>().GetById(key);
            }
            catch (ArgumentNullException) { throw; }
            catch (RedisResponseException) { throw; }
            catch (RedisException) { throw; }
            catch (InvalidOperationException) { throw; }
        }

        public IEnumerable<AUDIO_ALARM> FindAll(IEnumerable<string> keys, int? skip = null, int? take = null)
        {
            try
            {
                var allkeys = this.redis.As<AUDIO_ALARM>().GetAllKeys();
                if (skip == null && take == null)
                {
                    var filtered = !keys.NullOrEmpty()
                        ? allkeys.Intersect(keys.Select(x => UrnId.GetStringId(x)))
                        : new List<string>();
                    var dry = this.redis.As<AUDIO_ALARM>().GetByIds(filtered);
                    return !dry.NullOrEmpty() ? this.Hydrate(dry) : dry;
                }
                else
                {
                    var filtered = !keys.NullOrEmpty()
                        ? allkeys.Intersect(keys.Select(x => UrnId.GetStringId(x)))
                        : new List<string>();
                    var dry = this.redis.As<AUDIO_ALARM>().GetByIds(filtered);
                    return !dry.NullOrEmpty() ? this.Hydrate(dry) : dry;
                }
            }
            catch (ArgumentNullException) { throw; }
            catch (RedisResponseException) { throw; }
            catch (RedisException) { throw; }
            catch (InvalidOperationException) { throw; }
        }

        public IEnumerable<AUDIO_ALARM> Get(int? skip = null, int? take = null)
        {
            try
            {
                if (skip == null && take == null) return this.redis.As<AUDIO_ALARM>().GetAll();
                else
                {
                    var allkeys = this.redis.As<AUDIO_ALARM>().GetAllKeys();
                    var selected = !allkeys.NullOrEmpty()
                        ? allkeys.Skip(skip.Value).Take(take.Value)
                        : allkeys;
                    var dry = this.redis.As<AUDIO_ALARM>().GetValues(selected.ToList());
                    return !dry.NullOrEmpty() ? this.Hydrate(dry) : dry;
                }
            }
            catch (ArgumentNullException) { throw; }
            catch (RedisResponseException) { throw; }
            catch (RedisException) { throw; }
            catch (InvalidOperationException) { throw; }
        }

        public bool ContainsKey(string key)
        {
            try
            {
                return this.redis.As<AUDIO_ALARM>().ContainsKey(key);
            }
            catch (ArgumentNullException) { throw; }
            catch (RedisResponseException) { throw; }
            catch (RedisException) { throw; }
            catch (InvalidOperationException) { throw; }
        }

        public bool ContainsKeys(IEnumerable<string> keys, ExpectationMode mode = ExpectationMode.optimistic)
        {
            try
            {
                var matches = this.redis.As<AUDIO_ALARM>().GetAllKeys().Intersect(keys);
                if (matches.NullOrEmpty()) return false;
                return mode == ExpectationMode.pessimistic
                    ? matches.Count() == keys.Count()
                    : !matches.NullOrEmpty();
            }
            catch (ArgumentNullException) { throw; }
            catch (RedisResponseException) { throw; }
            catch (RedisException) { throw; }
            catch (InvalidOperationException) { throw; }
        }

        public void Save(AUDIO_ALARM entity)
        {
            this.manager.ExecTrans(transaction =>
            {
                try
                {
                    var keys = this.redis.As<AUDIO_ALARM>().GetAllKeys().ToArray();
                    if (!keys.NullOrEmpty()) this.redis.Watch(keys);

                    #region save attributes and  relations

                    if (entity.AttachmentBinary != null && entity.AttachmentBinary is ATTACH_BINARY)
                    {
                        var attachbin = entity.AttachmentBinary as ATTACH_BINARY;
                        transaction.QueueCommand(x => x.Store(attachbin));
                        var rattachbin = new REL_AALARMS_ATTACHBINS
                        {
                            Id = KeyGenerator.GetNextKey(),
                            AlarmId = entity.Id,
                            AttachmentId = attachbin.Id
                        };
                        var rclient = this.redis.As<REL_AALARMS_ATTACHBINS>();
                        var orattachbin = rclient.GetAll().Where(x => x.AlarmId.Equals(entity.Id, StringComparison.OrdinalIgnoreCase));
                        transaction.QueueCommand(x => rclient.StoreAll(
                            !orattachbin.NullOrEmpty()
                            ? rattachbin.ToSingleton().Except(orattachbin)
                            : rattachbin.ToSingleton()));
                    }

                    if (entity.AttachmentUri != null)
                    {
                        var attachuri = entity.AttachmentUri;
                        transaction.QueueCommand(x => x.Store(attachuri));
                        var rattachuri = new REL_AALARMS_ATTACHURIS
                        {
                            Id = KeyGenerator.GetNextKey(),
                            AlarmId = entity.Id,
                            AttachmentId = attachuri.Id
                        };
                        var rclient = this.redis.As<REL_AALARMS_ATTACHURIS>();
                        var orattachuri = rclient.GetAll().Where(x => x.AlarmId.Equals(entity.Id, StringComparison.OrdinalIgnoreCase));
                        transaction.QueueCommand(x => rclient.StoreAll(
                            !orattachuri.NullOrEmpty()
                            ? rattachuri.ToSingleton().Except(orattachuri)
                            : rattachuri.ToSingleton()));
                    }
                    
                    #endregion   
             
                    transaction.QueueCommand(x => this.redis.As<AUDIO_ALARM>().Store(this.Dehydrate(entity)));
                
                }
                catch (ArgumentNullException) { throw; }
                catch (RedisResponseException)
                {
                    try { transaction.Rollback(); }
                    catch (RedisResponseException) { throw; }
                    catch (RedisException) { throw; }
                    catch (Exception) { throw; }
                }
                catch (RedisException)
                {
                    try { transaction.Rollback(); }
                    catch (RedisResponseException) { throw; }
                    catch (RedisException) { throw; }
                    catch (Exception) { throw; }
                }
                catch (InvalidOperationException)
                {
                    try { transaction.Rollback(); }
                    catch (RedisResponseException) { throw; }
                    catch (RedisException) { throw; }
                    catch (Exception) { throw; }
                }
            });
        }

        public void Patch(AUDIO_ALARM source, Expression<Func<AUDIO_ALARM, object>> fields, IEnumerable<string> keys = null)
        {
            #region construct anonymous fields using expression lambdas

            var selection = fields.GetMemberNames();

            Expression<Func<AUDIO_ALARM, object>> primitives = x => new
            {
                x.Action,
                x.Duration,
                x.Trigger,
                x.Repeat
            };

            Expression<Func<AUDIO_ALARM, object>> relations = x => new
            {
                Attachment = x.AttachmentBinary
            };

            //4. Get list of selected relationals
            var srelation = relations.GetMemberNames().Intersect(selection, StringComparer.OrdinalIgnoreCase);

            //5. Get list of selected primitives
            var sprimitives = primitives.GetMemberNames().Intersect(selection, StringComparer.OrdinalIgnoreCase);

            #endregion

            this.manager.ExecTrans(transaction =>
            {
                try
                {
                    var aclient = this.redis.As<AUDIO_ALARM>();
                    var okeys = aclient.GetAllKeys().ToArray();
                    if (!okeys.NullOrEmpty()) this.redis.Watch(okeys);

                    #region save (insert or update) relational attributes

                    if (!srelation.NullOrEmpty())
                    {
                        Expression<Func<AUDIO_ALARM, object>> attachsexpr = y => y.AttachmentBinary;
                        if (selection.Contains(attachsexpr.GetMemberName()))
                        {
                            var attachbin = source.AttachmentBinary as ATTACH_BINARY;
                            if (attachbin != null)
                            {
                                transaction.QueueCommand(x => x.Store(attachbin));
                                var rattachbins = keys.Select(x => new REL_AALARMS_ATTACHBINS
                                {
                                    Id = KeyGenerator.GetNextKey(),
                                    AlarmId = x,
                                    AttachmentId = attachbin.Id
                                });

                                var orattachbins = this.redis.As<REL_AALARMS_ATTACHBINS>().GetAll().Where(x => keys.Contains(x.AlarmId));
                                transaction.QueueCommand(x =>
                                    x.StoreAll(!orattachbins.NullOrEmpty()
                                    ? rattachbins.Except(orattachbins)
                                    : rattachbins));
                            }

                            var attachuri = source.AttachmentUri;
                            if (attachuri != null)
                            {
                                transaction.QueueCommand(x => x.Store(attachuri));
                                var rattachuris= keys.Select(x => new REL_AALARMS_ATTACHURIS
                                {
                                    Id = KeyGenerator.GetNextKey(),
                                    AlarmId = x,
                                    AttachmentId = attachbin.Id
                                });

                                var orattachuris = this.redis.As<REL_AALARMS_ATTACHURIS>().GetAll().Where(x => keys.Contains(x.AlarmId));
                                transaction.QueueCommand(x =>
                                    x.StoreAll(!orattachuris.NullOrEmpty()
                                    ? rattachuris.Except(orattachuris)
                                    : rattachuris));
                            }
                        }
                    }

                    #endregion

                    #region update-only non-relational attributes

                    if (!sprimitives.NullOrEmpty())
                    {
                        Expression<Func<AUDIO_ALARM, object>> actionexpr = x => x.Action;
                        Expression<Func<AUDIO_ALARM, object>> repeatexpr = x => x.Repeat;
                        Expression<Func<AUDIO_ALARM, object>> durationexpr = x => x.Duration;
                        Expression<Func<AUDIO_ALARM, object>> triggerexpr = x => x.Trigger;

                        var entities = aclient.GetByIds(keys).ToList();
                        entities.ForEach(x =>
                        {
                            if (selection.Contains(actionexpr.GetMemberName())) x.Action = source.Action;
                            if (selection.Contains(repeatexpr.GetMemberName())) x.Repeat = source.Repeat;
                            if (selection.Contains(durationexpr.GetMemberName())) x.Duration = source.Duration;
                            if (selection.Contains(triggerexpr.GetMemberName())) x.Trigger = source.Trigger;
                        });

                        transaction.QueueCommand(x => x.StoreAll(this.Dehydrate(entities)));
                    }

                    #endregion

                }
                catch (ArgumentNullException) { throw; }
                catch (RedisResponseException)
                {
                    try { transaction.Rollback(); }
                    catch (RedisResponseException) { throw; }
                    catch (RedisException) { throw; }
                    catch (Exception) { throw; }
                }
                catch (RedisException)
                {
                    try { transaction.Rollback(); }
                    catch (RedisResponseException) { throw; }
                    catch (RedisException) { throw; }
                    catch (Exception) { throw; }
                }
                catch (InvalidOperationException)
                {
                    try { transaction.Rollback(); }
                    catch (RedisResponseException) { throw; }
                    catch (RedisException) { throw; }
                    catch (Exception) { throw; }
                }
            }); 
        }

        public void Erase(string key)
        {
            try
            {
                var aaclient = this.redis.As<AUDIO_ALARM>();
                if (aaclient.ContainsKey(key))
                {

                    var rattachbins = this.redis.As<REL_AALARMS_ATTACHBINS>().GetAll();
                    if (!rattachbins.NullOrEmpty()) this.redis.As<REL_AALARMS_ATTACHBINS>()
                        .DeleteByIds(rattachbins.Where(x => x.AlarmId.Equals(key, StringComparison.OrdinalIgnoreCase)));

                    var rattachuris = this.redis.As<REL_AALARMS_ATTACHURIS>().GetAll();
                    if (!rattachuris.NullOrEmpty()) this.redis.As<REL_AALARMS_ATTACHURIS>()
                        .DeleteByIds(rattachuris.Where(x => x.AlarmId.Equals(key, StringComparison.OrdinalIgnoreCase)));

                    aaclient.DeleteById(key);
                }
            }
            catch (ArgumentNullException) { throw; }
            catch (RedisResponseException) { throw; }
            catch (RedisException) { throw; }
            catch (InvalidOperationException) { throw; }
        }

        public void SaveAll(IEnumerable<AUDIO_ALARM> entities)
        {
            this.manager.ExecTrans(transaction =>
            {
                try
                {
                    var cclient = this.redis.As<AUDIO_ALARM>();

                    //watch keys
                    var okeys = cclient.GetAllKeys().ToArray();
                    if (!okeys.NullOrEmpty()) this.redis.Watch(okeys);

                    //save calendar
                    var keys = entities.Select(c => c.Id);
                    transaction.QueueCommand(x => x.StoreAll(entities));

                    //save attachments
                    var attachbins = entities.Where(x => x.AttachmentBinary != null && x.AttachmentBinary is ATTACH_BINARY)
                        .Select(x => x.AttachmentBinary as ATTACH_BINARY);
                    if (!attachbins.NullOrEmpty())
                    {
                        transaction.QueueCommand(x => x.StoreAll(attachbins));
                        var rattachbins = entities.Where(x => x.AttachmentBinary != null && x.AttachmentBinary is ATTACH_BINARY)
                            .Select(x => new REL_AALARMS_ATTACHBINS
                            {
                                Id = this.KeyGenerator.GetNextKey(),
                                AlarmId = x.Id,
                                AttachmentId = (x.AttachmentBinary as ATTACH_BINARY).Id
                            });

                        var orattachbins = this.redis.As<REL_AALARMS_ATTACHBINS>().GetAll()
                            .Where(x => keys.Contains(x.AlarmId));
                        transaction.QueueCommand(x => x.StoreAll(!orattachbins.NullOrEmpty()
                            ? rattachbins.Except(orattachbins)
                            : rattachbins));
                    }

                    var attachuris = entities.Where(x => x.AttachmentUri != null)
                        .Select(x => x.AttachmentUri);
                    if (!attachuris.NullOrEmpty())
                    {
                        transaction.QueueCommand(x => x.StoreAll(attachbins));
                        var rattachuris = entities.Where(x => x.AttachmentUri != null)
                            .Select(x => new REL_AALARMS_ATTACHURIS
                            {
                                Id = this.KeyGenerator.GetNextKey(),
                                AlarmId = x.Id,
                                AttachmentId = x.AttachmentUri.Id
                            });

                        var orattachuris = this.redis.As<REL_AALARMS_ATTACHURIS>().GetAll()
                            .Where(x => keys.Contains(x.AlarmId));
                        transaction.QueueCommand(x => x.StoreAll(!orattachuris.NullOrEmpty()
                            ? rattachuris.Except(orattachuris)
                            : rattachuris));
                    }

                    transaction.QueueCommand(x => x.StoreAll(this.Dehydrate(entities)));
                }
                catch (ArgumentNullException) { throw; }
                catch (RedisResponseException)
                {
                    try { transaction.Rollback(); }
                    catch (RedisResponseException) { throw; }
                    catch (RedisException) { throw; }
                    catch (Exception) { throw; }
                }
                catch (RedisException)
                {
                    try { transaction.Rollback(); }
                    catch (RedisResponseException) { throw; }
                    catch (RedisException) { throw; }
                    catch (Exception) { throw; }
                }
                catch (InvalidOperationException)
                {
                    try { transaction.Rollback(); }
                    catch (RedisResponseException) { throw; }
                    catch (RedisException) { throw; }
                    catch (Exception) { throw; }
                }
            });
        }

        public void EraseAll(IEnumerable<string> keys = null)
        {
            try
            {
                var aaclient = this.redis.As<AUDIO_ALARM>();
                var allkeys = aaclient.GetAllKeys().Select(x => UrnId.GetStringId(x));
                if (!allkeys.NullOrEmpty())
                {
                    var found = allkeys.Intersect(keys);
                    if (!found.NullOrEmpty())
                    {
                        var rattachbins = this.redis.As<REL_AALARMS_ATTACHBINS>().GetAll();
                        if (!rattachbins.NullOrEmpty()) this.redis.As<REL_AALARMS_ATTACHBINS>()
                            .DeleteByIds(rattachbins.Where(x => found.Contains(x.AlarmId, StringComparer.OrdinalIgnoreCase)));

                        var rattachuris = this.redis.As<REL_AALARMS_ATTACHURIS>().GetAll();
                        if (!rattachuris.NullOrEmpty()) this.redis.As<REL_AALARMS_ATTACHURIS>()
                            .DeleteByIds(rattachuris.Where(x => found.Contains(x.AlarmId, StringComparer.OrdinalIgnoreCase)));

                        aaclient.DeleteByIds(found);
                    }

                }
                else aaclient.DeleteAll();
            }
            catch (ArgumentNullException) { throw; }
            catch (RedisResponseException) { throw; }
            catch (RedisException) { throw; }
            catch (InvalidOperationException) { throw; }
        }

        public AUDIO_ALARM Hydrate(AUDIO_ALARM dry)
        {
            var full = dry;
            try
            {
                if (full != null)
                {
                    var rattachbins = this.redis.As<REL_AALARMS_ATTACHBINS>().GetAll().Where(x => x.AlarmId.Equals(full.Id, StringComparison.OrdinalIgnoreCase));
                    if (!rattachbins.NullOrEmpty()) full.AttachmentBinary = this.redis.As<ATTACH_BINARY>().GetById(rattachbins.FirstOrDefault().AttachmentId);

                    var rattachuris = this.redis.As<REL_AALARMS_ATTACHURIS>().GetAll().Where(x => x.AlarmId.Equals(full.Id, StringComparison.OrdinalIgnoreCase));
                    if (!rattachuris.NullOrEmpty()) full.AttachmentUri = this.redis.As<ATTACH_URI>().GetById(rattachuris.FirstOrDefault().AttachmentId);
                }
            }
            catch (ArgumentNullException) { throw; }
            catch (RedisResponseException) { throw; }
            catch (RedisException) { throw; }
            catch (InvalidOperationException) { throw; }
            return full ?? dry;
        }

        public IEnumerable<AUDIO_ALARM> Hydrate(IEnumerable<AUDIO_ALARM> dry)
        {
            var full = dry.ToList();
            var keys = full.Select(x => x.Id).Distinct().ToList();

            #region 1. retrieve relationships

            var rattachbins = this.redis.As<REL_EVENTS_ATTACHBINS>().GetAll().Where(x => keys.Contains(x.EventId)).ToList();
            var rattachuris = this.redis.As<REL_EVENTS_ATTACHURIS>().GetAll().Where(x => keys.Contains(x.EventId)).ToList();

            #endregion

            #region 2. retrieve secondary entities

            var attachbins = (!rattachbins.Empty()) ? this.redis.As<ATTACH_BINARY>().GetByIds(rattachbins.Select(x => x.AttachmentId)) : null;
            var attachuris = (!rattachuris.Empty()) ? this.redis.As<ATTACH_URI>().GetByIds(rattachuris.Select(x => x.AttachmentId)) : null;

            #endregion

            #region 3. Use Linq to stitch secondary entities to primary entities

            full.ForEach(x =>
            {

                if (!attachbins.NullOrEmpty())
                {
                    var xattachbins = from y in attachbins
                                      join r in rattachbins on y.Id equals r.AttachmentId
                                      join e in full on r.EventId equals e.Id
                                      where e.Id.Equals(x.Id, StringComparison.OrdinalIgnoreCase)
                                      select y;
                    if (!xattachbins.NullOrEmpty()) x.AttachmentBinary = xattachbins.FirstOrDefault();

                }

                if (!attachuris.NullOrEmpty())
                {
                    var xattachuris = from y in attachuris
                                      join r in rattachuris on y.Id equals r.AttachmentId
                                      join e in full on r.EventId equals e.Id
                                      where e.Id.Equals(x.Id, StringComparison.OrdinalIgnoreCase)
                                      select y;
                    if (!xattachuris.NullOrEmpty()) x.AttachmentUri = xattachuris.FirstOrDefault();
                }

            });

            #endregion

            return full ?? dry;
        }

        public AUDIO_ALARM Dehydrate(AUDIO_ALARM full)
        {
            try
            {
                full.AttachmentBinary = null;
            }
            catch (ArgumentNullException) { throw; }
            return full;        
        }

        public IEnumerable<AUDIO_ALARM> Dehydrate(IEnumerable<AUDIO_ALARM> full)
        {
            try
            {
                var pquery = full.AsParallel();
                pquery.ForAll(x => this.Dehydrate(x));
                return pquery.AsEnumerable();
            }
            catch (ArgumentNullException) { throw; }
            catch (OperationCanceledException) { throw; }
            catch (AggregateException) { throw; }
        }
    }

    public class DisplayAlarmRedisRepository: IDisplayAlarmRedisRepository
    {
        private IRedisClientsManager manager;
        private IKeyGenerator<string> keygen;
        private IRedisClient client = null;
        private IRedisClient redis
        {
            get
            {
                return (client != null) ? client : this.manager.GetClient();
            }
        }

        public IRedisClientsManager RedisClientsManager
        {
            get { return this.manager; }
            set
            {
                if (value == null) throw new ArgumentNullException("RedisClientsManager");
                this.manager = value;
                this.client = manager.GetClient();
            }
        }

        public IKeyGenerator<string> KeyGenerator
        {
            get { return this.keygen; }
            set
            {
                if (value == null) throw new ArgumentNullException("KeyGenerator");
                this.keygen = value;
            }
        }

        public DisplayAlarmRedisRepository() { }

        public DisplayAlarmRedisRepository(IRedisClientsManager manager)
        {
            this.RedisClientsManager = manager;
        }

        public DisplayAlarmRedisRepository(IRedisClient client)
        {
            if (client == null) throw new ArgumentNullException("IRedisClient");
            this.client = client;
        }

        public DISPLAY_ALARM Find(string key)
        {
            try
            {
                return this.redis.As<DISPLAY_ALARM>().GetById(key);

            }
            catch (ArgumentNullException) { throw; }
            catch (RedisResponseException) { throw; }
            catch (RedisException) { throw; }
            catch (InvalidOperationException) { throw; }
        }

        public IEnumerable<DISPLAY_ALARM> FindAll(IEnumerable<string> keys, int? skip = null, int? take = null)
        {
            try
            {
                var allkeys = this.redis.As<DISPLAY_ALARM>().GetAllKeys();
                if (skip == null && take == null)
                {
                    var filtered = !keys.NullOrEmpty()
                        ? allkeys.Intersect(keys.Select(x => UrnId.GetStringId(x)))
                        : new List<string>();
                    return this.redis.As<DISPLAY_ALARM>().GetByIds(filtered);
                }
                else
                {
                    var filtered = !keys.NullOrEmpty()
                        ? allkeys.Intersect(keys.Select(x => UrnId.GetStringId(x)))
                        : new List<string>();
                    return this.redis.As<DISPLAY_ALARM>().GetByIds(filtered);
                }
            }
            catch (ArgumentNullException) { throw; }
            catch (RedisResponseException) { throw; }
            catch (RedisException) { throw; }
            catch (InvalidOperationException) { throw; }
        }

        public IEnumerable<DISPLAY_ALARM> Get(int? skip = null, int? take = null)
        {
            try
            {
                if (skip == null && take == null) return this.redis.As<DISPLAY_ALARM>().GetAll();
                else
                {
                    var allkeys = this.redis.As<DISPLAY_ALARM>().GetAllKeys();
                    var selected = !allkeys.NullOrEmpty()
                        ? allkeys.Skip(skip.Value).Take(take.Value)
                        : allkeys;
                    return this.redis.As<DISPLAY_ALARM>().GetValues(selected.ToList());
                }
            }
            catch (ArgumentNullException) { throw; }
            catch (RedisResponseException) { throw; }
            catch (RedisException) { throw; }
            catch (InvalidOperationException) { throw; }
        }

        public bool ContainsKey(string key)
        {
            try
            {
                return this.redis.As<DISPLAY_ALARM>().ContainsKey(key);

            }
            catch (ArgumentNullException) { throw; }
            catch (RedisResponseException) { throw; }
            catch (RedisException) { throw; }
            catch (InvalidOperationException) { throw; }
        }

        public bool ContainsKeys(IEnumerable<string> keys, ExpectationMode mode = ExpectationMode.optimistic)
        {
            try
            {
                var matches = this.redis.As<DISPLAY_ALARM>().GetAllKeys().Intersect(keys);
                if (matches.NullOrEmpty()) return false;
                return mode == ExpectationMode.pessimistic
                    ? matches.Count() == keys.Count()
                    : !matches.NullOrEmpty();
            }
            catch (ArgumentNullException) { throw; }
            catch (RedisResponseException) { throw; }
            catch (RedisException) { throw; }
            catch (InvalidOperationException) { throw; }
        }

        public void Save(DISPLAY_ALARM entity)
        {
            this.manager.ExecTrans(transaction =>
            {
                try
                {
                    var keys = this.redis.As<DISPLAY_ALARM>().GetAllKeys().ToArray();
                    if (!keys.NullOrEmpty()) this.redis.Watch(keys);

                    transaction.QueueCommand(x => this.redis.As<DISPLAY_ALARM>().Store(entity));

                }
                catch (ArgumentNullException) { throw; }
                catch (RedisResponseException)
                {
                    try { transaction.Rollback(); }
                    catch (RedisResponseException) { throw; }
                    catch (RedisException) { throw; }
                    catch (Exception) { throw; }
                }
                catch (RedisException)
                {
                    try { transaction.Rollback(); }
                    catch (RedisResponseException) { throw; }
                    catch (RedisException) { throw; }
                    catch (Exception) { throw; }
                }
                catch (InvalidOperationException)
                {
                    try { transaction.Rollback(); }
                    catch (RedisResponseException) { throw; }
                    catch (RedisException) { throw; }
                    catch (Exception) { throw; }
                }
            });
        }

        public void Patch(DISPLAY_ALARM source, Expression<Func<DISPLAY_ALARM, object>> fields, IEnumerable<string> keys = null)
        {
            #region construct anonymous fields using expression lambdas

            var selection = fields.GetMemberNames();

            Expression<Func<DISPLAY_ALARM, object>> primitives = x => new
            {
                x.Action,
                x.Duration,
                x.Trigger,
                x.Repeat,
                x.Description
            };

            //4. Get list of selected primitives
            var sprimitives = primitives.GetMemberNames().Intersect(selection, StringComparer.OrdinalIgnoreCase);

            #endregion

            this.manager.ExecTrans(transaction =>
            {
                try
                {
                    var aclient = this.redis.As<DISPLAY_ALARM>();
                    var okeys = aclient.GetAllKeys().ToArray();
                    if (!okeys.NullOrEmpty()) this.redis.Watch(okeys);

                    #region update-only non-relational attributes

                    if (!sprimitives.NullOrEmpty())
                    {
                        Expression<Func<DISPLAY_ALARM, object>> actionexpr = x => x.Action;
                        Expression<Func<DISPLAY_ALARM, object>> repeatexpr = x => x.Repeat;
                        Expression<Func<DISPLAY_ALARM, object>> durationexpr = x => x.Duration;
                        Expression<Func<DISPLAY_ALARM, object>> triggerexpr = x => x.Trigger;
                        Expression<Func<DISPLAY_ALARM, object>> descexpr = x => x.Description;

                        var entities = aclient.GetByIds(keys).ToList();
                        entities.ForEach(x =>
                        {
                            if (selection.Contains(actionexpr.GetMemberName())) x.Action = source.Action;
                            if (selection.Contains(repeatexpr.GetMemberName())) x.Repeat = source.Repeat;
                            if (selection.Contains(durationexpr.GetMemberName())) x.Duration = source.Duration;
                            if (selection.Contains(triggerexpr.GetMemberName())) x.Trigger = source.Trigger;
                            if (selection.Contains(descexpr.GetMemberName())) x.Description = source.Description;
                        });

                        transaction.QueueCommand(x => x.StoreAll(entities));
                    }

                    #endregion

                }
                catch (ArgumentNullException) { throw; }
                catch (RedisResponseException)
                {
                    try { transaction.Rollback(); }
                    catch (RedisResponseException) { throw; }
                    catch (RedisException) { throw; }
                    catch (Exception) { throw; }
                }
                catch (RedisException)
                {
                    try { transaction.Rollback(); }
                    catch (RedisResponseException) { throw; }
                    catch (RedisException) { throw; }
                    catch (Exception) { throw; }
                }
                catch (InvalidOperationException)
                {
                    try { transaction.Rollback(); }
                    catch (RedisResponseException) { throw; }
                    catch (RedisException) { throw; }
                    catch (Exception) { throw; }
                }
            }); 
        }

        public void Erase(string key)
        {
            try
            {
                var daclient = this.redis.As<DISPLAY_ALARM>();
                if (daclient.ContainsKey(key)) daclient.DeleteById(key);
            }
            catch (ArgumentNullException) { throw; }
            catch (RedisResponseException) { throw; }
            catch (RedisException) { throw; }
            catch (InvalidOperationException) { throw; }
        }

        public void SaveAll(IEnumerable<DISPLAY_ALARM> entities)
        {
            this.manager.ExecTrans(transaction =>
            {
                try
                {
                    var daclient = this.redis.As<DISPLAY_ALARM>();

                    //watch keys
                    var okeys = daclient.GetAllKeys().ToArray();
                    if (!okeys.NullOrEmpty()) this.redis.Watch(okeys);

                    var keys = entities.Select(c => c.Id);
                    transaction.QueueCommand(x => x.StoreAll(entities));
                }
                catch (ArgumentNullException) { throw; }
                catch (RedisResponseException)
                {
                    try { transaction.Rollback(); }
                    catch (RedisResponseException) { throw; }
                    catch (RedisException) { throw; }
                    catch (Exception) { throw; }
                }
                catch (RedisException)
                {
                    try { transaction.Rollback(); }
                    catch (RedisResponseException) { throw; }
                    catch (RedisException) { throw; }
                    catch (Exception) { throw; }
                }
                catch (InvalidOperationException)
                {
                    try { transaction.Rollback(); }
                    catch (RedisResponseException) { throw; }
                    catch (RedisException) { throw; }
                    catch (Exception) { throw; }
                }
            });
        }

        public void EraseAll(IEnumerable<string> keys = null)
        {
            try
            {
                var daclient = this.redis.As<DISPLAY_ALARM>();
                var allkeys = daclient.GetAllKeys().Select(x => UrnId.GetStringId(x));
                if (!allkeys.NullOrEmpty())
                {
                    var found = allkeys.Intersect(keys);
                    if (!found.NullOrEmpty()) daclient.DeleteByIds(found);
                }
                else daclient.DeleteAll();
            }
            catch (ArgumentNullException) { throw; }
            catch (RedisResponseException) { throw; }
            catch (RedisException) { throw; }
            catch (InvalidOperationException) { throw; }
        }

    }

    public class EmailAlarmRedisRepository: IEmailAlarmRedisRepository
    {

        private IRedisClientsManager manager;
        private IKeyGenerator<string> keygen;
        private IRedisClient client = null;
        private IRedisClient redis
        {
            get
            {
                return (client != null) ? client : this.manager.GetClient();
            }
        }

        public IRedisClientsManager RedisClientsManager
        {
            get { return this.manager; }
            set
            {
                if (value == null) throw new ArgumentNullException("RedisClientsManager");
                this.manager = value;
                this.client = manager.GetClient();
            }
        }

        public IKeyGenerator<string> KeyGenerator
        {
            get { return this.keygen; }
            set
            {
                if (value == null) throw new ArgumentNullException("KeyGenerator");
                this.keygen = value;
            }
        }

        public EmailAlarmRedisRepository() { }

        public EmailAlarmRedisRepository(IRedisClientsManager manager)
        {
            this.RedisClientsManager = manager;
        }

        public EmailAlarmRedisRepository(IRedisClient client)
        {
            if (client == null) throw new ArgumentNullException("IRedisClient");
            this.client = client;
        }

        public EMAIL_ALARM Hydrate(EMAIL_ALARM dry)
        {
            try
            {
                var full = dry;
                var cclient = this.redis.As<EMAIL_ALARM>();
                full = cclient.GetValue(dry.Id);
                if (full != null)
                {
                    var rattendees = this.redis.As<REL_EALARMS_ATTENDEES>().GetAll().Where(x => x.AlarmId == full.Id);
                    var rattachbins = this.redis.As<REL_EALARMS_ATTACHBINS>().GetAll().Where(x => x.AlarmId == full.Id);
                    var rattachuris = this.redis.As<REL_EALARMS_ATTACHURIS>().GetAll().Where(x => x.AlarmId == full.Id);

                    if (!rattachbins.NullOrEmpty())
                    {
                        full.AttachmentBinaries.AddRangeComplement(this.redis.As<ATTACH_BINARY>().GetValues(rattachbins.Select(x => x.AttachmentId).ToList()));
                    }
                    if (!rattachuris.NullOrEmpty())
                    {
                        full.AttachmentUris.AddRangeComplement(this.redis.As<ATTACH_URI>().GetValues(rattachuris.Select(x => x.AttachmentId).ToList()));
                    }
                    if (!rattendees.NullOrEmpty())
                    {
                        full.Attendees.AddRangeComplement(this.redis.As<ATTENDEE>().GetValues(rattendees.Select(x => x.AttendeeId).ToList()));
                    }

                }
                return full ?? dry;
            }
            catch (ArgumentNullException) { throw; }
            catch (RedisResponseException) { throw; }
            catch (RedisException) { throw; }
            catch (InvalidOperationException) { throw; }
        }

        public IEnumerable<EMAIL_ALARM> HydrateAll(IEnumerable<EMAIL_ALARM> dry)
        {
            try
            {
                var full = dry.ToList();
                var eclient = this.redis.As<EMAIL_ALARM>();
                var keys = dry.Select(x => x.Id).Distinct().ToList();
                if (eclient.GetAllKeys().Intersect(keys).Count() == keys.Count()) //all keys are found
                {
                    full = eclient.GetValues(keys);

                    #region 1. retrieve relationships

                    var rattachbins = this.redis.As<REL_EVENTS_ATTACHBINS>().GetAll().Where(x => keys.Contains(x.EventId));
                    var rattachuris = this.redis.As<REL_EVENTS_ATTACHURIS>().GetAll().Where(x => keys.Contains(x.EventId));
                    var rattendees = this.redis.As<REL_EVENTS_ATTENDEES>().GetAll().Where(x => keys.Contains(x.EventId));
                    #endregion

                    #region 2. retrieve secondary entities

                    var attachbins = (!rattachbins.Empty()) ? this.redis.As<ATTACH_BINARY>().GetValues(rattachbins.Select(x => x.AttachmentId).ToList()) : null;
                    var attachuris = (!rattachuris.Empty()) ? this.redis.As<ATTACH_URI>().GetValues(rattachuris.Select(x => x.AttachmentId).ToList()) : null;
                    var attendees = (!rattendees.Empty()) ? this.redis.As<ATTENDEE>().GetValues(rattendees.Select(x => x.AttendeeId).ToList()) : null;

                    #endregion

                    #region 3. Use Linq to stitch secondary entities to primary entities

                    full.ForEach(x =>
                    {

                        if (!attendees.NullOrEmpty())
                        {
                            var xattendees = from y in attendees
                                             join r in rattendees on y.Id equals r.AttendeeId
                                             join e in full on r.EventId equals e.Id
                                             where e.Id == x.Id
                                             select y;
                            if (!xattendees.NullOrEmpty()) x.Attendees.AddRange(xattendees);
                        }

                        if (!attachbins.NullOrEmpty())
                        {
                            var xattachbins = from y in attachbins
                                              join r in rattachbins on y.Id equals r.AttachmentId
                                              join e in full on r.EventId equals e.Id
                                              where e.Id == x.Id
                                              select y;
                            if (!xattachbins.NullOrEmpty()) x.AttachmentBinaries.AddRangeComplement(xattachbins);

                        }

                        if (!attachuris.NullOrEmpty())
                        {
                            var xattachuris = from y in attachuris
                                              join r in rattachuris on y.Id equals r.AttachmentId
                                              join e in full on r.EventId equals e.Id
                                              where e.Id == x.Id
                                              select y;
                            if (!xattachuris.NullOrEmpty()) x.AttachmentUris.AddRangeComplement(xattachuris);
                        }

                    });
                    #endregion
                }

                return full ?? dry;
            }
            catch (ArgumentNullException) { throw; }
            catch (RedisResponseException) { throw; }
            catch (RedisException) { throw; }
            catch (InvalidOperationException) { throw; }
        }

        public EMAIL_ALARM Find(string key)
        {
            try
            {
                var dry = this.redis.As<EMAIL_ALARM>().GetById(key);
                return dry != null ? this.Hydrate(dry) : dry;

            }
            catch (ArgumentNullException) { throw; }
            catch (RedisResponseException) { throw; }
            catch (RedisException) { throw; }
            catch (InvalidOperationException) { throw; }
        }

        public IEnumerable<EMAIL_ALARM> FindAll(IEnumerable<string> keys, int? skip = null, int? take = null)
        {
            try
            {
                var allkeys = this.redis.As<EMAIL_ALARM>().GetAllKeys();
                if (skip == null && take == null)
                {
                    var filtered = !keys.NullOrEmpty()
                        ? allkeys.Intersect(keys.Select(x => UrnId.GetStringId(x)))
                        : new List<string>();
                    var dry = this.redis.As<EMAIL_ALARM>().GetByIds(filtered);
                    return !dry.NullOrEmpty() ? this.HydrateAll(dry) : dry;
                }
                else
                {
                    var filtered = !keys.NullOrEmpty()
                        ? allkeys.Intersect(keys.Select(x => UrnId.GetStringId(x)))
                        : new List<string>();
                    var dry = this.redis.As<EMAIL_ALARM>().GetByIds(filtered);
                    return !dry.NullOrEmpty() ? this.HydrateAll(dry) : dry;
                }
            }
            catch (ArgumentNullException) { throw; }
            catch (RedisResponseException) { throw; }
            catch (RedisException) { throw; }
            catch (InvalidOperationException) { throw; }
        }

        public IEnumerable<EMAIL_ALARM> Get(int? skip = null, int? take = null)
        {
            try
            {
                if (skip == null && take == null) return this.redis.As<EMAIL_ALARM>().GetAll();
                else
                {
                    var allkeys = this.redis.As<DISPLAY_ALARM>().GetAllKeys();
                    var selected = !allkeys.NullOrEmpty()
                        ? allkeys.Skip(skip.Value).Take(take.Value)
                        : allkeys;
                    var dry = this.redis.As<EMAIL_ALARM>().GetValues(selected.ToList());
                    return !dry.NullOrEmpty() ? this.HydrateAll(dry) : dry;
                }
            }
            catch (ArgumentNullException) { throw; }
            catch (RedisResponseException) { throw; }
            catch (RedisException) { throw; }
            catch (InvalidOperationException) { throw; }
        }

        public bool ContainsKey(string key)
        {
            try
            {
                return this.redis.As<EMAIL_ALARM>().ContainsKey(key);
            }
            catch (ArgumentNullException) { throw; }
            catch (RedisResponseException) { throw; }
            catch (RedisException) { throw; }
            catch (InvalidOperationException) { throw; }
        }

        public bool ContainsKeys(IEnumerable<string> keys, ExpectationMode mode = ExpectationMode.optimistic)
        {
            try
            {
                var matches = this.redis.As<EMAIL_ALARM>().GetAllKeys().Intersect(keys);
                if (matches.NullOrEmpty()) return false;
                return mode == ExpectationMode.pessimistic
                    ? matches.Count() == keys.Count()
                    : !matches.NullOrEmpty();
            }
            catch (ArgumentNullException) { throw; }
            catch (RedisResponseException) { throw; }
            catch (RedisException) { throw; }
            catch (InvalidOperationException) { throw; }
        }

        public void Save(EMAIL_ALARM entity)
        {
            this.manager.ExecTrans(transaction =>
            {
                try
                {

                    #region retrieve attributes of entity

                    var attendees = entity.Attendees;
                    var attachbins = entity.AttachmentBinaries;
                    var attachuris = entity.AttachmentUris;

                    #endregion

                    #region save attributes and  relations

                    if (!attendees.NullOrEmpty())
                    {
                        transaction.QueueCommand(x => x.StoreAll(attendees));
                        var rattendees = attendees.Select(x => new REL_EALARMS_ATTENDEES
                        {
                            Id = KeyGenerator.GetNextKey(),
                            AlarmId = entity.Id,
                            AttendeeId = x.Id
                        });

                        var rclient = this.redis.As<REL_EALARMS_ATTENDEES>();
                        var orattendees = rclient.GetAll().Where(x => x.AlarmId == entity.Id);
                        transaction.QueueCommand(x => x.StoreAll(
                            !orattendees.NullOrEmpty()
                            ? rattendees.Except(orattendees)
                            : rattendees));
                    }

                    if (!attachbins.NullOrEmpty())
                    {
                        transaction.QueueCommand(x => x.StoreAll(attachbins));
                        var rattachbins = attachbins.Select(x => new REL_EALARMS_ATTACHBINS
                        {
                            Id = KeyGenerator.GetNextKey(),
                            AlarmId = entity.Id,
                            AttachmentId = x.Id
                        });

                        var rclient = this.redis.As<REL_EALARMS_ATTACHBINS>();
                        var orattachbins = rclient.GetAll().Where(x => x.AlarmId == entity.Id);
                        transaction.QueueCommand(x => x.StoreAll(
                            !orattachbins.NullOrEmpty()
                            ? rattachbins.Except(orattachbins)
                            : rattachbins));
                    }

                    if (!attachuris.NullOrEmpty())
                    {
                        transaction.QueueCommand(x => x.StoreAll(attachuris));
                        var rattachuris = attachuris.Select(x => new REL_EALARMS_ATTACHURIS
                        {
                            Id = KeyGenerator.GetNextKey(),
                            AlarmId = entity.Id,
                            AttachmentId = x.Id
                        });

                        var rclient = this.redis.As<REL_EALARMS_ATTACHURIS>();
                        var orattachuris = rclient.GetAll().Where(x => x.AlarmId == entity.Id);
                        transaction.QueueCommand(x => x.StoreAll(
                            !orattachuris.NullOrEmpty()
                            ? rattachuris.Except(orattachuris)
                            : rattachuris));
                    }

                    #endregion


                    transaction.QueueCommand(x => x.Store(this.Dehydrate(entity)));

                }
                catch (ArgumentNullException) { throw; }
                catch (RedisResponseException)
                {
                    try { transaction.Rollback(); }
                    catch (RedisResponseException) { throw; }
                    catch (RedisException) { throw; }
                    catch (Exception) { throw; }
                }
                catch (RedisException)
                {
                    try { transaction.Rollback(); }
                    catch (RedisResponseException) { throw; }
                    catch (RedisException) { throw; }
                    catch (Exception) { throw; }
                }
                catch (InvalidOperationException)
                {
                    try { transaction.Rollback(); }
                    catch (RedisResponseException) { throw; }
                    catch (RedisException) { throw; }
                    catch (Exception) { throw; }
                }
            });
        }

        public void Patch(EMAIL_ALARM source, Expression<Func<EMAIL_ALARM, object>> fields, IEnumerable<string> keys = null)
        {
            #region construct anonymous fields using expression lambdas

            var selection = fields.GetMemberNames();

            Expression<Func<EMAIL_ALARM, object>> primitives = x => new
            {
                x.Action,
                x.Repeat,
                x.Trigger,
                x.Description,
                x.Duration
            };

            Expression<Func<EMAIL_ALARM, object>> relations = x => new
            {
                x.AttachmentBinaries,
                x.AttachmentUris,
                x.Attendees
            };

            //4. Get list of selected relationals
            var srelation = relations.GetMemberNames().Intersect(selection, StringComparer.OrdinalIgnoreCase).Distinct(StringComparer.OrdinalIgnoreCase);

            //5. Get list of selected primitives
            var sprimitives = primitives.GetMemberNames().Intersect(selection, StringComparer.OrdinalIgnoreCase).Distinct(StringComparer.OrdinalIgnoreCase);

            #endregion

            this.manager.ExecTrans(transaction =>
            {

                try
                {
                    var eaclient = this.redis.As<EMAIL_ALARM>();
                    var okeys = eaclient.GetAllKeys().ToArray();
                    if (!okeys.NullOrEmpty()) this.redis.Watch(okeys);

                    #region save (insert or update) relational attributes

                    if (!srelation.NullOrEmpty())
                    {
                        Expression<Func<EMAIL_ALARM, object>> attendsexpr = y => y.Attendees;
                        Expression<Func<EMAIL_ALARM, object>> attachbinsexpr = y => y.AttachmentBinaries;
                        Expression<Func<EMAIL_ALARM, object>> attachurisexpr = y => y.AttachmentUris;


                        if (selection.Contains(attendsexpr.GetMemberName()))
                        {
                            var attendees = source.Attendees.OfType<ATTENDEE>();
                            if (!attendees.NullOrEmpty())
                            {
                                transaction.QueueCommand(x => this.redis.As<ATTENDEE>().StoreAll(attendees));
                                var rattendees = keys.SelectMany(x => attendees.Select(y => new REL_EALARMS_ATTENDEES
                                {
                                    Id = KeyGenerator.GetNextKey(),
                                    AlarmId = x,
                                    AttendeeId = y.Id
                                }));

                                var rclient = this.redis.As<REL_EALARMS_ATTENDEES>();
                                var orattendees = rclient.GetAll().Where(x => x.AlarmId == source.Id);
                                transaction.QueueCommand(x => rclient.StoreAll(
                                    !orattendees.NullOrEmpty()
                                    ? rattendees.Except(orattendees)
                                    : rattendees));
                            }
                        }

                        if (selection.Contains(attachbinsexpr.GetMemberName()))
                        {
                            var attachbins = source.AttachmentBinaries;
                            if (!attachbins.NullOrEmpty())
                            {
                                transaction.QueueCommand(x => this.redis.As<ATTACH_BINARY>().StoreAll(attachbins));
                                var rattachbins = keys.SelectMany(x => attachbins.Select(y => new REL_EALARMS_ATTACHBINS
                                {
                                    Id = KeyGenerator.GetNextKey(),
                                    AlarmId = x,
                                    AttachmentId = y.Id
                                }));

                                var rclient = this.redis.As<REL_EALARMS_ATTACHBINS>();
                                var orattachbins = rclient.GetAll().Where(x => keys.Contains(x.AlarmId));
                                transaction.QueueCommand(x => rclient.StoreAll(!orattachbins.NullOrEmpty()
                                    ? rattachbins.Except(orattachbins)
                                    : rattachbins));
                            }

                        }

                        if(selection.Contains(attachurisexpr.GetMemberName()))
                        {
                            var attachuris = source.AttachmentUris;
                            if (!attachuris.NullOrEmpty())
                            {
                                transaction.QueueCommand(x => this.redis.As<ATTACH_URI>().StoreAll(attachuris));
                                var rattachuris = keys.SelectMany(x => attachuris.Select(y => new REL_EALARMS_ATTACHURIS
                                {
                                    Id = this.KeyGenerator.GetNextKey(),
                                    AlarmId = x,
                                    AttachmentId = y.Id
                                }));
                                var rclient = this.redis.As<REL_EALARMS_ATTACHURIS>();
                                var orattachuris = rclient.GetAll().Where(x => keys.Contains(x.AlarmId));
                                transaction.QueueCommand(x => rclient.StoreAll(!orattachuris.NullOrEmpty()
                                    ? rattachuris.Except(orattachuris)
                                    : rattachuris));
                            }
                        }
                    }

                    #endregion

                    #region save (insert or update) non-relational attributes

                    if (!sprimitives.NullOrEmpty())
                    {
                        Expression<Func<EMAIL_ALARM, object>> actionexpr = x => x.Action;
                        Expression<Func<EMAIL_ALARM, object>> repeatexpr = x => x.Repeat;
                        Expression<Func<EMAIL_ALARM, object>> durationexpr = x => x.Duration;
                        Expression<Func<EMAIL_ALARM, object>> triggerexpr = x => x.Trigger;
                        Expression<Func<EMAIL_ALARM, object>> descexpr = x => x.Description;
                        Expression<Func<EMAIL_ALARM, object>> summexpr = x => x.Summary;

                        var entities = eaclient.GetByIds(keys).ToList();
                        entities.ForEach(x =>
                        {
                            if (selection.Contains(actionexpr.GetMemberName())) x.Action = source.Action;
                            if (selection.Contains(repeatexpr.GetMemberName())) x.Repeat = source.Repeat;
                            if (selection.Contains(durationexpr.GetMemberName())) x.Duration = source.Duration;
                            if (selection.Contains(triggerexpr.GetMemberName())) x.Trigger = source.Trigger;
                            if (selection.Contains(descexpr.GetMemberName())) x.Description = source.Description;
                            if (selection.Contains(descexpr.GetMemberName())) x.Summary = source.Summary;
                        });

                        transaction.QueueCommand(x => x.StoreAll(this.Dehydrate(entities)));
                    }

                    #endregion
                }
                catch (ArgumentNullException) { throw; }
                catch (RedisResponseException)
                {
                    try { transaction.Rollback(); }
                    catch (RedisResponseException) { throw; }
                    catch (RedisException) { throw; }
                    catch (Exception) { throw; }
                }
                catch (RedisException)
                {
                    try { transaction.Rollback(); }
                    catch (RedisResponseException) { throw; }
                    catch (RedisException) { throw; }
                    catch (Exception) { throw; }
                }
                catch (InvalidOperationException)
                {
                    try { transaction.Rollback(); }
                    catch (RedisResponseException) { throw; }
                    catch (RedisException) { throw; }
                    catch (Exception) { throw; }
                }
            });


            

        }

        public void Erase(string key)
        {
            try
            {
                var eclient = this.redis.As<EMAIL_ALARM>();
                if (eclient.ContainsKey(key))
                {
                    eclient.DeleteRelatedEntities<REL_EALARMS_ATTACHBINS>(key);
                    eclient.DeleteRelatedEntities<REL_EALARMS_ATTACHURIS>(key);
                    eclient.DeleteRelatedEntities<REL_EALARMS_ATTENDEES>(key);
                    eclient.DeleteById(key);
                }
            }
            catch (ArgumentNullException) { throw; }
            catch (RedisResponseException) { throw; }
            catch (RedisException) { throw; }
            catch (InvalidOperationException) { throw; }
        }

        public void SaveAll(IEnumerable<EMAIL_ALARM> entities)
        {
            #region 1. retrieve attributes of entities

            var attendees = entities.Where(x => !x.Attendees.NullOrEmpty()).SelectMany(x => x.Attendees);
            var attachbins = entities.Where(x => !x.AttachmentBinaries.NullOrEmpty()).SelectMany(x => x.AttachmentBinaries);
            var attachuris = entities.Where(x => !x.AttachmentUris.NullOrEmpty()).SelectMany(x => x.AttachmentUris);

            #endregion

            #region 2. save aggregate attribbutes of entities

            using (var transaction = this.redis.CreateTransaction())
            {
                try
                {
                    transaction.QueueCommand(x => x.StoreAll(entities));
                    var keys = entities.Select(x => x.Id).ToArray();
                    if (!attendees.NullOrEmpty())
                    {
                        transaction.QueueCommand(x => x.StoreAll(attendees));
                        var rattendees = entities.Where(x => !x.Attendees.NullOrEmpty())
                            .SelectMany(e => e.Attendees.Select(x => new REL_EALARMS_ATTENDEES
                                {
                                    Id = this.KeyGenerator.GetNextKey(),
                                    AlarmId = e.Id,
                                    AttendeeId = x.Id
                                }));

                        var rclient = this.redis.As<REL_EALARMS_ATTENDEES>();
                        var orattendees = rclient.GetAll().Where(x => keys.Contains(x.AlarmId));
                        transaction.QueueCommand(x => x.StoreAll((!orattendees.NullOrEmpty())
                            ? rattendees.Except(orattendees)
                            : rattendees));
                    }

                    if (!attachbins.NullOrEmpty())
                    {
                        transaction.QueueCommand(x => x.StoreAll(attachbins));
                        var rattachbins = entities.Where(x => !x.AttachmentBinaries.NullOrEmpty())
                            .SelectMany(e => e.AttachmentBinaries.Select(x => new REL_EALARMS_ATTACHBINS
                                {
                                    Id = this.KeyGenerator.GetNextKey(),
                                    AlarmId = e.Id,
                                    AttachmentId = x.Id
                                }));
                        var rclient = this.redis.As<REL_EALARMS_ATTACHBINS>();
                        var orattachbins = rclient.GetAll().Where(x => keys.Contains(x.AlarmId));
                        transaction.QueueCommand(x => x.StoreAll((!orattachbins.NullOrEmpty())
                            ? rattachbins.Except(orattachbins)
                            : rattachbins));
                    }

                    if (!attachuris.NullOrEmpty())
                    {
                        transaction.QueueCommand(x => x.StoreAll(attachuris));
                        var rattachuris = entities.Where(x => !x.AttachmentUris.NullOrEmpty())
                            .SelectMany(e => e.AttachmentUris.Select(x => new REL_EALARMS_ATTACHURIS
                                {
                                    Id = this.KeyGenerator.GetNextKey(),
                                    AlarmId = e.Id,
                                    AttachmentId = x.Id
                                }));
                        var rclient = this.redis.As<REL_EALARMS_ATTACHURIS>();
                        var orattachuris = rclient.GetAll().Where(x => keys.Contains(x.AlarmId));
                        transaction.QueueCommand(x => x.StoreAll((!orattachuris.NullOrEmpty())
                            ? rattachuris.Except(orattachuris)
                            : rattachuris));
                    }

                    transaction.Commit();
                }
                catch (Exception)
                {
                    try { transaction.Rollback(); }
                    catch (Exception) { throw; }
                }
            }

            #endregion        
        }

        public void EraseAll(IEnumerable<string> keys = null)
        {
            try
            {
                var eaclient = this.redis.As<EMAIL_ALARM>();
                var allkeys = eaclient.GetAllKeys().Select(x => UrnId.GetStringId(x));
                if (!allkeys.NullOrEmpty())
                {
                    var found = allkeys.Intersect(keys);
                    if (!found.NullOrEmpty())
                    {
                        var rattachbins = this.redis.As<REL_EALARMS_ATTACHBINS>().GetAll();
                        if (!rattachbins.NullOrEmpty()) this.redis.As<REL_EALARMS_ATTACHBINS>()
                            .DeleteByIds(rattachbins.Where(x => found.Contains(x.AlarmId, StringComparer.OrdinalIgnoreCase)));

                        var rattachuris = this.redis.As<REL_EALARMS_ATTACHURIS>().GetAll();
                        if (!rattachuris.NullOrEmpty()) this.redis.As<REL_EALARMS_ATTACHURIS>()
                            .DeleteByIds(rattachuris.Where(x => found.Contains(x.AlarmId, StringComparer.OrdinalIgnoreCase)));

                        var rattendees = this.redis.As<REL_EALARMS_ATTENDEES>().GetAll();
                        if (!rattendees.NullOrEmpty()) this.redis.As<REL_EALARMS_ATTENDEES>()
                            .DeleteByIds(rattendees.Where(x => found.Contains(x.AlarmId, StringComparer.OrdinalIgnoreCase)));

                        eaclient.DeleteByIds(found);
                    }
                }
                else eaclient.DeleteAll();
            }
            catch (ArgumentNullException) { throw; }
            catch (RedisResponseException) { throw; }
            catch (RedisException) { throw; }
            catch (InvalidOperationException) { throw; }
        }

        public EMAIL_ALARM Dehydrate(EMAIL_ALARM full)
        {
            try
            {
                if (!full.Attendees.NullOrEmpty()) full.Attendees.Clear();
                if (!full.AttachmentBinaries.NullOrEmpty()) full.AttachmentBinaries.Clear();
                if (!full.AttachmentUris.NullOrEmpty()) full.AttachmentUris.Clear();
                return full;
            }
            catch (ArgumentNullException) { throw; }

        }

        public IEnumerable<EMAIL_ALARM> Dehydrate(IEnumerable<EMAIL_ALARM> full)
        {
            try
            {
                var pquery = full.AsParallel();
                pquery.ForAll(x => this.Dehydrate(x));
                return pquery.AsEnumerable();
            }
            catch (ArgumentNullException) { throw; }
            catch (OperationCanceledException) { throw; }
            catch (AggregateException) { throw; }
        }
    }
}