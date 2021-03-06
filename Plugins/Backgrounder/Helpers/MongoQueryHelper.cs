﻿using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;

namespace Logshark.Plugins.Backgrounder.Helpers
{
    public class MongoQueryHelper
    {
        public static readonly FilterDefinitionBuilder<BsonDocument> Query = Builders<BsonDocument>.Filter;

        #region Public Methods

        public static IEnumerable<string> GetDistinctBackgrounderJobTypes(IMongoCollection<BsonDocument> collection)
        {
            return collection.Distinct<string>("job_type", Query.Empty).ToEnumerable();
        }

        public static IEnumerable<string> GetDistinctWorkerIds(IMongoCollection<BsonDocument> collection)
        {
            return collection.Distinct<string>("worker", Query.Empty).ToEnumerable();
        }

        public static IEnumerable<int> GetDistinctBackgrounderIdsForWorker(IMongoCollection<BsonDocument> collection, string workerId)
        {
            ISet<int> distinctBackgrounderIdsForWorker = new HashSet<int>();

            IEnumerable<string> distinctFileNames = collection.Distinct<string>("file", FilterByWorkerId(workerId)).ToEnumerable();
            foreach (string distinctFileName in distinctFileNames)
            {
                int? backgrounderId = Backgrounder.GetBackgrounderIdFromFilename(distinctFileName);
                if (backgrounderId.HasValue)
                {
                    distinctBackgrounderIdsForWorker.Add(backgrounderId.Value);
                }
            }

            return distinctBackgrounderIdsForWorker;
        }

        public static IEnumerable<BsonDocument> GetJobEventsForProcessByType(IMongoCollection<BsonDocument> collection, string workerId, int processId, string jobType)
        {
            var filter = Query.And(FilterByWorkerId(workerId),
                                   FilterByProcessId(processId),
                                   FilterByIsJobStartOrEndEvent(jobType));

            var sort = Builders<BsonDocument>.Sort.Ascending("ts");

            return collection.Find(filter).Sort(sort).ToEnumerable();
        }

        public static IEnumerable<BsonDocument> GetEventsInRange(IMongoCollection<BsonDocument> collection, string workerId, int backgrounderId, DateTime startTime, DateTime endTime)
        {
            var filter = Query.And(FilterByWorkerId(workerId),
                                   FilterByProcessId(backgrounderId),
                                   FilterByTimeRange(startTime, endTime),
                                   FilterOutIgnoredClasses());

            return collection.Find(filter).ToEnumerable();
        }

        #endregion Public Methods

        #region Private Methods

        private static FilterDefinition<BsonDocument> FilterByClass(string className)
        {
            return Query.Eq("class", className);
        }

        private static FilterDefinition<BsonDocument> FilterByIsJobStartOrEndEvent(string jobType)
        {
            return Query.And(FilterByClass(BackgrounderConstants.BackgrounderJobRunnerClass),
                             Query.Or(FilterBySeverity("INFO"),
                                      FilterBySeverity("ERROR")),
                             Query.Or(FilterByIsJobStartEvent(jobType),
                                      FilterByIsJobEndEvent(jobType)));
        }

        private static FilterDefinition<BsonDocument> FilterByIsJobStartEvent(string jobType)
        {
            return Query.Or(Query.Eq("job_type", jobType) & Query.Regex("message", new BsonRegularExpression("^Running job of type")), // 10.5+
                            Query.Regex("message", new BsonRegularExpression(String.Format("^Running job of type :{0};", jobType))));  // 9.0-10.4

        }

        private static FilterDefinition<BsonDocument> FilterByIsJobEndEvent(string jobType)
        {
            return Query.Or(Query.Regex("message", new BsonRegularExpression(String.Format(@"^Job finished: [A-Z]+; name: [A-Za-z\s]+; type :{0};", jobType))),
                            Query.Eq("job_type", jobType) & Query.Regex("message", new BsonRegularExpression("^Error executing backgroundjob:")), // 10.5+
                            Query.Regex("message", new BsonRegularExpression(String.Format(@"^Error executing backgroundjob: :{0}", jobType))));  // 9.0-10.4
        }

        private static FilterDefinition<BsonDocument> FilterByProcessId(int processId)
        {
            return Query.Regex("file", new BsonRegularExpression(String.Format(@"^backgrounder(_node\d+)?-{0}\.", processId)));
        }

        private static FilterDefinition<BsonDocument> FilterBySeverity(string severity)
        {
            return Query.Eq("sev", severity);
        }

        private static FilterDefinition<BsonDocument> FilterByTimeRange(DateTime startTime, DateTime endTime)
        {
            return Query.And(Query.Gte("ts", startTime),
                             Query.Lte("ts", endTime));
        }

        private static FilterDefinition<BsonDocument> FilterByWorkerId(string workerId)
        {
            return Query.Eq("worker", workerId);
        }

        private static FilterDefinition<BsonDocument> FilterOutIgnoredClasses()
        {
            return Query.Nin("class", BackgrounderConstants.IgnoredClasses);
        }

        #endregion Private Methods
    }
}