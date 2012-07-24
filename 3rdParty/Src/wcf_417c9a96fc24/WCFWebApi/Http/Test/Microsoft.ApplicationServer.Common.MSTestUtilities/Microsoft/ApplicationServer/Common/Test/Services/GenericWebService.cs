// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Common.Test.Services
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Reflection;
    using System.ServiceModel;
    using System.ServiceModel.Web;

    public class GenericWebService
    {
        public const string UriTemplate = "SomePath";

        private static readonly Type xmlSerializerGenericWebServiceType = typeof(XmlSerializerGenericWebService<,>);
        private static readonly Type dataContractGenericWebServiceType = typeof(DataContractGenericWebService<,>);
        
        public Func<object> OnGetReturnInstance { get; set; }

        public static GenericWebService GetServiceInstance(Type contractType, Type knownType, bool useDataContract = false)
        {
            Type genericWebServiceType = useDataContract ?
                dataContractGenericWebServiceType :
                xmlSerializerGenericWebServiceType;

            Type closedWebServiceType = genericWebServiceType.MakeGenericType(contractType, knownType);
            ConstructorInfo constructor = closedWebServiceType.GetConstructor(new Type[] { });
            return constructor.Invoke(new object[] { }) as GenericWebService;
        }

        public static HttpRequestMessage GetRequest()
        {
            return new HttpRequestMessage(HttpMethod.Get, new Uri(UriTemplate, UriKind.Relative));
        }

        public static HttpRequestMessage InvokeRequest()
        {
            return new HttpRequestMessage(HttpMethod.Post, new Uri(UriTemplate, UriKind.Relative));
        }
    }

    public class GenericWebService<TContract, TKnown> : GenericWebService
    {
        protected TContract GetReturnInstance()
        {
            Func<object> localOnGetReturnInstance = this.OnGetReturnInstance;
            object obj = null;

            if (localOnGetReturnInstance != null)
            {
                obj = this.OnGetReturnInstance();
            }

            return (obj == null) ? default(TContract) : (TContract)obj;
        }
    }

    [ServiceContract]
    [ServiceBehavior(Name = "GenericWebService")]
    [ServiceKnownType("GetKnownType")]
    [XmlSerializerFormat]
    public class XmlSerializerGenericWebService<TContract, TKnown> : GenericWebService<TContract, TKnown>
    {
        public static IEnumerable<Type> GetKnownType(ICustomAttributeProvider provider)
        {
            if (typeof(TContract) != typeof(TKnown))
            {
                return new Type[] { typeof(TKnown) };
            }

            return new Type[0];
        }

        [WebGet(UriTemplate = GenericWebService.UriTemplate)]
        public TContract Get()
        {
            return GetReturnInstance();
        }

        [WebInvoke(UriTemplate = GenericWebService.UriTemplate)]
        public TContract Invoke(TContract instance)
        {
            return instance;
        }
    }

    [ServiceContract]
    [ServiceBehavior(Name = "GenericWebService")]
    [ServiceKnownType("GetKnownType")]
    [DataContractFormat]
    public class DataContractGenericWebService<TContract, TKnown> : GenericWebService<TContract, TKnown>
    {
        public static IEnumerable<Type> GetKnownType(ICustomAttributeProvider provider)
        {
            if (typeof(TContract) != typeof(TKnown))
            {
                return new Type[] { typeof(TKnown) };
            }

            return new Type[0];
        }

        [WebGet(UriTemplate = GenericWebService.UriTemplate)]
        public TContract Get()
        {
            return GetReturnInstance();
        }

        [WebInvoke(UriTemplate = GenericWebService.UriTemplate)]
        public TContract Invoke(TContract instance)
        {
            return instance;
        }
    }
}
