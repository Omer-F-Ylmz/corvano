using Autofac;
using Corvano.Business.Abstract;
using Corvano.Business.Concrete;
using Corvano.Core.DataAccess;
using Corvano.DataAccess.Abstract;
using Corvano.DataAccess.Concrete.EntityFramework;

namespace Corvano.Business.DependencyResolvers.Autofac;

public class AutofacBusinessModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<EfUnitOfWork>().As<IUnitOfWork>().InstancePerLifetimeScope();

        builder.RegisterType<EfProductDal>().As<IProductDal>().InstancePerLifetimeScope();
        builder.RegisterType<ProductManager>().As<IProductService>().InstancePerLifetimeScope();
    }
}
