using System;
using System.Linq;
using System.Threading.Tasks;
using Datadog.Trace;
using Datadog.Trace.ClrProfiler;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Samples.MongoDB
{
    public static class Program
    {
        private static string Host()
        {
            return Environment.GetEnvironmentVariable("MONGO_HOST") ?? "localhost";
        }

        public static void Main(string[] args)
        {
            Console.WriteLine($"Profiler attached: {Instrumentation.ProfilerAttached}");
            Console.WriteLine($"Platform: {(Environment.Is64BitProcess ? "x64" : "x32")}");

            var newDocument = new BsonDocument
            {
                { "name", "MongoDB" },
                { "type", "Database" },
                { "count", 1 },
                {
                    "info", new BsonDocument
                    {
                        { "x", 203 },
                        { "y", 102 }
                    }
                }
            };


            using (var mainScope = Tracer.Instance.StartActive("Main()", serviceName: "Samples.MongoDB"))
            {
                var connectionString = $"mongodb://{Host()}:27017";
                var client = new MongoClient(connectionString);
                var database = client.GetDatabase("test-db");
                var collection = database.GetCollection<BsonDocument>("employees");

                Run(collection, newDocument);
                RunAsync(collection, newDocument).Wait();
            }
        }

        public static void Run(IMongoCollection<BsonDocument> collection, BsonDocument newDocument)
        {
            var allFilter = new BsonDocument();

            using (var syncScope = Tracer.Instance.StartActive("sync-calls", serviceName: "Samples.MongoDB"))
            {
                collection.DeleteManyAsync(allFilter).Wait();
                collection.InsertOneAsync(newDocument).Wait();

                var countTask = collection.CountAsync(new BsonDocument());
                countTask.Wait();
                var count = countTask.Result;

                Console.WriteLine($"Documents: {count}");

                //var find = collection.Find(allFilter);
                //var allDocuments = find.ToList();
                //Console.WriteLine(allDocuments.FirstOrDefault());
            }
        }

        public static async Task RunAsync(IMongoCollection<BsonDocument> collection, BsonDocument newDocument)
        {
            var allFilter = new BsonDocument();

            using (var asyncScope = Tracer.Instance.StartActive("async-calls", serviceName: "Samples.MongoDB"))
            {
                await collection.DeleteManyAsync(allFilter);
                await collection.InsertOneAsync(newDocument);

                var countTask = collection.CountAsync(new BsonDocument());
                countTask.Wait();
                var count = countTask.Result;
                Console.WriteLine($"Documents: {count}");

                var find = await collection.FindAsync(allFilter);
                var allDocuments = await find.ToListAsync();
                Console.WriteLine(allDocuments.FirstOrDefault());
            }
        }
    }
}
