namespace TurkiyeSimulasyonu.Api.Tests;

// Aynı koleksiyondaki bütün test sınıfları tek bir ApiFactory'yi (tek veritabanı) paylaşır
[CollectionDefinition("Api")]
public sealed class ApiCollection : ICollectionFixture<ApiFactory>;