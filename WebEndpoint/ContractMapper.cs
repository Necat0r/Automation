using Nancy;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Reflection;
using System.Linq;

namespace WebEndpoint
{
    class ContractMapper
    {
        private MethodInfo mMethod;
        private string[] mWords;

        private string mUri;
        private bool mBindBody = false;

        public bool BindBody { get { return mBindBody; } }
        public bool DynamicBody { get; set; }
        public Type BodyType { get; set; }

        public ContractMapper(string uri, MethodInfo method)
        {
            char[] delimeters = { '{', '}' };
            mWords = uri.Split(delimeters);

            mMethod = method;

            var parameters = method.GetParameters().ToDictionary(key => key.Name, value => value);
            var uriParameters = mWords.Length / 2;

            // Validate parameters. Every second element will be a parameter while the rest is just static uri content

            // Make sure the in-parameters match up except for the last body parameter
            if (uriParameters > parameters.Count)
                throw new ArgumentException(string.Format("Too few parameters for contract method: {0}", method.Name));

            if (parameters.Count > uriParameters + 1)
                throw new ArgumentException(string.Format("Too many parameters for contract method: {0}", method.Name));

            DynamicBody = false;
            if (parameters.Count > uriParameters)
            {
                mBindBody = true;
                var methodParams = method.GetParameters();
                BodyType = methodParams[methodParams.Length - 1].ParameterType;

                // Doesn't seem to be any better way to classify it as being of a dynamic type.
                DynamicBody = BodyType == typeof(object);
            }

            // Construct the URI.
            string mappedUri = "";
            for (int i = 0; i < mWords.Length; ++i)
            {
                if (i % 2 == 0)
                {
                    // Just static part of the URI
                    mappedUri += mWords[i];
                    continue;
                }

                // Ensure the uri name match one of the method parameters
                ParameterInfo info;
                bool result = parameters.TryGetValue(mWords[i], out info);
                if (!result)
                    throw new ArgumentException(string.Format("Parameter '{0}' does not correspond to a matching method parameter.", mWords[i]));

                mappedUri += string.Format("{{{0}}}", mWords[i]);

                // TODO - There seems to be a mismatching between the nancy documentation which allows you to specify type
                // In practice it doesn't seem to work (or we're simply doing it wrong...
                //var typeMap = new Dictionary<Type, string> {
                //    { typeof(long), "long" },
                //    { typeof(int), "int" },
                //    { typeof(string), "string" },
                //    { typeof(bool), "bool" }
                //};
                //mappedUri += string.Format("{{{0}:{1}}}", mWords[i], typeMap[info.ParameterType]);
            }

            mUri = mappedUri;
        }

        public string GetMappedUri()
        {
            return mUri;
        }

        public void MapArguments(DynamicDictionary parameters, List<object> arguments)
        {
            for (int i = 1; i < mWords.Length; i+=2)
            {
                arguments.Add(parameters[mWords[i]].Value);
            }
        }
    }
}
