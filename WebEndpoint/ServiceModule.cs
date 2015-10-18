using Module;
using Nancy;
using System;
using System.Text;
using System.Dynamic;
using System.Reflection;
using System.Linq;

namespace WebEndpoint
{
    public class ServiceModule : NancyModule
    {
        public ServiceModule(ServiceManager serviceManager)
        {
            Console.WriteLine("Number of registered services {0}", serviceManager.Services.Count);

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
                string uri = "/" + service.Name + "/" + contract.uri;

                // Validate parameters. Every second element will be a parameter while the rest is just static uri content
                char[] delimeters = { '{', '}' };
                string[] words = uri.Split(delimeters);

                for (int i = 1; i < words.Length; i += 2)
                {

                    
                }


                      
                // Make sure there's only a single in-parameters at most
                var paramCount = method.GetParameters().Length;
                if (paramCount > 2)
                    throw new ArgumentException(string.Format("Invalid number or parameters for contract method: {0}::{1}", service.GetType().Name, method.Name));

                // TODO - Check that types match!
                var isDynamic = false;
                if (paramCount == 2)
                {
                    var bodyType = method.GetParameters()[1].GetType();
                    isDynamic = bodyType.IsSubclassOf(typeof(DynamicObject));
                }


                // Map parameters to binding.

                Func<dynamic, dynamic> lambda = parameters =>
                {
                    //DynamicDictionary parameters = this.Bind<DynamicDictionary>();
                    Nancy.DynamicDictionary body = null;

                    // Attempt to deserialize body from Json
                    if (Request.Body.Length > 0)
                    {
                        var buffer = new byte[Request.Body.Length];
                        Request.Body.Position = 0;
                        Request.Body.Read(buffer, 0, (int)Request.Body.Length);
                        string bodyStr = Encoding.Default.GetString(buffer);
                        Console.WriteLine("Got body data:\n{0}", bodyStr);

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
                            Console.WriteLine("Got request with invalid json body for url: " + Request.Url);
                            return null;
                        }
                    }
                            
                    try
                    {
                        object[] arguments;

                        if (method.GetParameters().Length == 2)
                            arguments = new object[] { parameters, body };
                        else if (method.GetParameters().Length == 1)
                            arguments = new object[] { parameters };
                        else
                            arguments = new object[] { };

                        object result = method.Invoke(service, arguments);

                        return Response.AsJson(result);
                    }
                    catch (TargetInvocationException e)
                    {
                        Console.WriteLine(
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
                    //Console.WriteLine("Adding PUT binding for {0}", uri);
                    Put[uri] = lambda;
                    Post[uri] = lambda;
                }
                else
                {
                    //Console.WriteLine("Adding GET binding for {0}", uri);
                    Get[uri] = lambda;
                }
            }
        }
    }
}
