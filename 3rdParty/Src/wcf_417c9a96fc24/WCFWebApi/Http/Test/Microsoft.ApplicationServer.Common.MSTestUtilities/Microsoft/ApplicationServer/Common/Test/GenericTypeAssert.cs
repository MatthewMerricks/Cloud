// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.ApplicationServer.Common.Test
{
    using System;
    using System.Linq;
    using System.Reflection;
    using Microsoft.ApplicationServer.Common.Test.Types;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// Test assertion class to provide convenience and assert methods for generic types
    /// whose type parameters are not known at compile time.
    /// </summary>
    public static class GenericTypeAssert
    {
        private static readonly Type genericDataContractType = typeof(GenericDataContractType<>);
        private static readonly Type genericXmlSerializableType = typeof(GenericXmlSerializableType<>);

        /// <summary>
        /// Asserts the given <paramref name="genericBaseType"/> is a generic type and creates a new
        /// bound generic type using <paramref name="genericParameterType"/>.  It then asserts there
        /// is a constructor that will accept <paramref name="parameterTypes"/> and returns it.
        /// </summary>
        /// <param name="genericBaseType">The unbound generic base type.</param>
        /// <param name="genericParameterType">The type of the single generic parameter to apply to create a bound generic type.</param>
        /// <param name="parameterTypes">The list of parameter types for a constructor that must exist.</param>
        /// <returns>The <see cref="ConstructorInfo"/> of that constructor which may be invoked to create that new generic type.</returns>
        public static ConstructorInfo GetConstructor(Type genericBaseType, Type genericParameterType, params Type[] parameterTypes)
        {
            Assert.IsNotNull(genericBaseType == null, "Test error: genericBaseType must be specified.");
            Assert.IsTrue(genericBaseType.IsGenericTypeDefinition, "Test error: " + genericBaseType.Name + " is not a valid generic type.");
            Assert.IsNotNull(genericParameterType, "Test error: genericParameterType must be specified.");
            Assert.IsNotNull(parameterTypes, "Test error: parameterTypes must be specified.");

            Type genericType = genericBaseType.MakeGenericType(new Type[] { genericParameterType });
            ConstructorInfo ctor = genericType.GetConstructor(parameterTypes);
            Assert.IsNotNull(ctor, string.Format("Test error: failed to locate generic ctor for type '{0}<{1}>',", genericBaseType.Name, genericParameterType.Name));
            return ctor;
        }

        /// <summary>
        /// Asserts the given <paramref name="genericBaseType"/> is a generic type and creates a new
        /// bound generic type using <paramref name="genericParameterType"/>.  It then asserts there
        /// is a constructor that will accept <paramref name="parameterTypes"/> and returns it.
        /// </summary>
        /// <param name="genericBaseType">The unbound generic base type.</param>
        /// <param name="genericParameterTypes">The types of the generic parameters to apply to create a bound generic type.</param>
        /// <param name="parameterTypes">The list of parameter types for a constructor that must exist.</param>
        /// <returns>The <see cref="ConstructorInfo"/> of that constructor which may be invoked to create that new generic type.</returns>
        public static ConstructorInfo GetConstructor(Type genericBaseType, Type[] genericParameterTypes, params Type[] parameterTypes)
        {
            Assert.IsNotNull(genericBaseType == null, "Test error: genericBaseType must be specified.");
            Assert.IsTrue(genericBaseType.IsGenericTypeDefinition, "Test error: " + genericBaseType.Name + " is not a valid generic type.");
            Assert.IsNotNull(genericParameterTypes, "Test error: genericParameterType must be specified.");
            Assert.IsNotNull(parameterTypes, "Test error: parameterTypes must be specified.");

            Type genericType = genericBaseType.MakeGenericType(genericParameterTypes);
            ConstructorInfo ctor = genericType.GetConstructor(parameterTypes);
            Assert.IsNotNull(ctor, string.Format("Test error: failed to locate generic ctor for type '{0}<>',", genericBaseType.Name));
            return ctor;
        }

        /// <summary>
        /// Creates a new bound generic type and invokes the constructor matched from <see cref="parameterTypes"/>.
        /// </summary>
        /// <param name="genericBaseType">The unbound generic base type.</param>
        /// <param name="genericParameterType">The type of the single generic parameter to apply to create a bound generic type.</param>
        /// <param name="parameterTypes">The list of parameter types for a constructor that must exist.</param>
        /// <param name="parameterValues">The list of values to supply to the constructor</param>
        /// <returns>The instance created by calling that constructor.</returns>
        public static object InvokeConstructor(Type genericBaseType, Type genericParameterType, Type[] parameterTypes, object[] parameterValues)
        {
            ConstructorInfo ctor = GetConstructor(genericBaseType, genericParameterType, parameterTypes);
            Assert.IsNotNull(parameterValues, "Test error: parameterValues must be specified");
            Assert.AreEqual(parameterTypes.Length, parameterValues.Length, "Test error: parameterTypes and parameterValues must agree in length.");
            return ctor.Invoke(parameterValues);
        }

        /// <summary>
        /// Creates a new bound generic type and invokes the constructor matched from <see cref="parameterTypes"/>.
        /// </summary>
        /// <param name="genericBaseType">The unbound generic base type.</param>
        /// <param name="genericParameterTypse">The types of the generic parameters to apply to create a bound generic type.</param>
        /// <param name="parameterTypes">The list of parameter types for a constructor that must exist.</param>
        /// <param name="parameterValues">The list of values to supply to the constructor</param>
        /// <returns>The instance created by calling that constructor.</returns>
        public static object InvokeConstructor(Type genericBaseType, Type[] genericParameterTypes, Type[] parameterTypes, object[] parameterValues)
        {
            ConstructorInfo ctor = GetConstructor(genericBaseType, genericParameterTypes, parameterTypes);
            Assert.IsNotNull(parameterValues, "Test error: parameterValues must be specified");
            Assert.AreEqual(parameterTypes.Length, parameterValues.Length, "Test error: parameterTypes and parameterValues must agree in length.");
            return ctor.Invoke(parameterValues);
        }

        /// <summary>
        /// Creates a new bound generic type and invokes the constructor matched from the types of <paramref name="parameterValues"/>.
        /// </summary>
        /// <param name="genericBaseType">The unbound generic base type.</param>
        /// <param name="genericParameterType">The type of the single generic parameter to apply to create a bound generic type.</param>
        /// <param name="parameterValues">The list of values to supply to the constructor.  It must be possible to determine the</param>
        /// <returns>The instance created by calling that constructor.</returns>
        public static object InvokeConstructor(Type genericBaseType, Type genericParameterType, params object[] parameterValues)
        {
            Assert.IsNotNull(genericBaseType == null, "Test error: genericBaseType must be specified.");
            Assert.IsTrue(genericBaseType.IsGenericTypeDefinition, "Test error: " + genericBaseType.Name + " is not a valid generic type.");
            Assert.IsNotNull(genericParameterType, "Test error: genericParameterType must be specified.");

            Type genericType = genericBaseType.MakeGenericType(new Type[] { genericParameterType });

            ConstructorInfo ctor = FindConstructor(genericType, parameterValues);
            Assert.IsNotNull(ctor, string.Format("Test error: failed to locate generic ctor for type '{0}<{1}>',", genericBaseType.Name, genericParameterType.Name));
            return ctor.Invoke(parameterValues);
        }

        /// <summary>
        /// Creates a new bound generic type and invokes the constructor matched from the types of <paramref name="parameterValues"/>.
        /// </summary>
        /// <param name="genericBaseType">The unbound generic base type.</param>
        /// <param name="genericParameterTypes">The types of the generic parameters to apply to create a bound generic type.</param>
        /// <param name="parameterValues">The list of values to supply to the constructor.  It must be possible to determine the</param>
        /// <returns>The instance created by calling that constructor.</returns>
        public static object InvokeConstructor(Type genericBaseType, Type[] genericParameterTypes, params object[] parameterValues)
        {
            Assert.IsNotNull(genericBaseType == null, "Test error: genericBaseType must be specified.");
            Assert.IsTrue(genericBaseType.IsGenericTypeDefinition, "Test error: " + genericBaseType.Name + " is not a valid generic type.");
            Assert.IsNotNull(genericParameterTypes, "Test error: genericParameterTypes must be specified.");

            Type genericType = genericBaseType.MakeGenericType(genericParameterTypes);

            ConstructorInfo ctor = FindConstructor(genericType, parameterValues);
            Assert.IsNotNull(ctor, string.Format("Test error: failed to locate generic ctor for type '{0}<>',", genericBaseType.Name));
            return ctor.Invoke(parameterValues);
        }

        /// <summary>
        /// Creates a new bound generic type and invokes the constructor matched from <see cref="parameterTypes"/>.
        /// </summary>
        /// <typeparam name="T">The type of object the constuctor is expected to yield.</typeparam>
        /// <param name="genericBaseType">The unbound generic base type.</param>
        /// <param name="genericParameterType">The type of the single generic parameter to apply to create a bound generic type.</param>
        /// <param name="parameterTypes">The list of parameter types for a constructor that must exist.</param>
        /// <param name="parameterValues">The list of values to supply to the constructor</param>
        /// <returns>An instance of type <typeparamref name="T"/>.</returns>
        public static T InvokeConstructor<T>(Type genericBaseType, Type genericParameterType, Type[] parameterTypes, object[] parameterValues)
        {
            ConstructorInfo ctor = GetConstructor(genericBaseType, genericParameterType, parameterTypes);
            Assert.IsNotNull(parameterValues, "Test error: parameterValues must be specified");
            Assert.AreEqual(parameterTypes.Length, parameterValues.Length, "Test error: parameterTypes and parameterValues must agree in length.");
            return (T)ctor.Invoke(parameterValues);
        }

        /// <summary>
        /// Creates a new bound generic type and invokes the constructor matched from <see cref="parameterTypes"/>.
        /// </summary>
        /// <typeparam name="T">The type of object the constuctor is expected to yield.</typeparam>
        /// <param name="genericBaseType">The unbound generic base type.</param>
        /// <param name="genericParameterTypes">The types of the generic parameters to apply to create a bound generic type.</param>
        /// <param name="parameterTypes">The list of parameter types for a constructor that must exist.</param>
        /// <param name="parameterValues">The list of values to supply to the constructor</param>
        /// <returns>An instance of type <typeparamref name="T"/>.</returns>
        public static T InvokeConstructor<T>(Type genericBaseType, Type[] genericParameterTypes, Type[] parameterTypes, object[] parameterValues)
        {
            ConstructorInfo ctor = GetConstructor(genericBaseType, genericParameterTypes, parameterTypes);
            Assert.IsNotNull(parameterValues, "Test error: parameterValues must be specified");
            Assert.AreEqual(parameterTypes.Length, parameterValues.Length, "Test error: parameterTypes and parameterValues must agree in length.");
            return (T)ctor.Invoke(parameterValues);
        }

        /// <summary>
        /// Creates a new bound generic type and invokes the constructor matched from <see cref="parameterTypes"/>.
        /// </summary>
        /// <typeparam name="T">The type of object the constuctor is expected to yield.</typeparam>
        /// <param name="genericBaseType">The unbound generic base type.</param>
        /// <param name="genericParameterType">The type of the single generic parameter to apply to create a bound generic type.</param>
        /// <param name="parameterValues">The list of values to supply to the constructor.  It must be possible to determine the</param>
        /// <returns>The instance created by calling that constructor.</returns>
        /// <returns>An instance of type <typeparamref name="T"/>.</returns>
        public static T InvokeConstructor<T>(Type genericBaseType, Type genericParameterType, params object[] parameterValues)
        {
            Assert.IsNotNull(genericBaseType == null, "Test error: genericBaseType must be specified.");
            Assert.IsTrue(genericBaseType.IsGenericTypeDefinition, "Test error: " + genericBaseType.Name + " is not a valid generic type.");
            Assert.IsNotNull(genericParameterType, "Test error: genericParameterType must be specified.");

            Type genericType = genericBaseType.MakeGenericType(new Type[] { genericParameterType });

            ConstructorInfo ctor = FindConstructor(genericType, parameterValues);
            Assert.IsNotNull(ctor, string.Format("Test error: failed to locate generic ctor for type '{0}<{1}>',", genericBaseType.Name, genericParameterType.Name));
            return (T)ctor.Invoke(parameterValues);
        }

        /// <summary>
        /// Creates a new bound generic type and invokes the constructor matched from <see cref="parameterTypes"/>.
        /// </summary>
        /// <typeparam name="T">The type of object the constuctor is expected to yield.</typeparam>
        /// <param name="genericBaseType">The unbound generic base type.</param>
        /// <param name="genericParameterTypes">The types of the generic parameters to apply to create a bound generic type.</param>
        /// <param name="parameterValues">The list of values to supply to the constructor.  It must be possible to determine the</param>
        /// <returns>The instance created by calling that constructor.</returns>
        /// <returns>An instance of type <typeparamref name="T"/>.</returns>
        public static T InvokeConstructor<T>(Type genericBaseType, Type[] genericParameterTypes, params object[] parameterValues)
        {
            Assert.IsNotNull(genericBaseType == null, "Test error: genericBaseType must be specified.");
            Assert.IsTrue(genericBaseType.IsGenericTypeDefinition, "Test error: " + genericBaseType.Name + " is not a valid generic type.");
            Assert.IsNotNull(genericParameterTypes, "Test error: genericParameterType must be specified.");

            Type genericType = genericBaseType.MakeGenericType(genericParameterTypes);

            ConstructorInfo ctor = FindConstructor(genericType, parameterValues);
            Assert.IsNotNull(ctor, string.Format("Test error: failed to locate generic ctor for type '{0}<>',", genericBaseType.Name));
            return (T)ctor.Invoke(parameterValues);
        }

        /// <summary>
        /// Asserts the given instance is one from a generic type of the specified parameter type.
        /// </summary>
        /// <typeparam name="T">The type of instance.</typeparam>
        /// <param name="instance">The instance to test.</param>
        /// <param name="genericTypeParameter">The type of the generic parameter to which the instance's generic type should have been bound.</param>
        public static void IsCorrectGenericType<T>(T instance, Type genericTypeParameter)
        {
            Assert.IsNotNull(instance, string.Format("Test error: instance of type '{0}' cannot be null.", typeof(T).Name));
            Assert.IsNotNull(genericTypeParameter, "Test error: genericTypeParameter must be specified.");
            Assert.IsTrue(instance.GetType().IsGenericType, string.Format("instance must be generic type of '{0}'", genericTypeParameter.Name));
            Type[] genericArguments = instance.GetType().GetGenericArguments();
            Assert.AreEqual(1, genericArguments.Length, "genericArgument count was wrong.");
            Assert.AreEqual(genericTypeParameter, genericArguments[0], "generic argument was wrong.");
        }

        /// <summary>
        /// Invokes via Reflection the method on the given instance.
        /// </summary>
        /// <param name="instance">The instance to use.</param>
        /// <param name="methodName">The name of the method to call.</param>
        /// <param name="parameterTypes">The types of the parameters to the method.</param>
        /// <param name="parameterValues">The values to supply to the method.</param>
        /// <returns>The results of the method.</returns>
        public static object InvokeMethod(object instance, string methodName, Type[] parameterTypes, object[] parameterValues)
        {
            Assert.IsNotNull(instance, "Test error: instance cannot be null.");
            Assert.IsNotNull(parameterTypes, "Test error: parameterTypes must be specified.");
            Assert.IsNotNull(parameterValues, "Test error: parameterValues must be specified.");
            Assert.AreEqual(parameterTypes.Length, parameterValues.Length, "ParameterTypes and ParameterValues must be same size.");
            MethodInfo methodInfo = instance.GetType().GetMethod(methodName, parameterTypes);
            Assert.IsNotNull(methodInfo, string.Format("The method '{0}' could not be found on type '{1}'.", methodName, instance.GetType().Name));
            return methodInfo.Invoke(instance, parameterValues);
        }

        /// <summary>
        /// Invokes via Reflection the static method on the given type.
        /// </summary>
        /// <param name="type">The type containing the method.</param>
        /// <param name="methodName">The name of the method to call.</param>
        /// <param name="parameterTypes">The types of the parameters to the method.</param>
        /// <param name="parameterValues">The values to supply to the method.</param>
        /// <returns>The results of the method.</returns>
        public static object InvokeMethod(Type type, string methodName, Type[] parameterTypes, object[] parameterValues)
        {
            Assert.IsNotNull(type, "Test error: type cannot be null.");
            Assert.IsNotNull(parameterTypes, "Test error: parameterTypes must be specified.");
            Assert.IsNotNull(parameterValues, "Test error: parameterValues must be specified.");
            Assert.AreEqual(parameterTypes.Length, parameterValues.Length, "ParameterTypes and ParameterValues must be same size.");
            MethodInfo methodInfo = type.GetMethod(methodName, parameterTypes);
            Assert.IsNotNull(methodInfo, string.Format("The method '{0}' could not be found on type '{1}'.", methodName, type.Name));
            return methodInfo.Invoke(null, parameterValues);
        }

        /// <summary>
        /// Invokes via Reflection the static method on the given type.
        /// </summary>
        /// <param name="type">The type containing the method.</param>
        /// <param name="methodName">The name of the method to call.</param>
        /// <param name="genericParameterType">The generic parameter type of the method.</param>
        /// <param name="parameterTypes">The types of the parameters to the method.</param>
        /// <param name="parameterValues">The values to supply to the method.</param>
        /// <returns>The results of the method.</returns>
        public static MethodInfo CreateGenericMethod(Type type, string methodName, Type genericParameterType, Type[] parameterTypes)
        {
            Assert.IsNotNull(type, "Test error: type cannot be null.");
            Assert.IsNotNull(parameterTypes, "Test error: parameterTypes must be specified.");
            Assert.IsNotNull(genericParameterType, "Test error: genericParameterType must be specified.");
            //MethodInfo methodInfo = type.GetMethod(methodName, parameterTypes);
            MethodInfo methodInfo = type.GetMethods().Where((m) => m.Name.Equals(methodName, StringComparison.OrdinalIgnoreCase) && m.IsGenericMethod && AreAssignableFrom(m.GetParameters(), parameterTypes)).FirstOrDefault();
            Assert.IsNotNull(methodInfo, string.Format("The method '{0}' could not be found on type '{1}'.", methodName, type.Name));
            Assert.IsTrue(methodInfo.IsGenericMethod, string.Format("The method '{0}' on type '{1}' is not a generic method.", methodName, type.Name));
            MethodInfo genericMethod = methodInfo.MakeGenericMethod(genericParameterType);
            Assert.IsNotNull(genericMethod, "Could not create a generic method.");
            return genericMethod;
        }

        /// <summary>
        /// Invokes via Reflection the static generic method on the given type.
        /// </summary>
        /// <param name="type">The type containing the method.</param>
        /// <param name="methodName">The name of the method to call.</param>
        /// <param name="genericParameterType">The generic parameter type of the method.</param>
        /// <param name="parameterTypes">The types of the parameters to the method.</param>
        /// <param name="parameterValues">The values to supply to the method.</param>
        /// <returns>The results of the method.</returns>
        public static object InvokeGenericMethod(Type type, string methodName, Type genericParameterType, Type[] parameterTypes, object[] parameterValues)
        {
            MethodInfo methodInfo = CreateGenericMethod(type, methodName, genericParameterType, parameterTypes);
            Assert.AreEqual(parameterTypes.Length, parameterValues.Length, "ParameterTypes and ParameterValues must be same size.");
            return methodInfo.Invoke(null, parameterValues);
        }

        /// <summary>
        /// Invokes via Reflection the generic method on the given instance.
        /// </summary>
        /// <param name="instance">The instance on which to invoke the method.</param>
        /// <param name="methodName">The name of the method to call.</param>
        /// <param name="genericParameterType">The generic parameter type of the method.</param>
        /// <param name="parameterTypes">The types of the parameters to the method.</param>
        /// <param name="parameterValues">The values to supply to the method.</param>
        /// <returns>The results of the method.</returns>
        public static object InvokeGenericMethod(object instance, string methodName, Type genericParameterType, Type[] parameterTypes, object[] parameterValues)
        {
            Assert.IsNotNull(instance, "Test error: instance cannot be null.");
            MethodInfo methodInfo = CreateGenericMethod(instance.GetType(), methodName, genericParameterType, parameterTypes);
            Assert.AreEqual(parameterTypes.Length, parameterValues.Length, "ParameterTypes and ParameterValues must be same size.");
            return methodInfo.Invoke(instance, parameterValues);
        }

        /// <summary>
        /// Invokes via Reflection the generic method on the given instance.
        /// </summary>
        /// <typeparam name="T">The type of the return value from the method.</typeparam>
        /// <param name="instance">The instance on which to invoke the method.</param>
        /// <param name="methodName">The name of the method to call.</param>
        /// <param name="genericParameterType">The generic parameter type of the method.</param>
        /// <param name="parameterTypes">The types of the parameters to the method.</param>
        /// <param name="parameterValues">The values to supply to the method.</param>
        /// <returns>The results of the method.</returns>
        public static T InvokeGenericMethod<T>(object instance, string methodName, Type genericParameterType, Type[] parameterTypes, object[] parameterValues)
        {
            return (T)InvokeGenericMethod(instance, methodName, genericParameterType, parameterTypes, parameterValues);
        }

        /// <summary>
        /// Invokes via Reflection the method on the given instance.
        /// </summary>
        /// <param name="instance">The instance to use.</param>
        /// <param name="methodName">The name of the method to call.</param>
        /// <param name="parameterValues">The values to supply to the method.</param>
        /// <returns>The results of the method.</returns>
        public static object InvokeMethod(object instance, string methodName, params object[] parameterValues)
        {
            Assert.IsNotNull(instance, "Test error: instance cannot be null.");
            MethodInfo methodInfo = FindMethod(instance.GetType(), methodName, parameterValues);
            Assert.IsNotNull(methodInfo, string.Format("Test error: the method '{0}' could not be found on type '{1}'.", methodName, instance.GetType().Name));
            return methodInfo.Invoke(instance, parameterValues);
        }

        /// <summary>
        /// Invokes via Reflection the static method on the given type.
        /// </summary>
        /// <param name="instance">The instance to use.</param>
        /// <param name="methodName">The name of the method to call.</param>
        /// <param name="parameterValues">The values to supply to the method.</param>
        /// <returns>The results of the method.</returns>
        public static object InvokeMethod(Type type, string methodName, params object[] parameterValues)
        {
            Assert.IsNotNull(type, "Test error: type cannot be null.");
            MethodInfo methodInfo = FindMethod(type, methodName, parameterValues);
            Assert.IsNotNull(methodInfo, string.Format("Test error: the method '{0}' could not be found on type '{1}'.", methodName, type.Name));
            return methodInfo.Invoke(null, parameterValues);
        }

        /// <summary>
        /// Invokes via Reflection the method on the given instance.
        /// </summary>
        /// <param name="instance">The instance to use.</param>
        /// <param name="methodName">The name of the method to call.</param>
        /// <param name="genericParameterType">The type of the generic parameter.</param>
        /// <param name="parameterValues">The values to supply to the method.</param>
        /// <returns>The results of the method.</returns>
        public static object InvokeGenericMethod(object instance, string methodName, Type genericParameterType, params object[] parameterValues)
        {
            Assert.IsNotNull(instance, "Test error: instance cannot be null.");
            Assert.IsNotNull(genericParameterType, "Test error: genericParameterType cannot be null.");
            MethodInfo methodInfo = FindMethod(instance.GetType(), methodName, parameterValues);
            Assert.IsNotNull(methodInfo, string.Format("Test error: the method '{0}' could not be found on type '{1}'.", methodName, instance.GetType().Name));
            Assert.IsTrue(methodInfo.IsGenericMethod, string.Format("The method '{0}' on type '{1}' is not a generic method.", methodName, instance.GetType().Name));
            MethodInfo genericMethod = methodInfo.MakeGenericMethod(genericParameterType);
            return genericMethod.Invoke(instance, parameterValues);
        }

        /// <summary>
        /// Invokes via Reflection the method on the given instance.
        /// </summary>
        /// <param name="instance">The instance to use.</param>
        /// <param name="methodName">The name of the method to call.</param>
        /// <param name="genericParameterType">The type of the generic parameter.</param>
        /// <param name="parameterValues">The values to supply to the method.</param>
        /// <returns>The results of the method.</returns>
        public static object InvokeGenericMethod(Type type, string methodName, Type genericParameterType, params object[] parameterValues)
        {
            Assert.IsNotNull(type, "Test error: type cannot be null.");
            Assert.IsNotNull(genericParameterType, "Test error: genericParameterType cannot be null.");
            MethodInfo methodInfo = FindMethod(type, methodName, parameterValues);
            Assert.IsNotNull(methodInfo, string.Format("Test error: the method '{0}' could not be found on type '{1}'.", methodName, type.Name));
            Assert.IsTrue(methodInfo.IsGenericMethod, string.Format("The method '{0}' on type '{1}' is not a generic method.", methodName, type.Name));
            MethodInfo genericMethod = methodInfo.MakeGenericMethod(genericParameterType);
            return genericMethod.Invoke(null, parameterValues);
        }

        /// <summary>
        /// Retrieves the value from the specified property.
        /// </summary>
        /// <param name="instance">The instance containing the property value.</param>
        /// <param name="propertyName">The name of the property.</param>
        /// <param name="failureMessage">The error message to prefix any test assertions.</param>
        /// <returns>The value returned from the property.</returns>
        public static object GetProperty(object instance, string propertyName, string failureMessage)
        {
            PropertyInfo propertyInfo = instance.GetType().GetProperty(propertyName);
            Assert.IsNotNull(propertyInfo, string.Format("{0}  The object of type '{1}' did not have the required '{1}' property.", failureMessage, instance.GetType(), propertyName));
            return propertyInfo.GetValue(instance, null);
        }

        /// <summary>
        /// Asserts <paramref name="instance"/> is a <c>GenericDataContractType</c> and returns
        /// the value of its "Value" property.
        /// </summary>
        /// <param name="instance">The instance to test.</param>
        /// <param name="failureMessage">The error message to prefix any test assertions.</param>
        /// <returns>The value returned from the "Value" property.</returns>
        public static object HasGenericDataContractTypeValue(object instance, string failureMessage)
        {
            Type expectedType = instance.GetType();
            bool isGenericDataContractType = expectedType.IsGenericType && expectedType.GetGenericTypeDefinition() == genericDataContractType;

            Assert.IsTrue(
                isGenericDataContractType,
                string.Format("{0}: object was type '{1}' but should have been GenericDataContractType.", failureMessage, instance.GetType().Name));

            return GetProperty(instance, "Value", failureMessage);
        }

        /// <summary>
        /// Asserts <paramref name="instance"/> is a <c>GenericXmlSerializerType</c> and returns
        /// the value of its "Value" property.
        /// </summary>
        /// <param name="instance">The instance to test.</param>
        /// <param name="failureMessage">The error message to prefix any test assertions.</param>
        /// <returns>The value returned from the "Value" property.</returns>
        public static object HasGenericXmlSerializableTypeValue(object instance, string failureMessage)
        {
            Type expectedType = instance.GetType();
            bool isXmlSerializerType = expectedType.IsGenericType && expectedType.GetGenericTypeDefinition() == genericXmlSerializableType;

            Assert.IsTrue(
                isXmlSerializerType,
                string.Format("{0}: object was type '{1}' but should have been GenericXmlSerializableType.", failureMessage, instance.GetType().Name));

            return GetProperty(instance, "Value", failureMessage);
        }

        private static bool AreAssignableFrom(Type[] parameterTypes, params object[] parameterValues)
        {
            Assert.IsNotNull(parameterTypes, "Test error: parameterTypes cannot be null.");
            Assert.IsNotNull(parameterValues, "Test error: parameterValues cannot be null.");
            if (parameterTypes.Length != parameterValues.Length)
            {
                return false;
            }

            for (int i = 0; i < parameterTypes.Length; ++i)
            {
                if (!parameterTypes[i].IsInstanceOfType(parameterValues[i]))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool AreAssignableFrom(ParameterInfo[] parameterInfos, params Type[] parameterTypes)
        {
            Assert.IsNotNull(parameterInfos, "Test error: parameterInfos cannot be null.");
            Assert.IsNotNull(parameterTypes, "Test error: parameterTypes cannot be null.");
            Type[] parameterInfoTypes = parameterInfos.Select<ParameterInfo, Type>((info) => info.ParameterType).ToArray();
            if (parameterInfoTypes.Length != parameterTypes.Length)
            {
                return false;
            }

            for (int i = 0; i < parameterInfoTypes.Length; ++i)
            {
                // Generic parameters are assumed to be assignable
                if (parameterInfoTypes[i].IsGenericParameter)
                {
                    continue;
                }

                if (!parameterInfoTypes[i].IsAssignableFrom(parameterTypes[i]))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool AreAssignableFrom(ParameterInfo[] parameterInfos, params object[] parameterValues)
        {
            Assert.IsNotNull(parameterInfos, "Test error: parameterInfos cannot be null.");
            Assert.IsNotNull(parameterValues, "Test error: parameterValues cannot be null.");
            Type[] parameterTypes = parameterInfos.Select<ParameterInfo, Type>((info) => info.ParameterType).ToArray();
            return AreAssignableFrom(parameterTypes, parameterValues);
        }

        private static ConstructorInfo FindConstructor(Type type, params object[] parameterValues)
        {
            Assert.IsNotNull(type, "type cannot be null.");
            Assert.IsNotNull(parameterValues, "parameterValues cannot be null.");
            return type.GetConstructors().FirstOrDefault((c) => AreAssignableFrom(c.GetParameters(), parameterValues));
        }

        private static MethodInfo FindMethod(Type type, string methodName, params object[] parameterValues)
        {
            Assert.IsNotNull(type, "type cannot be null.");
            Assert.IsFalse(string.IsNullOrWhiteSpace(methodName), "Test error: methodName cannot be empty.");
            Assert.IsNotNull(parameterValues, "parameterValues cannot be null.");
            return type.GetMethods().FirstOrDefault((m) => string.Equals(m.Name, methodName, StringComparison.Ordinal) && AreAssignableFrom(m.GetParameters(), parameterValues));
        }
    }
}
