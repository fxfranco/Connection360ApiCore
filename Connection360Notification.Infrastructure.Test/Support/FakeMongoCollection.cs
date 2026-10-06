using MongoDB.Driver;

namespace Connection360Notification.Infrastructure.Tests.Support
{
    /// <summary>Estado compartido entre la prueba y el doble de coleccion.</summary>
    internal sealed class FakeCollectionState
    {
        public Int64 NextSequence { get; set; } = 1;

        public Exception? ToThrow { get; set; }

        public List<(Object Filter, Object Update, Object Options, CancellationToken Token)> Calls { get; } = new();
    }

    /// <summary>
    /// Doble manual de IMongoCollection&lt;T&gt; para tipos T no visibles (p. ej. el CounterDocument privado
    /// del generador de ids), donde Castle/Moq/DispatchProxy no pueden generar proxies. Solo implementa
    /// FindOneAndUpdateAsync: devuelve un documento T cuya propiedad "Sequence" vale NextSequence.
    /// </summary>
    internal sealed class FakeMongoCollection<T> : IMongoCollection<T>
    {
        private readonly FakeCollectionState _state;

        public FakeMongoCollection(FakeCollectionState state) => _state = state;

        public MongoDB.Driver.CollectionNamespace CollectionNamespace => throw new NotSupportedException();
        public MongoDB.Driver.IMongoDatabase Database => throw new NotSupportedException();
        public MongoDB.Bson.Serialization.IBsonSerializer<T> DocumentSerializer => throw new NotSupportedException();
        public MongoDB.Driver.IMongoIndexManager<T> Indexes => throw new NotSupportedException();
        public MongoDB.Driver.Search.IMongoSearchIndexManager SearchIndexes => throw new NotSupportedException();
        public MongoDB.Driver.MongoCollectionSettings Settings => throw new NotSupportedException();
        public MongoDB.Driver.IAsyncCursor<TResult> Aggregate<TResult>(MongoDB.Driver.PipelineDefinition<T, TResult> pipeline, MongoDB.Driver.AggregateOptions options, CancellationToken cancellationToken) => throw new NotSupportedException();
        public MongoDB.Driver.IAsyncCursor<TResult> Aggregate<TResult>(MongoDB.Driver.IClientSessionHandle session, MongoDB.Driver.PipelineDefinition<T, TResult> pipeline, MongoDB.Driver.AggregateOptions options, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<MongoDB.Driver.IAsyncCursor<TResult>> AggregateAsync<TResult>(MongoDB.Driver.PipelineDefinition<T, TResult> pipeline, MongoDB.Driver.AggregateOptions options, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<MongoDB.Driver.IAsyncCursor<TResult>> AggregateAsync<TResult>(MongoDB.Driver.IClientSessionHandle session, MongoDB.Driver.PipelineDefinition<T, TResult> pipeline, MongoDB.Driver.AggregateOptions options, CancellationToken cancellationToken) => throw new NotSupportedException();
        public void AggregateToCollection<TResult>(MongoDB.Driver.PipelineDefinition<T, TResult> pipeline, MongoDB.Driver.AggregateOptions options, CancellationToken cancellationToken) => throw new NotSupportedException();
        public void AggregateToCollection<TResult>(MongoDB.Driver.IClientSessionHandle session, MongoDB.Driver.PipelineDefinition<T, TResult> pipeline, MongoDB.Driver.AggregateOptions options, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task AggregateToCollectionAsync<TResult>(MongoDB.Driver.PipelineDefinition<T, TResult> pipeline, MongoDB.Driver.AggregateOptions options, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task AggregateToCollectionAsync<TResult>(MongoDB.Driver.IClientSessionHandle session, MongoDB.Driver.PipelineDefinition<T, TResult> pipeline, MongoDB.Driver.AggregateOptions options, CancellationToken cancellationToken) => throw new NotSupportedException();
        public MongoDB.Driver.BulkWriteResult<T> BulkWrite(IEnumerable<MongoDB.Driver.WriteModel<T>> requests, MongoDB.Driver.BulkWriteOptions options, CancellationToken cancellationToken) => throw new NotSupportedException();
        public MongoDB.Driver.BulkWriteResult<T> BulkWrite(MongoDB.Driver.IClientSessionHandle session, IEnumerable<MongoDB.Driver.WriteModel<T>> requests, MongoDB.Driver.BulkWriteOptions options, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<MongoDB.Driver.BulkWriteResult<T>> BulkWriteAsync(IEnumerable<MongoDB.Driver.WriteModel<T>> requests, MongoDB.Driver.BulkWriteOptions options, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<MongoDB.Driver.BulkWriteResult<T>> BulkWriteAsync(MongoDB.Driver.IClientSessionHandle session, IEnumerable<MongoDB.Driver.WriteModel<T>> requests, MongoDB.Driver.BulkWriteOptions options, CancellationToken cancellationToken) => throw new NotSupportedException();
        public System.Int64 Count(MongoDB.Driver.FilterDefinition<T> filter, MongoDB.Driver.CountOptions options, CancellationToken cancellationToken) => throw new NotSupportedException();
        public System.Int64 Count(MongoDB.Driver.IClientSessionHandle session, MongoDB.Driver.FilterDefinition<T> filter, MongoDB.Driver.CountOptions options, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<System.Int64> CountAsync(MongoDB.Driver.FilterDefinition<T> filter, MongoDB.Driver.CountOptions options, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<System.Int64> CountAsync(MongoDB.Driver.IClientSessionHandle session, MongoDB.Driver.FilterDefinition<T> filter, MongoDB.Driver.CountOptions options, CancellationToken cancellationToken) => throw new NotSupportedException();
        public System.Int64 CountDocuments(MongoDB.Driver.FilterDefinition<T> filter, MongoDB.Driver.CountOptions options, CancellationToken cancellationToken) => throw new NotSupportedException();
        public System.Int64 CountDocuments(MongoDB.Driver.IClientSessionHandle session, MongoDB.Driver.FilterDefinition<T> filter, MongoDB.Driver.CountOptions options, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<System.Int64> CountDocumentsAsync(MongoDB.Driver.FilterDefinition<T> filter, MongoDB.Driver.CountOptions options, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<System.Int64> CountDocumentsAsync(MongoDB.Driver.IClientSessionHandle session, MongoDB.Driver.FilterDefinition<T> filter, MongoDB.Driver.CountOptions options, CancellationToken cancellationToken) => throw new NotSupportedException();
        public MongoDB.Driver.DeleteResult DeleteMany(MongoDB.Driver.FilterDefinition<T> filter, CancellationToken cancellationToken) => throw new NotSupportedException();
        public MongoDB.Driver.DeleteResult DeleteMany(MongoDB.Driver.FilterDefinition<T> filter, MongoDB.Driver.DeleteOptions options, CancellationToken cancellationToken) => throw new NotSupportedException();
        public MongoDB.Driver.DeleteResult DeleteMany(MongoDB.Driver.IClientSessionHandle session, MongoDB.Driver.FilterDefinition<T> filter, MongoDB.Driver.DeleteOptions options, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<MongoDB.Driver.DeleteResult> DeleteManyAsync(MongoDB.Driver.FilterDefinition<T> filter, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<MongoDB.Driver.DeleteResult> DeleteManyAsync(MongoDB.Driver.FilterDefinition<T> filter, MongoDB.Driver.DeleteOptions options, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<MongoDB.Driver.DeleteResult> DeleteManyAsync(MongoDB.Driver.IClientSessionHandle session, MongoDB.Driver.FilterDefinition<T> filter, MongoDB.Driver.DeleteOptions options, CancellationToken cancellationToken) => throw new NotSupportedException();
        public MongoDB.Driver.DeleteResult DeleteOne(MongoDB.Driver.FilterDefinition<T> filter, CancellationToken cancellationToken) => throw new NotSupportedException();
        public MongoDB.Driver.DeleteResult DeleteOne(MongoDB.Driver.FilterDefinition<T> filter, MongoDB.Driver.DeleteOptions options, CancellationToken cancellationToken) => throw new NotSupportedException();
        public MongoDB.Driver.DeleteResult DeleteOne(MongoDB.Driver.IClientSessionHandle session, MongoDB.Driver.FilterDefinition<T> filter, MongoDB.Driver.DeleteOptions options, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<MongoDB.Driver.DeleteResult> DeleteOneAsync(MongoDB.Driver.FilterDefinition<T> filter, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<MongoDB.Driver.DeleteResult> DeleteOneAsync(MongoDB.Driver.FilterDefinition<T> filter, MongoDB.Driver.DeleteOptions options, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<MongoDB.Driver.DeleteResult> DeleteOneAsync(MongoDB.Driver.IClientSessionHandle session, MongoDB.Driver.FilterDefinition<T> filter, MongoDB.Driver.DeleteOptions options, CancellationToken cancellationToken) => throw new NotSupportedException();
        public MongoDB.Driver.IAsyncCursor<TField> Distinct<TField>(MongoDB.Driver.FieldDefinition<T, TField> field, MongoDB.Driver.FilterDefinition<T> filter, MongoDB.Driver.DistinctOptions options, CancellationToken cancellationToken) => throw new NotSupportedException();
        public MongoDB.Driver.IAsyncCursor<TField> Distinct<TField>(MongoDB.Driver.IClientSessionHandle session, MongoDB.Driver.FieldDefinition<T, TField> field, MongoDB.Driver.FilterDefinition<T> filter, MongoDB.Driver.DistinctOptions options, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<MongoDB.Driver.IAsyncCursor<TField>> DistinctAsync<TField>(MongoDB.Driver.FieldDefinition<T, TField> field, MongoDB.Driver.FilterDefinition<T> filter, MongoDB.Driver.DistinctOptions options, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<MongoDB.Driver.IAsyncCursor<TField>> DistinctAsync<TField>(MongoDB.Driver.IClientSessionHandle session, MongoDB.Driver.FieldDefinition<T, TField> field, MongoDB.Driver.FilterDefinition<T> filter, MongoDB.Driver.DistinctOptions options, CancellationToken cancellationToken) => throw new NotSupportedException();
        public MongoDB.Driver.IAsyncCursor<TItem> DistinctMany<TItem>(MongoDB.Driver.FieldDefinition<T, IEnumerable<TItem>> field, MongoDB.Driver.FilterDefinition<T> filter, MongoDB.Driver.DistinctOptions options, CancellationToken cancellationToken) => throw new NotSupportedException();
        public MongoDB.Driver.IAsyncCursor<TItem> DistinctMany<TItem>(MongoDB.Driver.IClientSessionHandle session, MongoDB.Driver.FieldDefinition<T, IEnumerable<TItem>> field, MongoDB.Driver.FilterDefinition<T> filter, MongoDB.Driver.DistinctOptions options, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<MongoDB.Driver.IAsyncCursor<TItem>> DistinctManyAsync<TItem>(MongoDB.Driver.FieldDefinition<T, IEnumerable<TItem>> field, MongoDB.Driver.FilterDefinition<T> filter, MongoDB.Driver.DistinctOptions options, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<MongoDB.Driver.IAsyncCursor<TItem>> DistinctManyAsync<TItem>(MongoDB.Driver.IClientSessionHandle session, MongoDB.Driver.FieldDefinition<T, IEnumerable<TItem>> field, MongoDB.Driver.FilterDefinition<T> filter, MongoDB.Driver.DistinctOptions options, CancellationToken cancellationToken) => throw new NotSupportedException();
        public System.Int64 EstimatedDocumentCount(MongoDB.Driver.EstimatedDocumentCountOptions options, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<System.Int64> EstimatedDocumentCountAsync(MongoDB.Driver.EstimatedDocumentCountOptions options, CancellationToken cancellationToken) => throw new NotSupportedException();
        public MongoDB.Driver.IAsyncCursor<TProjection> FindSync<TProjection>(MongoDB.Driver.FilterDefinition<T> filter, MongoDB.Driver.FindOptions<T, TProjection> options, CancellationToken cancellationToken) => throw new NotSupportedException();
        public MongoDB.Driver.IAsyncCursor<TProjection> FindSync<TProjection>(MongoDB.Driver.IClientSessionHandle session, MongoDB.Driver.FilterDefinition<T> filter, MongoDB.Driver.FindOptions<T, TProjection> options, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<MongoDB.Driver.IAsyncCursor<TProjection>> FindAsync<TProjection>(MongoDB.Driver.FilterDefinition<T> filter, MongoDB.Driver.FindOptions<T, TProjection> options, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<MongoDB.Driver.IAsyncCursor<TProjection>> FindAsync<TProjection>(MongoDB.Driver.IClientSessionHandle session, MongoDB.Driver.FilterDefinition<T> filter, MongoDB.Driver.FindOptions<T, TProjection> options, CancellationToken cancellationToken) => throw new NotSupportedException();
        public TProjection FindOneAndDelete<TProjection>(MongoDB.Driver.FilterDefinition<T> filter, MongoDB.Driver.FindOneAndDeleteOptions<T, TProjection> options, CancellationToken cancellationToken) => throw new NotSupportedException();
        public TProjection FindOneAndDelete<TProjection>(MongoDB.Driver.IClientSessionHandle session, MongoDB.Driver.FilterDefinition<T> filter, MongoDB.Driver.FindOneAndDeleteOptions<T, TProjection> options, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<TProjection> FindOneAndDeleteAsync<TProjection>(MongoDB.Driver.FilterDefinition<T> filter, MongoDB.Driver.FindOneAndDeleteOptions<T, TProjection> options, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<TProjection> FindOneAndDeleteAsync<TProjection>(MongoDB.Driver.IClientSessionHandle session, MongoDB.Driver.FilterDefinition<T> filter, MongoDB.Driver.FindOneAndDeleteOptions<T, TProjection> options, CancellationToken cancellationToken) => throw new NotSupportedException();
        public TProjection FindOneAndReplace<TProjection>(MongoDB.Driver.FilterDefinition<T> filter, T replacement, MongoDB.Driver.FindOneAndReplaceOptions<T, TProjection> options, CancellationToken cancellationToken) => throw new NotSupportedException();
        public TProjection FindOneAndReplace<TProjection>(MongoDB.Driver.IClientSessionHandle session, MongoDB.Driver.FilterDefinition<T> filter, T replacement, MongoDB.Driver.FindOneAndReplaceOptions<T, TProjection> options, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<TProjection> FindOneAndReplaceAsync<TProjection>(MongoDB.Driver.FilterDefinition<T> filter, T replacement, MongoDB.Driver.FindOneAndReplaceOptions<T, TProjection> options, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<TProjection> FindOneAndReplaceAsync<TProjection>(MongoDB.Driver.IClientSessionHandle session, MongoDB.Driver.FilterDefinition<T> filter, T replacement, MongoDB.Driver.FindOneAndReplaceOptions<T, TProjection> options, CancellationToken cancellationToken) => throw new NotSupportedException();
        public TProjection FindOneAndUpdate<TProjection>(MongoDB.Driver.FilterDefinition<T> filter, MongoDB.Driver.UpdateDefinition<T> update, MongoDB.Driver.FindOneAndUpdateOptions<T, TProjection> options, CancellationToken cancellationToken) => throw new NotSupportedException();
        public TProjection FindOneAndUpdate<TProjection>(MongoDB.Driver.IClientSessionHandle session, MongoDB.Driver.FilterDefinition<T> filter, MongoDB.Driver.UpdateDefinition<T> update, MongoDB.Driver.FindOneAndUpdateOptions<T, TProjection> options, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<TProjection> FindOneAndUpdateAsync<TProjection>(MongoDB.Driver.FilterDefinition<T> filter, MongoDB.Driver.UpdateDefinition<T> update, MongoDB.Driver.FindOneAndUpdateOptions<T, TProjection> options, CancellationToken cancellationToken)
        {
            _state.Calls.Add((filter, update, options, cancellationToken));
            if (_state.ToThrow is not null) return Task.FromException<TProjection>(_state.ToThrow);
            var document = Activator.CreateInstance(typeof(TProjection), nonPublic: true)!;
            typeof(TProjection).GetProperty("Sequence")!.SetValue(document, _state.NextSequence);
            return Task.FromResult((TProjection)document);
        }
        public Task<TProjection> FindOneAndUpdateAsync<TProjection>(MongoDB.Driver.IClientSessionHandle session, MongoDB.Driver.FilterDefinition<T> filter, MongoDB.Driver.UpdateDefinition<T> update, MongoDB.Driver.FindOneAndUpdateOptions<T, TProjection> options, CancellationToken cancellationToken) => throw new NotSupportedException();
        public void InsertOne(T document, MongoDB.Driver.InsertOneOptions options, CancellationToken cancellationToken) => throw new NotSupportedException();
        public void InsertOne(MongoDB.Driver.IClientSessionHandle session, T document, MongoDB.Driver.InsertOneOptions options, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task InsertOneAsync(T document, CancellationToken _cancellationToken) => throw new NotSupportedException();
        public Task InsertOneAsync(T document, MongoDB.Driver.InsertOneOptions options, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task InsertOneAsync(MongoDB.Driver.IClientSessionHandle session, T document, MongoDB.Driver.InsertOneOptions options, CancellationToken cancellationToken) => throw new NotSupportedException();
        public void InsertMany(IEnumerable<T> documents, MongoDB.Driver.InsertManyOptions options, CancellationToken cancellationToken) => throw new NotSupportedException();
        public void InsertMany(MongoDB.Driver.IClientSessionHandle session, IEnumerable<T> documents, MongoDB.Driver.InsertManyOptions options, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task InsertManyAsync(IEnumerable<T> documents, MongoDB.Driver.InsertManyOptions options, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task InsertManyAsync(MongoDB.Driver.IClientSessionHandle session, IEnumerable<T> documents, MongoDB.Driver.InsertManyOptions options, CancellationToken cancellationToken) => throw new NotSupportedException();
        public MongoDB.Driver.IAsyncCursor<TResult> MapReduce<TResult>(MongoDB.Bson.BsonJavaScript map, MongoDB.Bson.BsonJavaScript reduce, MongoDB.Driver.MapReduceOptions<T, TResult> options, CancellationToken cancellationToken) => throw new NotSupportedException();
        public MongoDB.Driver.IAsyncCursor<TResult> MapReduce<TResult>(MongoDB.Driver.IClientSessionHandle session, MongoDB.Bson.BsonJavaScript map, MongoDB.Bson.BsonJavaScript reduce, MongoDB.Driver.MapReduceOptions<T, TResult> options, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<MongoDB.Driver.IAsyncCursor<TResult>> MapReduceAsync<TResult>(MongoDB.Bson.BsonJavaScript map, MongoDB.Bson.BsonJavaScript reduce, MongoDB.Driver.MapReduceOptions<T, TResult> options, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<MongoDB.Driver.IAsyncCursor<TResult>> MapReduceAsync<TResult>(MongoDB.Driver.IClientSessionHandle session, MongoDB.Bson.BsonJavaScript map, MongoDB.Bson.BsonJavaScript reduce, MongoDB.Driver.MapReduceOptions<T, TResult> options, CancellationToken cancellationToken) => throw new NotSupportedException();
        public MongoDB.Driver.IFilteredMongoCollection<TDerivedDocument> OfType<TDerivedDocument>() where TDerivedDocument : T => throw new NotSupportedException();
        public MongoDB.Driver.ReplaceOneResult ReplaceOne(MongoDB.Driver.FilterDefinition<T> filter, T replacement, MongoDB.Driver.ReplaceOptions options, CancellationToken cancellationToken) => throw new NotSupportedException();
        public MongoDB.Driver.ReplaceOneResult ReplaceOne(MongoDB.Driver.FilterDefinition<T> filter, T replacement, MongoDB.Driver.UpdateOptions options, CancellationToken cancellationToken) => throw new NotSupportedException();
        public MongoDB.Driver.ReplaceOneResult ReplaceOne(MongoDB.Driver.IClientSessionHandle session, MongoDB.Driver.FilterDefinition<T> filter, T replacement, MongoDB.Driver.ReplaceOptions options, CancellationToken cancellationToken) => throw new NotSupportedException();
        public MongoDB.Driver.ReplaceOneResult ReplaceOne(MongoDB.Driver.IClientSessionHandle session, MongoDB.Driver.FilterDefinition<T> filter, T replacement, MongoDB.Driver.UpdateOptions options, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<MongoDB.Driver.ReplaceOneResult> ReplaceOneAsync(MongoDB.Driver.FilterDefinition<T> filter, T replacement, MongoDB.Driver.ReplaceOptions options, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<MongoDB.Driver.ReplaceOneResult> ReplaceOneAsync(MongoDB.Driver.FilterDefinition<T> filter, T replacement, MongoDB.Driver.UpdateOptions options, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<MongoDB.Driver.ReplaceOneResult> ReplaceOneAsync(MongoDB.Driver.IClientSessionHandle session, MongoDB.Driver.FilterDefinition<T> filter, T replacement, MongoDB.Driver.ReplaceOptions options, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<MongoDB.Driver.ReplaceOneResult> ReplaceOneAsync(MongoDB.Driver.IClientSessionHandle session, MongoDB.Driver.FilterDefinition<T> filter, T replacement, MongoDB.Driver.UpdateOptions options, CancellationToken cancellationToken) => throw new NotSupportedException();
        public MongoDB.Driver.UpdateResult UpdateMany(MongoDB.Driver.FilterDefinition<T> filter, MongoDB.Driver.UpdateDefinition<T> update, MongoDB.Driver.UpdateOptions options, CancellationToken cancellationToken) => throw new NotSupportedException();
        public MongoDB.Driver.UpdateResult UpdateMany(MongoDB.Driver.IClientSessionHandle session, MongoDB.Driver.FilterDefinition<T> filter, MongoDB.Driver.UpdateDefinition<T> update, MongoDB.Driver.UpdateOptions options, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<MongoDB.Driver.UpdateResult> UpdateManyAsync(MongoDB.Driver.FilterDefinition<T> filter, MongoDB.Driver.UpdateDefinition<T> update, MongoDB.Driver.UpdateOptions options, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<MongoDB.Driver.UpdateResult> UpdateManyAsync(MongoDB.Driver.IClientSessionHandle session, MongoDB.Driver.FilterDefinition<T> filter, MongoDB.Driver.UpdateDefinition<T> update, MongoDB.Driver.UpdateOptions options, CancellationToken cancellationToken) => throw new NotSupportedException();
        public MongoDB.Driver.UpdateResult UpdateOne(MongoDB.Driver.FilterDefinition<T> filter, MongoDB.Driver.UpdateDefinition<T> update, MongoDB.Driver.UpdateOptions options, CancellationToken cancellationToken) => throw new NotSupportedException();
        public MongoDB.Driver.UpdateResult UpdateOne(MongoDB.Driver.IClientSessionHandle session, MongoDB.Driver.FilterDefinition<T> filter, MongoDB.Driver.UpdateDefinition<T> update, MongoDB.Driver.UpdateOptions options, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<MongoDB.Driver.UpdateResult> UpdateOneAsync(MongoDB.Driver.FilterDefinition<T> filter, MongoDB.Driver.UpdateDefinition<T> update, MongoDB.Driver.UpdateOptions options, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<MongoDB.Driver.UpdateResult> UpdateOneAsync(MongoDB.Driver.IClientSessionHandle session, MongoDB.Driver.FilterDefinition<T> filter, MongoDB.Driver.UpdateDefinition<T> update, MongoDB.Driver.UpdateOptions options, CancellationToken cancellationToken) => throw new NotSupportedException();
        public MongoDB.Driver.IChangeStreamCursor<TResult> Watch<TResult>(MongoDB.Driver.PipelineDefinition<MongoDB.Driver.ChangeStreamDocument<T>, TResult> pipeline, MongoDB.Driver.ChangeStreamOptions options, CancellationToken cancellationToken) => throw new NotSupportedException();
        public MongoDB.Driver.IChangeStreamCursor<TResult> Watch<TResult>(MongoDB.Driver.IClientSessionHandle session, MongoDB.Driver.PipelineDefinition<MongoDB.Driver.ChangeStreamDocument<T>, TResult> pipeline, MongoDB.Driver.ChangeStreamOptions options, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<MongoDB.Driver.IChangeStreamCursor<TResult>> WatchAsync<TResult>(MongoDB.Driver.PipelineDefinition<MongoDB.Driver.ChangeStreamDocument<T>, TResult> pipeline, MongoDB.Driver.ChangeStreamOptions options, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<MongoDB.Driver.IChangeStreamCursor<TResult>> WatchAsync<TResult>(MongoDB.Driver.IClientSessionHandle session, MongoDB.Driver.PipelineDefinition<MongoDB.Driver.ChangeStreamDocument<T>, TResult> pipeline, MongoDB.Driver.ChangeStreamOptions options, CancellationToken cancellationToken) => throw new NotSupportedException();
        public MongoDB.Driver.IMongoCollection<T> WithReadConcern(MongoDB.Driver.ReadConcern readConcern) => throw new NotSupportedException();
        public MongoDB.Driver.IMongoCollection<T> WithReadPreference(MongoDB.Driver.ReadPreference readPreference) => throw new NotSupportedException();
        public MongoDB.Driver.IMongoCollection<T> WithWriteConcern(MongoDB.Driver.WriteConcern writeConcern) => throw new NotSupportedException();
    }
}
