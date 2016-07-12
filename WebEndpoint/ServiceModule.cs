using Module;
using Nancy;
using System;
using System.Text;
using System.Reflection;
using System.Linq;
using System.Collections.Generic;
using Nancy.ModelBinding;
using Logging;

namespace WebEndpoint
{
    public class ServiceModule : NancyModule
    {
        public ServiceModule(ServiceManager serviceManager)
        {
            Log.Info("Number of registered services {0}", serviceManager.Services.Count);

            // Find all service methods having a ServiceGetContractAttribute attached to it.
            var bindQuery = from service in serviceManager.Services
                        from method in service.GetType().GetMethods()
                        from attribute in method.GetCustomAttributes(true)
                        where attribute is ServiceBase.ServiceGetContractAttribute
                        select new { service, method, attribute };

            foreach (var bindData in bindQuery)
            {
                var service = bindData.service;
                var method = bindData.method;
                var attribute = bindData.attribute;

                var contract = attribute as ServiceBase.ServiceGetContractAttribute;
                if (contract == null)
                    continue;

                bool isPut = contract is ServiceBase.ServicePutContractAttribute;

                var mapper = new ContractMapper("/" + service.Name + "/" + contract.uri, method);
                string uri = mapper.GetMappedUri();

                Func<dynamic, dynamic> lambda = parameters =>
                {
                    var arguments = new List<object>();

                    mapper.MapArguments(parameters, Request.Query, arguments);

                    if (mapper.BindBody && mapper.DynamicBody)
                    {
                        // Bind manually for now until I've fixed the dynamic type.

                        // Attempt to deserialize body from Json

                        Nancy.DynamicDictionary body = null;
                        if (Request.Body.Length > 0)
                        {
                            var buffer = new byte[Request.Body.Length];
                            Request.Body.Position = 0;
                            Request.Body.Read(buffer, 0, (int)Request.Body.Length);
                            string bodyStr = Encoding.Default.GetString(buffer);
                            Log.Debug("Got body data:\n{0}", bodyStr);

                            var serializer = new Nancy.Json.JavaScriptSerializer();
                            try
                            {
                                var bodyJson = serializer.DeserializeObject(bodyStr) as System.Collections.Generic.Dictionary<string, object>;
                                if (bodyJson != null)
                                    body = Nancy.DynamicDictionary.Create(bodyJson);
                            }
                            catch (System.ArgumentException)
                            {
                                // Just eat it.
                                Log.Warning("Got request with invalid json body for url: " + Request.Url);
                                return null;
                            }
                        }

                        arguments.Add(body);
                    }

                    else if (mapper.BindBody)
                    {
                        // Bind specific type.

                        var config = new BindingConfig();
                        config.BodyOnly = true;
                        config.IgnoreErrors = false;

                        // The Bind<> method exists on the ModuleExtension rather than the NancyModule.
                        var extensionMethods = typeof(ModuleExtensions).GetMethods();
                        var methodList = new List<MethodInfo>(extensionMethods);

                        // Get correct generic bind method
                        var bindMethod = methodList.Find(x => x.Name == "Bind" && x.GetParameters().Length == 1 && x.IsGenericMethod == true);
                        var genericMethod = bindMethod.MakeGenericMethod(mapper.BodyType);

                        // Bind our object.
                        var boundBody = genericMethod.Invoke(null, new object[] { this });
                        arguments.Add(boundBody);
                    }

                    try
                    {
                        object result = method.Invoke(service, arguments.ToArray());

                        return Response.AsJson(result);
                    }
                    catch (TargetInvocationException e)
                    {
                        Log.Error(
                            "Invocation exception of uri: " + uri + "\n"
                            + "Exception: " + e.Message + "\n"
                            + "Callstack:" + e.StackTrace + "\n"
                            + "Inner: " + e.InnerException.Message + "\n"
                            + "Callstack: " + e.InnerException.StackTrace);

                        // TODO - A better way to unwrap this? Just throwing inner exception will cause an ObjectDisposedException
                        throw new Exception(e.InnerException.Message);
                    }
                };

                if (isPut)
                {
                    //Log.Debug("Adding PUT binding for {0}", uri);
                    Put[uri] = lambda;
                    Post[uri] = lambda;
                }
                else
                {
                    //Log.Debug("Adding GET binding for {0}", uri);
                    Get[uri] = lambda;
                }
            }
        }
    }
}
