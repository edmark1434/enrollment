using System.Web.Mvc;
using Unity;
using Unity.Mvc5;
using EnrollmentSystem.Controllers.Service;

namespace EnrollmentSystem
{
    public static class UnityConfig
    {
        public static void RegisterComponents()
        {
            var container = new UnityContainer();

            // Register services here
            container.RegisterType<IFetchService, FetchService>();

            DependencyResolver.SetResolver(new UnityDependencyResolver(container));
        }
    }
}