using System.Net;
using Corvano.Business.Abstract;
using Corvano.Business.Utilities;
using Corvano.Core.DataAccess;
using Corvano.Core.Utilities.Results;
using Corvano.DataAccess.Abstract;
using Corvano.Entities.Concrete;

namespace Corvano.Business.Concrete;

public class CategoryManager : ICategoryService
{
    private readonly ICategoryDal _categoryDal;
    private readonly IProductDal _productDal;
    private readonly IUnitOfWork _unitOfWork;

    public CategoryManager(ICategoryDal categoryDal, IProductDal productDal, IUnitOfWork unitOfWork)
    {
        _categoryDal = categoryDal;
        _productDal = productDal;
        _unitOfWork = unitOfWork;
    }

    public async Task<(HttpStatusCode, IDataResult<List<Category>>)> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var categories = await _categoryDal.GetListAsync(cancellationToken: cancellationToken);
        return (HttpStatusCode.OK, new SuccessDataResult<List<Category>>(categories));
    }

    public async Task<(HttpStatusCode, IDataResult<Category>)> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var category = await _categoryDal.GetAsync(c => c.Id == id, cancellationToken);
        return category is null
            ? (HttpStatusCode.NotFound, new ErrorDataResult<Category>("Kategori bulunamadı."))
            : (HttpStatusCode.OK, new SuccessDataResult<Category>(category));
    }

    public async Task<(HttpStatusCode, IResult)> AddAsync(Category category, CancellationToken cancellationToken = default)
    {
        category.Slug = await UniqueSlugAsync(category.Name, excludedId: 0, cancellationToken);
        await _categoryDal.AddAsync(category, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return (HttpStatusCode.Created, new SuccessResult("Kategori eklendi."));
    }

    public async Task<(HttpStatusCode, IResult)> UpdateAsync(Category category, CancellationToken cancellationToken = default)
    {
        var stored = await _categoryDal.GetAsync(c => c.Id == category.Id, cancellationToken);
        if (stored is null)
        {
            return (HttpStatusCode.NotFound, new ErrorResult("Kategori bulunamadı."));
        }

        stored.Name = category.Name;
        stored.ParentId = category.ParentId;
        stored.SortOrder = category.SortOrder;
        stored.IsActive = category.IsActive;
        stored.Slug = await UniqueSlugAsync(category.Name, stored.Id, cancellationToken);

        _categoryDal.Update(stored);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return (HttpStatusCode.OK, new SuccessResult("Kategori güncellendi."));
    }

    public async Task<(HttpStatusCode, IResult)> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var category = await _categoryDal.GetAsync(c => c.Id == id, cancellationToken);
        if (category is null)
        {
            return (HttpStatusCode.NotFound, new ErrorResult("Kategori bulunamadı."));
        }

        if (await _productDal.GetAsync(p => p.CategoryId == id, cancellationToken) is not null)
        {
            return (HttpStatusCode.Conflict, new ErrorResult("Kategoride ürün var; önce ürünleri başka kategoriye taşıyın."));
        }

        if (await _categoryDal.GetAsync(c => c.ParentId == id, cancellationToken) is not null)
        {
            return (HttpStatusCode.Conflict, new ErrorResult("Kategorinin alt kategorileri var; önce onları silin."));
        }

        _categoryDal.Delete(category);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return (HttpStatusCode.OK, new SuccessResult("Kategori silindi."));
    }

    private Task<string> UniqueSlugAsync(string name, int excludedId, CancellationToken cancellationToken)
        => SlugGenerator.MakeUniqueAsync(
            SlugGenerator.Generate(name),
            async slug => await _categoryDal.GetAsync(c => c.Slug == slug && c.Id != excludedId, cancellationToken) is not null);
}
