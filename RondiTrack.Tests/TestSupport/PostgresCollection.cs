namespace RondiTrack.Tests.TestSupport;

[CollectionDefinition("Postgres collection")]
public class PostgresCollection : ICollectionFixture<PostgresApiFactory>
{
}