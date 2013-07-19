using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace CheckAssignablePrimitives
{
    class Program
    {
        static void Main(string[] args)
        {
            Type[] primitives = new[]
            {
                typeof(bool),
                typeof(byte),
                typeof(sbyte),
                typeof(short),
                typeof(ushort),
                typeof(int),
                typeof(uint),
                typeof(long),
                typeof(ulong),
                typeof(IntPtr),
                typeof(UIntPtr),
                typeof(char),
                typeof(double),
                typeof(float)
            };

            bool[][] primitiveCastSuccess = new bool[primitives.Length][];

            Dictionary<Type, List<ValidCast>> validCasts = new Dictionary<Type, List<ValidCast>>();

            for (int primitiveIndex = 0; primitiveIndex < primitives.Length; primitiveIndex++)
            {
                primitiveCastSuccess[primitiveIndex] = new bool[primitives.Length];
                for (int innerIndex = 0; innerIndex < primitives.Length; innerIndex++)
                {
                    bool isValid = TryCastType(primitives[primitiveIndex], primitives[innerIndex]);

                    primitiveCastSuccess[primitiveIndex][innerIndex] = isValid;

                    if (isValid
                        && primitiveIndex != innerIndex)
                    {
                        List<ValidCast> validCastsForCurrentFromType;
                        if (!validCasts.TryGetValue(primitives[primitiveIndex], out validCastsForCurrentFromType))
                        {
                            validCasts.Add(primitives[primitiveIndex], validCastsForCurrentFromType = new List<ValidCast>());
                        }

                        validCastsForCurrentFromType.Add(new ValidCast(primitives[primitiveIndex], primitives[innerIndex]));
                    }
                }
            }

            StringBuilder dictionaryBuilder = new StringBuilder(
                "private static readonly Dictionary<Type, HashSet<Type>> convertablePrimitivesKeyedByFromType = new Dictionary<Type, HashSet<Type>>()" + Environment.NewLine +
                "{" + Environment.NewLine +
                CreateTab());

            bool firstAppend = true;
            foreach (KeyValuePair<Type, List<ValidCast>> currentCasts in validCasts)
            {
                if (firstAppend)
                {
                    firstAppend = false;
                }
                else
                {
                    dictionaryBuilder.Append("," + Environment.NewLine + CreateTab());
                }

                dictionaryBuilder.Append("{" + Environment.NewLine +
                    CreateTab(2) + "typeof(" + currentCasts.Key.Name + ")," + Environment.NewLine +
                    CreateTab(2) + "new HashSet<Type>()" + Environment.NewLine +
                        CreateTab(3) + "{" + Environment.NewLine +
                            CreateTab(4) + "typeof(" + string.Join(")," + Environment.NewLine +
                            CreateTab(4) + "typeof(",
                            currentCasts.Value.Select(currentCast => currentCast.ToType.Name)) + ")" + Environment.NewLine +
                        CreateTab(3) + "}" + Environment.NewLine +
                    CreateTab(1) + "}");
            }

            dictionaryBuilder.Append(Environment.NewLine + "};");
        }

        private static string CreateTab(int tabCount = 1)
        {
            return new string(' ', tabCount * 4);
        }

        private sealed class ValidCast
        {
            public Type FromType
            {
                get
                {
                    return _fromType;
                }
            }
            private readonly Type _fromType;

            public Type ToType
            {
                get
                {
                    return _toType;
                }
            }
            private readonly Type _toType;

            public ValidCast(Type FromType, Type ToType)
            {
                this._fromType = FromType;
                this._toType = ToType;
            }
        }

        private static bool TryCastType(Type from, Type to)
        {
            try
            {
                object fromValue = Activator.CreateInstance(from);

                ParameterExpression fromExpression = Expression.Parameter(from);

                UnaryExpression castExpression = Expression.Convert(
                    fromExpression,
                    to);

                LambdaExpression castLambda = Expression.Lambda(castExpression, fromExpression);

                Delegate compiledLambda = castLambda.Compile();

                return compiledLambda.DynamicInvoke(fromValue).GetType() == to;
            }
            catch
            {
                return false;
            }
        }
    }
}