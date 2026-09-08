using System.Net;
using Corvano.Business.Abstract;
using Corvano.Core.DataAccess;
using Corvano.Core.Utilities.Results;
using Corvano.DataAccess.Abstract;
using Corvano.Entities.Concrete;

namespace Corvano.Business.Concrete;

public class ProductManager : IProductService
{
    private readonly IProductDal _productDal;
    private readonly IUnitOfWork _unitOfWork;

    public ProductManager(IProductDal productDal, IUnitOfWork unitOfWork)
    {
        _productDal = productDal;
        _unitOfWork = unitOfWork;
    }

    public async Task<(HttpStatusCode, IDataResult<List<Product>>)> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var products = await _productDal.GetListAsync(cancellationToken: cancellationToken);
        return (HttpStatusCode.OK, new SuccessDataResult<List<Product>>(products));
    }

    public async Task<(HttpStatusCode, IResult)> AddAsync(Product product, CancellationToken cancellationToken = default)
    {
        await _productDal.AddAsync(product, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return (HttpStatusCode.Created, new SuccessResult());
    }
}
