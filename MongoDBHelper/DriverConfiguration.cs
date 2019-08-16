using System;
using MongoDB.Driver;
using MongoDB.Driver.Core;
using MongoDB.Driver.Core.Configuration;

namespace BindDns.MongoDBEntity
{
    public static class DriverConfiguration
    {
        // private static fields
        private static Lazy<MongoClient> __client;
        private static CollectionNamespace __collectionNamespace;
        private static DatabaseNamespace __databaseNamespace;

        // static constructor
        static DriverConfiguration()
        {
            __client = new Lazy<MongoClient>(() => new MongoClient(GetClientSettings(string.Empty)), true);
            __databaseNamespace = CoreConfiguration.DatabaseNamespace;
            __collectionNamespace = new CollectionNamespace(__databaseNamespace, "testcollection");
        }

        // public static properties
        /// <summary>
        /// Gets the  client.
        /// </summary>
        public static MongoClient Client
        {
            get { return __client.Value; }
        }

        /// <summary>
        /// Gets the collection namespace.
        /// </summary>
        /// <value>
        /// The collection namespace.
        /// </value>
        public static CollectionNamespace CollectionNamespace
        {
            get { return __collectionNamespace; }
        }

        /// <summary>
        /// Gets the database namespace.
        /// </summary>
        /// <value>
        /// The database namespace.
        /// </value>
        public static DatabaseNamespace DatabaseNamespace
        {
            get { return __databaseNamespace; }
        }

        // public static methods
        public static DisposableMongoClient CreateDisposableClient()
        {
            return CreateDisposableClient((MongoClientSettings s) => { });
        }

        public static DisposableMongoClient CreateDisposableClient(Action<ClusterBuilder> clusterConfigurator)
        {
            return CreateDisposableClient((MongoClientSettings s) => s.ClusterConfigurator = clusterConfigurator);
        }

        public static DisposableMongoClient CreateDisposableClient(Action<MongoClientSettings> clientSettingsConfigurator)
        {
            var connectionString = CoreConfiguration.ConnectionString.ToString();
            var clientSettings = MongoClientSettings.FromUrl(new MongoUrl(connectionString));
            clientSettingsConfigurator(clientSettings);
            var client = new MongoClient(clientSettings);
            return new DisposableMongoClient(client);
        }

        public static DisposableMongoClient CreateDisposableClient(EventCapturer capturer)
        {
            return CreateDisposableClient((ClusterBuilder c) => c.Subscribe(capturer));
        }

        public static MongoClientSettings GetClientSettings(string connStr)
        {
            var connectionString =string.IsNullOrEmpty(connStr)? CoreConfiguration.ConnectionString.ToString(): connStr;
            var clientSettings = MongoClientSettings.FromUrl(new MongoUrl(connectionString));

            var serverSelectionTimeoutString = Environment.GetEnvironmentVariable("MONGO_SERVER_SELECTION_TIMEOUT_MS");
            if (serverSelectionTimeoutString == null)
            {
                serverSelectionTimeoutString = "30000";
            }
            clientSettings.ServerSelectionTimeout = TimeSpan.FromMilliseconds(int.Parse(serverSelectionTimeoutString));
            clientSettings.ClusterConfigurator = cb => CoreConfiguration.ConfigureLogging(cb);

            return clientSettings;
        }
    }
}
