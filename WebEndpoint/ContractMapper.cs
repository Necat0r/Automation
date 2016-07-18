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
        private string[] mUriParameters;
        private string[] mQueryParameters;

        private string mUri;
        private bool mBindBody = false;

        public bool BindBody { get { return mBindBody; } }
        public bool DynamicBody { get; set; }
        public Type BodyType { get; set; }

        public ContractMapper(string uri, MethodInfo method)
        {
            mMethod = method;

            var uriParts = uri.Split('?');
            uri = uriParts[0];

            // Parse normal uri. Every second element will be static content
            char[] delimeters = { '{', '}' };
            var words = uri.Split(delimeters);

            mUriParameters = (from index in Enumerable.Range(0, words.Length) where index % 2 == 1 select words[index]).ToArray();

            // Parse query parameters
            if (uriParts.Length > 1)
            {
                var queryWords = uriParts[1].Split('&');

                mQueryParameters = (from word in queryWords select word.Replace("{", "").Replace("}", "")).ToArray();
            }
            else
            {
                mQueryParameters = new string[] { };
            }

            // TODO - Need to match types here as well!

            var methodParameters = method.GetParameters().ToDictionary(key => key.Name, value => value);

            var parameterCount = mUriParameters.Length + mQueryParameters.Length;

            // Make sure the in-parameters match up except for the last body parameter
            if (parameterCount > methodParameters.Count)
                throw new ArgumentException(string.Format("Too few parameters for contract method: {0}", method.Name));

            if (methodParameters.Count > parameterCount + 1)
                throw new ArgumentException(string.Format("Too many parameters for contract method: {0}", method.Name));

            // If there's one additional parameter it would be the body.
            DynamicBody = false;
            if (methodParameters.Count > parameterCount)
            {
                mBindBody = true;
                var methodParams = method.GetParameters();
                BodyType = methodParams[methodParams.Length - 1].ParameterType;

                // Doesn't seem to be any better way to classify it as being of a dynamic type.
                DynamicBody = BodyType == typeof(object);
            }

            // Reconstruct the URI.
            string mappedUri = "";
            for (int i = 0; i < words.Length; ++i)
            {
                if (i % 2 == 0)
                {
                    // Just static part of the URI
                    mappedUri += words[i];
                    continue;
                }

                // Ensure the uri name match one of the method parameters
                ParameterInfo info;
                bool result = methodParameters.TryGetValue(words[i], out info);
                if (!result)
                    throw new ArgumentException(string.Format("Parameter '{0}' does not correspond to a matching method parameter.", words[i]));

                mappedUri += string.Format("{{{0}}}", words[i]);

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

        public void MapArguments(DynamicDictionary parameters, DynamicDictionary queryParameters, List<object> arguments)
        {
            foreach (var parameterName in mUriParameters)
                arguments.Add(parameters[parameterName].Value);

            foreach (var parameterName in mQueryParameters)
                arguments.Add(queryParameters[parameterName].Value);
        }
    }
}
