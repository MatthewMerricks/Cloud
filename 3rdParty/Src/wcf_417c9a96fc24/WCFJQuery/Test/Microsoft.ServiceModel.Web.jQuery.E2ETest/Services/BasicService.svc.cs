namespace Microsoft.ServiceModel.Web.E2ETest.Services
{
    using System;
    using System.ComponentModel.DataAnnotations;
    using System.Globalization;
    using System.Json;
    using System.Net;
    using System.ServiceModel;
    using System.ServiceModel.Activation;
    using System.ServiceModel.Description;
    using System.ServiceModel.Web;
    using System.Text;

    public class BasicService : IBasicService
    {
        public JsonValue SumQueryStringParameters()
        {
            WebOperationContext.Current.OutgoingResponse.Headers[HttpResponseHeader.CacheControl] = "no-cache";
            JsonValue queryParams = WebOperationContext.Current.IncomingRequest.GetQueryStringAsJsonObject();
            if (queryParams.JsonType == JsonType.Object)
            {
                double result = 0;
                JsonObject objParams = (JsonObject)queryParams;
                foreach (var name in objParams.Keys)
                {
                    JsonValue value = objParams[name];
                    if (value.JsonType == JsonType.String)
                    {
                        result += (double)value;
                    }
                }

                return result;
            }
            else
            {
                return 0;
            }
        }

        public JsonValue EchoJsonValue(JsonValue input)
        {
            return input;
        }

        public JsonValue EchoJsonValueGet()
        {
            WebOperationContext.Current.OutgoingResponse.Headers[HttpResponseHeader.CacheControl] = "no-cache";
            JsonValue input = WebOperationContext.Current.IncomingRequest.GetQueryStringAsJsonObject();
            return input;
        }

        public string ConvertIntoCLRType(string keyName, string typeName, string toStringFormat, JsonValue input)
        {
            switch (typeName.ToLowerInvariant())
            {
                case "datetime":
                    DateTime dt = (DateTime)input[keyName];
                    return dt.ToString(toStringFormat, CultureInfo.InvariantCulture);
                case "int":
                    return ((int)input[keyName]).ToString(CultureInfo.InvariantCulture);
                case "short":
                    return ((short)input[keyName]).ToString(CultureInfo.InvariantCulture);
                case "long":
                    return ((long)input[keyName]).ToString(CultureInfo.InvariantCulture);
                case "sbyte":
                    return ((sbyte)input[keyName]).ToString(CultureInfo.InvariantCulture);
                case "byte":
                    return ((byte)input[keyName]).ToString(CultureInfo.InvariantCulture);
                case "ushort":
                    return ((ushort)input[keyName]).ToString(CultureInfo.InvariantCulture);
                case "uint":
                    return ((uint)input[keyName]).ToString(CultureInfo.InvariantCulture);
                case "ulong":
                    return ((ulong)input[keyName]).ToString(CultureInfo.InvariantCulture);
                case "decimal":
                    return ((decimal)input[keyName]).ToString(CultureInfo.InvariantCulture);
                case "double":
                    return ((double)input[keyName]).ToString("R", CultureInfo.InvariantCulture);
                case "float":
                    return ((float)input[keyName]).ToString("R", CultureInfo.InvariantCulture);
                default:
                    return "Type " + typeName + " not implemented yet; this will fail the client assertion.";
            }
        }

        public JsonValue EchoDate(string keyName, JsonValue input)
        {
            return new JsonPrimitive(input[keyName].ReadAs<DateTime>());
        }

        public JsonValue ValidateJsonValue(string keyName, string validationMethod, JsonValue input)
        {
            JsonObject jo = (JsonObject)input;

            switch (validationMethod.ToLowerInvariant())
            {
                case "validatepresence":
                    jo.ValidatePresence(keyName);
                    break;
                case "validaterange":
                    jo.ValidateRange(keyName, 0, 10);
                    break;
                case "validateregex":
                    jo.ValidateRegularExpression(keyName, "^.{10}$");
                    break;
                case "validatestringlength":
                    jo.ValidateStringLength(keyName, 10);
                    break;
                case "validatetypeofint":
                    jo.ValidateTypeOf<int>(keyName);
                    break;
                case "validatecustomsimple":
                    jo.ValidateCustomValidator(keyName, typeof(MyCustomValidationClass), "IsStringContainsCharSimple");
                    break;
                case "validatecustomcomplex":
                    jo.ValidateCustomValidator(keyName, typeof(MyCustomValidationClass), "IsStringContainsCharComplex");
                    break;
                default:
                    throw new InvalidOperationException(String.Format("Validation test '{0}' is not defined.", validationMethod));
            }

            return input;
        }

        public class MyCustomValidationClass
        {
            public static ValidationResult IsStringContainsCharSimple(JsonValue jv)
            {
                string str = jv.ReadAs<string>();

                if (str.Contains("Char"))
                {
                    return ValidationResult.Success;
                }

                return new ValidationResult("String must contain 'Char'");
            }

            public static ValidationResult IsStringContainsCharComplex(JsonValue jv, ValidationContext context)
            {
                JsonValue value = (JsonValue)context.ObjectInstance;

                string strValue;
                if (value[context.MemberName].TryReadAs<string>(out strValue) && strValue.Contains("Char"))
                {
                    return ValidationResult.Success;
                }

                return new ValidationResult("String must contain 'Char'", new System.Collections.Generic.List<string> { context.MemberName });
            }
        }
    }

    public class BasicServiceHostFactory : ServiceHostFactory
    {
        protected override ServiceHost CreateServiceHost(Type serviceType, Uri[] baseAddresses)
        {
            return new BasicServiceHost(serviceType, baseAddresses);
        }
    }

    class BasicServiceHost : ServiceHost
    {
        public BasicServiceHost(Type serviceType, Uri[] baseAddresses)
            : base(serviceType, baseAddresses)
        {
        }

        protected override void InitializeRuntime()
        {
            WebHttpBinding binding = new WebHttpBinding();
            ServiceEndpoint endpoint = this.AddServiceEndpoint(typeof(IBasicService), binding, "");
            endpoint.Behaviors.Add(new WebHttpBehavior3());

            binding = new WebHttpBinding();
            binding.WriteEncoding = Encoding.Unicode;
            endpoint = this.AddServiceEndpoint(typeof(IBasicService), binding, "utf16le");
            endpoint.Behaviors.Add(new WebHttpBehavior3());

            binding = new WebHttpBinding();
            binding.WriteEncoding = Encoding.BigEndianUnicode;
            endpoint = this.AddServiceEndpoint(typeof(IBasicService), binding, "utf16be");
            endpoint.Behaviors.Add(new WebHttpBehavior3());

            base.InitializeRuntime();
        }
    }
}
