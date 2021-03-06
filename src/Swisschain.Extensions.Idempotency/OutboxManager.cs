﻿using System;
using System.Threading.Tasks;

namespace Swisschain.Extensions.Idempotency
{
    internal sealed class OutboxManager : IOutboxManager
    {
        private readonly IOutboxDispatcher _dispatcher;
        private readonly IOutboxRepository _repository;

        public OutboxManager(IOutboxDispatcher dispatcher,
            IOutboxRepository repository)
        {
            _dispatcher = dispatcher;
            _repository = repository;
        }

        public Task<Outbox> Open(string requestId, Func<Task<long>> aggregateIdFactory)
        {
            return _repository.Open(requestId, aggregateIdFactory);
        }

        public Task<Outbox> Open(string requestId)
        {
            return _repository.Open(requestId, OutboxAggregateIdGenerator.None);
        }

        public async Task Store(Outbox outbox)
        {
            await _repository.Save(outbox, OutboxPersistingReason.Storing);

            outbox.IsStored = true;
        }

        public async Task EnsureDispatched(Outbox outbox)
        {
            if (outbox.IsDispatched)
            {
                return;
            }

            foreach (var command in outbox.Commands)
            {
                await _dispatcher.Send(command);
            }

            foreach (var evt in outbox.Events)
            {
                await _dispatcher.Publish(evt);
            }

            await _repository.Save(outbox, OutboxPersistingReason.Dispatching);

            outbox.IsDispatched = true;
        }

        public async Task EnsureDispatched(Outbox outbox, IOutboxDispatcher dispatcher)
        {
            if (outbox.IsDispatched)
            {
                return;
            }

            foreach (var command in outbox.Commands)
            {
                await dispatcher.Send(command);
            }

            foreach (var evt in outbox.Events)
            {
                await dispatcher.Publish(evt);
            }

            await _repository.Save(outbox, OutboxPersistingReason.Dispatching);

            outbox.IsDispatched = true;
        }
    }
}
