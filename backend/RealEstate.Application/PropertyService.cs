using System.Threading;
using System.Threading.Tasks;

namespace RealEstate.Application;

public interface IPropertyRepository
{
    Task<PagedResult<PropertyLiteDto>> SearchAsync(SearchPropertiesQuery query, CancellationToken cancellationToken = default);
    Task<PropertyDetailDto?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
}

public sealed class PropertyReadService : IPropertyReadService
{
    private readonly IPropertyRepository _repository;

    public PropertyReadService(IPropertyRepository repository)
    {
        _repository = repository;
    }

    public Task<PagedResult<PropertyLiteDto>> SearchAsync(SearchPropertiesQuery query, CancellationToken cancellationToken = default)
        => _repository.SearchAsync(Normalize(query), cancellationToken);

    public Task<PropertyDetailDto?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
        => _repository.GetByIdAsync(id, cancellationToken);

    private static SearchPropertiesQuery Normalize(SearchPropertiesQuery q)
    {
        var page = q.Page <= 0 ? PagingDefaults.DefaultPage : q.Page;
        var size = q.PageSize <= 0 ? PagingDefaults.DefaultPageSize : Math.Min(q.PageSize, PagingDefaults.MaxPageSize);
        return q with { Page = page, PageSize = size };
    }
}
