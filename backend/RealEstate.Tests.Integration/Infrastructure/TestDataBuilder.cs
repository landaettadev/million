using Bogus;
using RealEstate.Infrastructure;

namespace RealEstate.Tests.Integration.Infrastructure;

public static class TestDataBuilder
{
    private static readonly Faker _faker = new();

    public static List<OwnerDocument> CreateOwners(int count = 5)
    {
        return Enumerable.Range(1, count)
            .Select(_ => new OwnerDocument
            {
                Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(),
                Name = _faker.Person.FullName,
                Address = _faker.Address.FullAddress(),
                Photo = _faker.Internet.Avatar(),
                Birthday = _faker.Date.Past(40, DateTime.Now.AddYears(-18))
            })
            .ToList();
    }

    public static List<PropertyDocument> CreateProperties(List<OwnerDocument> owners, int count = 10)
    {
        return new Faker<PropertyDocument>()
            .RuleFor(p => p.Id, _ => MongoDB.Bson.ObjectId.GenerateNewId().ToString())
            .RuleFor(p => p.OwnerId, f => f.PickRandom(owners).Id)
            .RuleFor(p => p.Name, f => $"{f.Address.BuildingNumber()} {f.Address.StreetName()}")
            .RuleFor(p => p.Address, f => f.Address.FullAddress())
            .RuleFor(p => p.Price, f => f.Random.Decimal(100_000, 5_000_000))
            .RuleFor(p => p.OperationType, f => f.PickRandom("sale", "rent"))
            .RuleFor(p => p.Description, f => f.Lorem.Sentence(10))
            .RuleFor(p => p.Beds, f => f.Random.Int(1, 6))
            .RuleFor(p => p.Baths, f => f.Random.Int(1, 4))
            .RuleFor(p => p.HalfBaths, f => f.Random.Int(0, 2))
            .RuleFor(p => p.Sqft, f => f.Random.Int(800, 8000))
            .Generate(count);
    }

    public static List<PropertyImageDocument> CreatePropertyImages(List<PropertyDocument> properties)
    {
        var images = new List<PropertyImageDocument>();
        
        foreach (var property in properties)
        {
            var imageCount = _faker.Random.Int(2, 6);
            for (int i = 0; i < imageCount; i++)
            {
                images.Add(new PropertyImageDocument
                {
                    Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(),
                    PropertyId = property.Id,
                    File = $"https://picsum.photos/id/{_faker.Random.Int(1, 1000)}/1600/1000",
                    Enabled = i == 0, // First image enabled
                    Order = i + 1
                });
            }
        }
        
        return images;
    }

    public static List<PropertyTraceDocument> CreatePropertyTraces(List<PropertyDocument> properties)
    {
        var traces = new List<PropertyTraceDocument>();
        var traceNames = new[] { "Initial Sale", "Property Transfer", "Ownership Change", "Estate Transfer", "Investment Purchase" };
        
        foreach (var property in properties)
        {
            var traceCount = _faker.Random.Int(1, 3);
            for (int i = 0; i < traceCount; i++)
            {
                traces.Add(new PropertyTraceDocument
                {
                    Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(),
                    PropertyId = property.Id,
                    DateSale = _faker.Date.Past(3),
                    Name = _faker.PickRandom(traceNames),
                    Value = property.Price * _faker.Random.Decimal(0.8m, 1.2m),
                    Tax = property.Price * _faker.Random.Decimal(0.01m, 0.03m)
                });
            }
        }
        
        return traces;
    }
}
