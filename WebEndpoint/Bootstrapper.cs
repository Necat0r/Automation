using Module;
using Nancy;
using Nancy.Bootstrapper;
using Nancy.Conventions;
using Nancy.TinyIoc;
using System;
using System.Collections.Generic;
using System.IO;

namespace WebEndpoint
{
    public class Bootstrapper : DefaultNancyBootstrapper
    {
        // The bootstrapper enables you to reconfigure the composition of the framework,
        // by overriding the various methods and properties.
        // For more information https://github.com/NancyFx/Nancy/wiki/Bootstrapper

        private ServiceManager mServiceManager;
        private DeviceManager mDeviceManager;

        public Bootstrapper(ServiceManager serviceManager, DeviceManager deviceManager)
        {
            mServiceManager = serviceManager;
            mDeviceManager = deviceManager;
        }

        protected override void ApplicationStartup(TinyIoCContainer container, Nancy.Bootstrapper.IPipelines pipelines)
        {
            base.ApplicationStartup(container, pipelines);

            this.Conventions.ViewLocationConventions.Insert(0, (viewName, model, context) =>
            {
                return string.Concat("Web/", viewName);
            });

            pipelines.AfterRequest.AddItemToEndOfPipeline(x =>
            {
                x.Response.Headers.Add("Access-Control-Allow-Origin", "*");
                x.Response.Headers.Add("Access-Control-Allow-Methods", "POST,GET,DELETE,PUT,OPTIONS");
                x.Response.Headers.Add("Access-Control-Allow-Headers", "Accept, Origin, Content-type");
            });
        }

        protected override void RegisterInstances(TinyIoCContainer container, IEnumerable<InstanceRegistration> instanceRegistrations)
        {
            base.RegisterInstances(container, instanceRegistrations);

            container.Register(mServiceManager.GetType(), mServiceManager);
            container.Register(mDeviceManager.GetType(), mDeviceManager);
        }

        protected override void ConfigureConventions(Nancy.Conventions.NancyConventions nancyConventions)
        {
            base.ConfigureConventions(nancyConventions);

            Conventions.StaticContentsConventions.Add(
                StaticContentConventionBuilder.AddDirectory("/", "/Web")
            );
        }

        //protected override IEnumerable<Type> BodyDeserializers
        //{
        //    get
        //    {
        //        yield return typeof(Nancy.Serialization.JsonNet.JsonNetBodyDeserializer /* JsonNetBodyDeserializer */ );
        //    }
        //}

        public class CustomRootPathProvider : IRootPathProvider
        {
            public string GetRootPath()
            {
                return Directory.GetCurrentDirectory();
                //return "What ever path you want to use as your application root";
            }
        }

        protected override IRootPathProvider RootPathProvider
        {
            get { return new CustomRootPathProvider(); }
        }
    }
}