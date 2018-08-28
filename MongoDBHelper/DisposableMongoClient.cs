﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Core.Clusters;

namespace BindDns.MongoDBEntity
{
    public class DisposableMongoClient : IMongoClient, IDisposable
    {
        private readonly IMongoClient wrapped;

        public DisposableMongoClient(IMongoClient wrapped)
        {
            this.wrapped = wrapped;
        }

        public ICluster Cluster => wrapped.Cluster;

        public MongoClientSettings Settings => wrapped.Settings;

        public IMongoClient Wrapped => wrapped;

        public void DropDatabase(string name, CancellationToken cancellationToken = default(CancellationToken))
        {
            wrapped.DropDatabase(name, cancellationToken);
        }

        public void DropDatabase(IClientSessionHandle session, string name, CancellationToken cancellationToken = default(CancellationToken))
        {
            wrapped.DropDatabase(session, name, cancellationToken);
        }

        public Task DropDatabaseAsync(string name, CancellationToken cancellationToken = default(CancellationToken))
        {
            return wrapped.DropDatabaseAsync(name, cancellationToken);
        }

        public Task DropDatabaseAsync(IClientSessionHandle session, string name, CancellationToken cancellationToken = default(CancellationToken))
        {
            return wrapped.DropDatabaseAsync(session, name, cancellationToken);
        }

        public IMongoDatabase GetDatabase(string name, MongoDatabaseSettings settings = null)
        {
            return wrapped.GetDatabase(name, settings);
        }

        public IAsyncCursor<string> ListDatabaseNames(
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return wrapped.ListDatabaseNames(cancellationToken);
        }

        public IAsyncCursor<string> ListDatabaseNames(
            IClientSessionHandle session,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return wrapped.ListDatabaseNames(session, cancellationToken);
        }

        public Task<IAsyncCursor<string>> ListDatabaseNamesAsync(
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return wrapped.ListDatabaseNamesAsync(cancellationToken);
        }

        public Task<IAsyncCursor<string>> ListDatabaseNamesAsync(
            IClientSessionHandle session,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return wrapped.ListDatabaseNamesAsync(session, cancellationToken);
        }

        public IAsyncCursor<BsonDocument> ListDatabases(
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return wrapped.ListDatabases(cancellationToken);
        }

        public IAsyncCursor<BsonDocument> ListDatabases(
            ListDatabasesOptions options = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return wrapped.ListDatabases(options, cancellationToken);
        }

        public IAsyncCursor<BsonDocument> ListDatabases(
            IClientSessionHandle session,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return wrapped.ListDatabases(session, cancellationToken);
        }

        public IAsyncCursor<BsonDocument> ListDatabases(
            IClientSessionHandle session,
            ListDatabasesOptions options,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return wrapped.ListDatabases(session, options, cancellationToken);
        }


        public Task<IAsyncCursor<BsonDocument>> ListDatabasesAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            return wrapped.ListDatabasesAsync(cancellationToken);
        }

        public Task<IAsyncCursor<BsonDocument>> ListDatabasesAsync(
            ListDatabasesOptions options = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return wrapped.ListDatabasesAsync(options, cancellationToken);
        }

        public Task<IAsyncCursor<BsonDocument>> ListDatabasesAsync(IClientSessionHandle session, CancellationToken cancellationToken = default(CancellationToken))
        {
            return wrapped.ListDatabasesAsync(session, cancellationToken);
        }

        public Task<IAsyncCursor<BsonDocument>> ListDatabasesAsync(
            IClientSessionHandle session,
            ListDatabasesOptions options,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return wrapped.ListDatabasesAsync(session, options, cancellationToken);
        }

        public IClientSessionHandle StartSession(ClientSessionOptions options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return wrapped.StartSession(options, cancellationToken);
        }

        public Task<IClientSessionHandle> StartSessionAsync(ClientSessionOptions options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return wrapped.StartSessionAsync(options, cancellationToken);
        }

        /// <inheritdoc />
        public virtual IAsyncCursor<TResult> Watch<TResult>(
            PipelineDefinition<ChangeStreamDocument<BsonDocument>, TResult> pipeline,
            ChangeStreamOptions options = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return wrapped.Watch(pipeline, options, cancellationToken);
        }

        /// <inheritdoc />
        public virtual IAsyncCursor<TResult> Watch<TResult>(
            IClientSessionHandle session,
            PipelineDefinition<ChangeStreamDocument<BsonDocument>, TResult> pipeline,
            ChangeStreamOptions options = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return wrapped.Watch(session, pipeline, options, cancellationToken);
        }

        /// <inheritdoc />
        public virtual Task<IAsyncCursor<TResult>> WatchAsync<TResult>(
            PipelineDefinition<ChangeStreamDocument<BsonDocument>, TResult> pipeline,
            ChangeStreamOptions options = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return wrapped.WatchAsync(pipeline, options, cancellationToken);
        }

        /// <inheritdoc />
        public virtual Task<IAsyncCursor<TResult>> WatchAsync<TResult>(
            IClientSessionHandle session,
            PipelineDefinition<ChangeStreamDocument<BsonDocument>, TResult> pipeline,
            ChangeStreamOptions options = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return wrapped.WatchAsync(session, pipeline, options, cancellationToken);
        }

        public IMongoClient WithReadConcern(ReadConcern readConcern)
        {
            return wrapped.WithReadConcern(readConcern);
        }

        public IMongoClient WithReadPreference(ReadPreference readPreference)
        {
            return wrapped.WithReadPreference(readPreference);
        }

        public IMongoClient WithWriteConcern(WriteConcern writeConcern)
        {
            return wrapped.WithWriteConcern(writeConcern);
        }

        public void Dispose()
        {
            ClusterRegistry.Instance.UnregisterAndDisposeCluster(wrapped.Cluster);
        }
    }
}
