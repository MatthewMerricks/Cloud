// <copyright file="JsonValueDynamicMetaObject.cs" company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace System.Json
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Dynamic;
    using System.Linq.Expressions;
    using System.Reflection;
    using System.Runtime.Serialization.Json;

    /// <summary>
    /// This class provides dynamic behavior support for the JsonValue types.
    /// </summary>
    internal class JsonValueDynamicMetaObject : DynamicMetaObject
    {
        private static readonly MethodInfo GetValueByIndexMethodInfo = typeof(JsonValue).GetMethod("GetValue", new Type[] { typeof(int) });
        private static readonly MethodInfo GetValueByKeyMethodInfo = typeof(JsonValue).GetMethod("GetValue", new Type[] { typeof(string) });
        private static readonly MethodInfo SetValueByIndexMethodInfo = typeof(JsonValue).GetMethod("SetValue", new Type[] { typeof(int), typeof(object) });
        private static readonly MethodInfo SetValueByKeyMethodInfo = typeof(JsonValue).GetMethod("SetValue", new Type[] { typeof(string), typeof(object) });
        private static readonly MethodInfo CastValueMethodInfo = typeof(JsonValue).GetMethod("CastValue", new Type[] { typeof(JsonValue) });
        private static readonly MethodInfo ChangeTypeMethodInfo = typeof(Convert).GetMethod("ChangeType", new Type[] { typeof(object), typeof(Type) });
        private static readonly MethodInfo ReadAsMethodInfo = typeof(JsonValue).GetMethod("ReadAs", new Type[] { typeof(Type) });

        /// <summary>
        /// Class constructor.
        /// </summary>
        /// <param name="parameter">The expression representing this <see cref="DynamicMetaObject"/> during the dynamic binding process.</param>
        /// <param name="value">The runtime value represented by the <see cref="DynamicMetaObject"/>.</param>
        internal JsonValueDynamicMetaObject(Expression parameter, JsonValue value)
            : base(parameter, BindingRestrictions.Empty, value)
        {
        }

        /// <summary>
        /// Represents the level of support for operations.
        /// </summary>
        private enum OperationSupport
        {
            /// <summary>
            /// Operation fully supported on operands.
            /// </summary>
            Supported,

            /// <summary>
            /// Operation not supported on operand.
            /// </summary>
            NotSupported,

            /// <summary>
            /// Operation not supported on a <see cref="JsonValue "/> instance of certain <see cref="JsonType"/> type.
            /// </summary>
            NotSupportedOnJsonType,

            /// <summary>
            /// Operation not supported on second operand type.
            /// </summary>
            NotSupportedOnOperand,

            /// <summary>
            ///  Operation not supported on second operand's value type.
            /// </summary>
            NotSupportedOnValueType
        }

        /// <summary>
        /// Gets the default binding restrictions for this type.
        /// </summary>
        private BindingRestrictions DefaultRestrictions
        {
            get { return BindingRestrictions.GetTypeRestriction(this.Expression, this.LimitType); }
        }

        /// <summary>
        /// Performs the binding of the dynamic unary operation.
        /// </summary>
        /// <param name="binder">An instance of the <see cref="UnaryOperationBinder"/> that represents the details of the dynamic operation.</param>
        /// <returns>The new <see cref="DynamicMetaObject"/> representing the result of the binding.</returns>
        public override DynamicMetaObject BindUnaryOperation(UnaryOperationBinder binder)
        {
            if (binder == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentNullException("binder"));
            }

            Expression operExpression = null;
            JsonValue jsonValue = this.Value as JsonValue;

            if (jsonValue is JsonPrimitive)
            {
                OperationSupport supportValue = GetUnaryOperationSupport(binder.Operation, jsonValue);

                if (supportValue == OperationSupport.Supported)
                {
                    Type operationReturnType = this.GetUnaryOperationReturnType(binder);

                    Expression instance = Expression.Convert(this.Expression, this.LimitType);
                    Expression thisExpression = Expression.Convert(Expression.Call(instance, ReadAsMethodInfo, new Expression[] { Expression.Constant(operationReturnType) }), operationReturnType);

                    operExpression = JsonValueDynamicMetaObject.GetUnaryOperationExpression(binder.Operation, thisExpression);
                }
            }
            
            if (operExpression == null)
            {
                operExpression = JsonValueDynamicMetaObject.GetOperationErrorExpression(OperationSupport.NotSupportedOnJsonType, binder.Operation, jsonValue, null);
            }

            operExpression = Expression.Convert(operExpression, binder.ReturnType);

            return new DynamicMetaObject(operExpression, this.DefaultRestrictions);
        }

        /// <summary>
        /// Performs the binding of the dynamic binary operation.
        /// </summary>
        /// <param name="binder">An instance of the <see cref="BinaryOperationBinder"/> that represents the details of the dynamic operation.</param>
        /// <param name="arg">An instance of the <see cref="DynamicMetaObject"/> representing the right hand side of the binary operation.</param>
        /// <returns>The new <see cref="DynamicMetaObject"/> representing the result of the binding.</returns>
        public override DynamicMetaObject BindBinaryOperation(BinaryOperationBinder binder, DynamicMetaObject arg)
        {
            if (binder == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentNullException("binder"));
            }

            if (arg == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentNullException("arg"));
            }

            Expression thisExpression = this.Expression;
            Expression otherExpression = arg.Expression;
            Expression operExpression = null;

            JsonValue otherValue = arg.Value as JsonValue;
            JsonValue thisValue = this.Value as JsonValue;

            OperationSupport supportValue = JsonValueDynamicMetaObject.GetBinaryOperationSupport(binder.Operation, thisValue, arg.Value);

            if (supportValue == OperationSupport.Supported)
            {
                if (otherValue != null)
                {
                    if (thisValue is JsonPrimitive && otherValue is JsonPrimitive)
                    {
                        //// operation on primitive types.

                        JsonValueDynamicMetaObject.GetBinaryOperandExpressions(binder.Operation, this, arg, ref thisExpression, ref otherExpression);
                    }
                    else
                    {
                        //// operation on JsonValue types.

                        thisExpression = Expression.Convert(thisExpression, typeof(JsonValue));
                        otherExpression = Expression.Convert(otherExpression, typeof(JsonValue));
                    }
                }
                else
                {
                    if (arg.Value != null)
                    {
                        //// operation on JSON primitive and CLR primitive

                        JsonValueDynamicMetaObject.GetBinaryOperandExpressions(binder.Operation, this, arg, ref thisExpression, ref otherExpression);
                    }
                    else
                    { 
                        //// operation on JsonValue and null.

                        thisExpression = Expression.Convert(thisExpression, typeof(JsonValue));

                        if (thisValue.JsonType == JsonType.Default)
                        {
                            thisExpression = Expression.Constant(null);
                        }
                    }
                }

                operExpression = JsonValueDynamicMetaObject.GetBinaryOperationExpression(binder.Operation, thisExpression, otherExpression);
            }
            
            if (operExpression == null)
            {
                operExpression = JsonValueDynamicMetaObject.GetOperationErrorExpression(supportValue, binder.Operation, thisValue, arg.Value);
            }

            operExpression = Expression.Convert(operExpression, typeof(object));

            return new DynamicMetaObject(operExpression, this.DefaultRestrictions);
        }

        /// <summary>
        /// Implements dynamic cast for JsonValue types.
        /// </summary>
        /// <param name="binder">An instance of the <see cref="ConvertBinder"/> that represents the details of the dynamic operation.</param>
        /// <returns>The new <see cref="DynamicMetaObject"/> representing the result of the binding.</returns>
        public override DynamicMetaObject BindConvert(ConvertBinder binder)
        {
            if (binder == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentNullException("binder"));
            }

            Expression expression = this.Expression;

            bool implicitCastSupported =
                binder.Type.IsAssignableFrom(this.LimitType) ||
                binder.Type == typeof(IEnumerable<KeyValuePair<string, JsonValue>>) ||
                binder.Type == typeof(IDynamicMetaObjectProvider) ||
                binder.Type == typeof(object);

            if (!implicitCastSupported)
            {
                if (JsonValue.IsSupportedExplicitCastType(binder.Type))
                {
                    Expression instance = Expression.Convert(this.Expression, this.LimitType);
                    expression = Expression.Call(CastValueMethodInfo.MakeGenericMethod(binder.Type), new Expression[] { instance });
                }
                else
                {
                    string exceptionMessage = SG.GetString(SR.CannotCastJsonValue, this.LimitType.FullName, binder.Type.FullName);
                    expression = Expression.Throw(Expression.Constant(new InvalidCastException(exceptionMessage)), typeof(object));
                }
            }

            expression = Expression.Convert(expression, binder.Type);

            return new DynamicMetaObject(expression, this.DefaultRestrictions);
        }

        /// <summary>
        /// Implements setter for dynamic indexer by index (JsonArray)
        /// </summary>
        /// <param name="binder">An instance of the <see cref="GetIndexBinder"/> that represents the details of the dynamic operation.</param>
        /// <param name="indexes">An array of <see cref="DynamicMetaObject"/> instances - indexes for the get index operation.</param>
        /// <returns>The new <see cref="DynamicMetaObject"/> representing the result of the binding.</returns>
        public override DynamicMetaObject BindGetIndex(GetIndexBinder binder, DynamicMetaObject[] indexes)
        {
            if (binder == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentNullException("binder"));
            }

            if (indexes == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentNullException("indexes"));
            }

            Expression indexExpression;
            if (!JsonValueDynamicMetaObject.TryGetIndexExpression(indexes, out indexExpression))
            {
                return new DynamicMetaObject(indexExpression, this.DefaultRestrictions);
            }

            MethodInfo methodInfo = indexExpression.Type == typeof(string) ? GetValueByKeyMethodInfo : GetValueByIndexMethodInfo;
            Expression[] args = new Expression[] { indexExpression };

            return this.GetMethodMetaObject(methodInfo, args);
        }

        /// <summary>
        /// Implements getter for dynamic indexer by index (JsonArray).
        /// </summary>
        /// <param name="binder">An instance of the <see cref="SetIndexBinder"/> that represents the details of the dynamic operation.</param>
        /// <param name="indexes">An array of <see cref="DynamicMetaObject"/> instances - indexes for the set index operation.</param>
        /// <param name="value">The <see cref="DynamicMetaObject"/> representing the value for the set index operation.</param>
        /// <returns>The new <see cref="DynamicMetaObject"/> representing the result of the binding.</returns>
        public override DynamicMetaObject BindSetIndex(SetIndexBinder binder, DynamicMetaObject[] indexes, DynamicMetaObject value)
        {
            if (binder == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentNullException("binder"));
            }

            if (indexes == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentNullException("indexes"));
            }

            if (value == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentNullException("value"));
            }

            Expression indexExpression;
            if (!JsonValueDynamicMetaObject.TryGetIndexExpression(indexes, out indexExpression))
            { 
                return new DynamicMetaObject(indexExpression, this.DefaultRestrictions);
            }

            MethodInfo methodInfo = indexExpression.Type == typeof(string) ? SetValueByKeyMethodInfo : SetValueByIndexMethodInfo;
            Expression[] args = new Expression[] { indexExpression, Expression.Convert(value.Expression, typeof(object)) };

            return this.GetMethodMetaObject(methodInfo, args);
        }

        /// <summary>
        /// Implements getter for dynamic indexer by key (JsonObject).
        /// </summary>
        /// <param name="binder">An instance of the <see cref="GetMemberBinder"/> that represents the details of the dynamic operation.</param>
        /// <returns>The new <see cref="DynamicMetaObject"/> representing the result of the binding.</returns>
        public override DynamicMetaObject BindGetMember(GetMemberBinder binder)
        {
            if (binder == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentNullException("binder"));
            }

            PropertyInfo propInfo = this.LimitType.GetProperty(binder.Name, BindingFlags.Instance | BindingFlags.Public);

            if (propInfo != null)
            {
                return base.BindGetMember(binder);
            }

            Expression[] args = new Expression[] { Expression.Constant(binder.Name) };

            return this.GetMethodMetaObject(GetValueByKeyMethodInfo, args);
        }

        /// <summary>
        /// Implements setter for dynamic indexer by key (JsonObject).
        /// </summary>
        /// <param name="binder">An instance of the <see cref="SetMemberBinder"/> that represents the details of the dynamic operation.</param>
        /// <param name="value">The <see cref="DynamicMetaObject"/> representing the value for the set member operation.</param>
        /// <returns>The new <see cref="DynamicMetaObject"/> representing the result of the binding.</returns>
        public override DynamicMetaObject BindSetMember(SetMemberBinder binder, DynamicMetaObject value)
        {
            if (binder == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentNullException("binder"));
            }

            if (value == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentNullException("value"));
            }

            Expression[] args = new Expression[] { Expression.Constant(binder.Name), Expression.Convert(value.Expression, typeof(object)) };

            return this.GetMethodMetaObject(SetValueByKeyMethodInfo, args);
        }

        /// <summary>
        /// Performs the binding of the dynamic invoke member operation.
        /// Implemented to support extension methods defined in <see cref="JsonValueExtensions"/> type.
        /// </summary>
        /// <param name="binder">An instance of the InvokeMemberBinder that represents the details of the dynamic operation.</param>
        /// <param name="args">An array of DynamicMetaObject instances - arguments to the invoke member operation.</param>
        /// <returns>The new DynamicMetaObject representing the result of the binding.</returns>
        public override DynamicMetaObject BindInvokeMember(InvokeMemberBinder binder, DynamicMetaObject[] args)
        {
            if (binder == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentNullException("binder"));
            }

            if (args == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentNullException("args"));
            }

            List<Type> argTypeList = new List<Type>();

            for (int idx = 0; idx < args.Length; idx++)
            {
                argTypeList.Add(args[idx].LimitType);
            }

            MethodInfo methodInfo = this.Value.GetType().GetMethod(binder.Name, argTypeList.ToArray());

            if (methodInfo == null)
            {
                argTypeList.Insert(0, typeof(JsonValue));

                Type[] argTypes = argTypeList.ToArray();

                methodInfo = JsonValueDynamicMetaObject.GetExtensionMethod(typeof(JsonValueExtensions), binder.Name, argTypes);

                if (methodInfo != null)
                {
                    Expression thisInstance = Expression.Convert(this.Expression, this.LimitType);
                    Expression[] argsExpression = new Expression[argTypes.Length];

                    argsExpression[0] = thisInstance;
                    for (int i = 0; i < args.Length; i++)
                    {
                        argsExpression[i + 1] = args[i].Expression;
                    }

                    Expression callExpression = Expression.Call(methodInfo, argsExpression);

                    if (methodInfo.ReturnType == typeof(void))
                    {
                        callExpression = Expression.Block(callExpression, Expression.Default(binder.ReturnType));
                    }
                    else
                    {
                        callExpression = Expression.Convert(Expression.Call(methodInfo, argsExpression), binder.ReturnType);
                    }

                    return new DynamicMetaObject(callExpression, this.DefaultRestrictions);
                }
            }

            return base.BindInvokeMember(binder, args);
        }

        /// <summary>
        /// Returns the enumeration of all dynamic member names.
        /// </summary>
        /// <returns>An <see cref="IEnumerable{T}"/> of string reprenseting the dynamic member names.</returns>
        public override IEnumerable<string> GetDynamicMemberNames()
        {
            JsonValue jsonValue = this.Value as JsonValue;

            if (jsonValue != null)
            {
                List<string> names = new List<string>();
                
                foreach (KeyValuePair<string, JsonValue> pair in jsonValue)
                {
                    names.Add(pair.Key);
                }

                return names;
            }

            return base.GetDynamicMemberNames();
        }

        /// <summary>
        /// Gets the operation support value for the specified operation on the specified operand.
        /// </summary>
        /// <param name="operation">The operation type.</param>
        /// <param name="thisValue">The JsonValue instance to check operation for.</param>
        /// <returns>An <see cref="OperationSupport"/> value.</returns>
        private static OperationSupport GetUnaryOperationSupport(ExpressionType operation, JsonValue thisValue)
        {
            //// Unary operators: +, -, !, ~, false (&&), true (||)
            //// unsupported: ++, --

            switch (operation)
            {
                case ExpressionType.UnaryPlus:
                case ExpressionType.Negate:
                case ExpressionType.OnesComplement:
                case ExpressionType.IsFalse:
                case ExpressionType.IsTrue:
                    break;

                case ExpressionType.Not:
                    ////  The DLR converts the 'Not' operation into a 'OnesComplement' operation for integer numbers, need to block that scenario.
                    bool boolVal;
                    if (!thisValue.TryReadAs<bool>(out boolVal))
                    {
                        return OperationSupport.NotSupportedOnOperand;
                    }

                    break;
   
                default:
                    return OperationSupport.NotSupported;
            }

            return OperationSupport.Supported;
        }

        /// <summary>
        /// Gets the operation support value for the specified operation and operands.
        /// </summary>
        /// <param name="operation">The operation type.</param>
        /// <param name="thisValue">The JsonValue instance to check operation for.</param>
        /// <param name="operand">The second operand instance.</param>
        /// <returns>An <see cref="OperationSupport"/> value.</returns>
        private static OperationSupport GetBinaryOperationSupport(ExpressionType operation, JsonValue thisValue, object operand)
        {
            //// Supported binary operators: +, -, *, /, %, &, |, ^, <<, >>, ==, !=, >, <, >=, <=

            bool isCompareOperation = false;

            JsonValue otherValue = operand as JsonValue;

            switch (operation)
            {
                //// supported binary operations

                case ExpressionType.Add:
                case ExpressionType.Subtract:
                case ExpressionType.Multiply:
                case ExpressionType.Divide:
                case ExpressionType.Modulo:
                case ExpressionType.And:
                case ExpressionType.Or:
                case ExpressionType.ExclusiveOr:
                case ExpressionType.LeftShift:
                case ExpressionType.RightShift:
                case ExpressionType.GreaterThan:
                case ExpressionType.GreaterThanOrEqual:
                case ExpressionType.LessThan:
                case ExpressionType.LessThanOrEqual:
                    break;

                //// compare operations:
                case ExpressionType.Equal:
                case ExpressionType.NotEqual:
                    isCompareOperation = true;
                    break;

                default:
                    return OperationSupport.NotSupported;
            }

            if (operand != null)
            {
                bool thisIsPrimitive = thisValue is JsonPrimitive;

                if (otherValue != null)
                {
                    if (!(otherValue is JsonPrimitive) || !thisIsPrimitive)
                    {
                        //// When either value is non-primitive it must be a compare operation.
                        
                        if (!isCompareOperation)
                        {
                            return OperationSupport.NotSupportedOnJsonType;
                        }
                    }
                }
                else
                {
                    //// if operand is not a JsonValue it must be a primitive CLR type and first operand must be a JsonPrimitive.

                    if (!thisIsPrimitive)
                    {
                        return OperationSupport.NotSupportedOnJsonType;
                    }

                    JsonPrimitive primitiveValue = null;

                    if (!JsonPrimitive.TryCreate(operand, out primitiveValue))
                    {
                        return OperationSupport.NotSupportedOnValueType;
                    }
                }
            }
            else
            {
                //// when operand is null only compare operations are valid.

                if (!isCompareOperation)
                {
                    return OperationSupport.NotSupportedOnOperand;
                }
            }
            
            return OperationSupport.Supported;
        }

        /// <summary>
        /// Returns an expression representing a unary operation based on the specified operation type.
        /// </summary>
        /// <param name="operation">The operation type.</param>
        /// <param name="thisExpression">The operand.</param>
        /// <returns>The expression representing the unary operation.</returns>
        private static Expression GetUnaryOperationExpression(ExpressionType operation, Expression thisExpression)
        {
            //// Unary operators: +, -, !, ~, false (&&), true (||)
            //// unsupported: ++, --

            Expression operExpression = null;

            try
            {
                switch (operation)
                {
                    case ExpressionType.UnaryPlus:
                        operExpression = Expression.UnaryPlus(thisExpression);
                        break;
                    case ExpressionType.Negate:
                        operExpression = Expression.Negate(thisExpression);
                        break;
                    case ExpressionType.Not:
                        operExpression = Expression.Not(thisExpression);
                        break;
                    case ExpressionType.OnesComplement:
                        operExpression = Expression.OnesComplement(thisExpression);
                        break;
                    case ExpressionType.IsFalse:
                        operExpression = Expression.IsFalse(thisExpression);
                        break;
                    case ExpressionType.IsTrue:
                        operExpression = Expression.IsTrue(thisExpression);
                        break;
                }
            }
            catch (InvalidOperationException ex)
            {
                operExpression = Expression.Throw(Expression.Constant(ex), typeof(object));
            }

            return operExpression;
        }

        /// <summary>
        /// Updates the <see cref="Expression"/> tree for the operands of the specified operation.
        /// </summary>
        /// <param name="operation">The operation to evalutes.</param>
        /// <param name="thisOperand">The first operand.</param>
        /// <param name="otherOperand">The second operand.</param>
        /// <param name="thisExpression">The <see cref="Expression"/> for the first operand.</param>
        /// <param name="otherExpression">The <see cref="Expression"/> for the second operand.</param>
        private static void GetBinaryOperandExpressions(ExpressionType operation, DynamicMetaObject thisOperand, DynamicMetaObject otherOperand, ref Expression thisExpression, ref Expression otherExpression)
        {
            JsonValue thisValue = thisOperand.Value as JsonValue;
            JsonValue otherValue = otherOperand.Value as JsonValue;

            Type thisType = thisValue.Read().GetType();
            Type otherType = otherValue != null ? otherValue.Read().GetType() : otherOperand.Value.GetType();
            Type coercedType;

            if (JsonValueDynamicMetaObject.TryCoerceType(operation, thisType, otherType, out coercedType))
            {
                thisType = otherType = coercedType;
            }
            else if (JsonValueDynamicMetaObject.TryCoerceSpecialTypes(thisOperand, otherOperand, out coercedType))
            {
                thisType = otherType = coercedType;
            }

            thisExpression = Expression.Convert(thisExpression, thisOperand.LimitType);
            thisExpression = Expression.Convert(Expression.Call(thisExpression, ReadAsMethodInfo, new Expression[] { Expression.Constant(thisType) }), thisType);

            otherExpression = Expression.Convert(otherExpression, otherOperand.LimitType);
            if (otherValue != null)
            {
                otherExpression = Expression.Convert(Expression.Call(otherExpression, ReadAsMethodInfo, new Expression[] { Expression.Constant(otherType) }), otherType);
            }
            else if (otherOperand.LimitType != otherType)
            {
                otherExpression = Expression.Convert(otherExpression, otherType);
            }
        }

        /// <summary>
        /// Returns an Expression representing a binary operation based on the specified operation type.
        /// </summary>
        /// <param name="operation">The operation type.</param>
        /// <param name="thisExpression">An expression representing the left operand.</param>
        /// <param name="otherExpression">An expression representing the right operand.</param>
        /// <returns>The expression representing the binary operation.</returns>
        private static Expression GetBinaryOperationExpression(ExpressionType operation, Expression thisExpression, Expression otherExpression)
        {
            //// Binary operators: +, -, *, /, %, &, |, ^, <<, >>, ==, !=, >, <, >=, <=
            //// The '&&' and '||' operators are conditional versions of the '&' and '|' operators.
            //// Unsupported: Compound assignment operators.

            Expression operExpression = null;

            try
            {
                switch (operation)
                {
                    case ExpressionType.Equal:
                        operExpression = Expression.Equal(thisExpression, otherExpression);
                        break;
                    case ExpressionType.NotEqual:
                        operExpression = Expression.NotEqual(thisExpression, otherExpression);
                        break;
                    case ExpressionType.GreaterThan:
                        operExpression = Expression.GreaterThan(thisExpression, otherExpression);
                        break;
                    case ExpressionType.GreaterThanOrEqual:
                        operExpression = Expression.GreaterThanOrEqual(thisExpression, otherExpression);
                        break;
                    case ExpressionType.LessThan:
                        operExpression = Expression.LessThan(thisExpression, otherExpression);
                        break;
                    case ExpressionType.LessThanOrEqual:
                        operExpression = Expression.LessThanOrEqual(thisExpression, otherExpression);
                        break;
                    case ExpressionType.LeftShift:
                        operExpression = Expression.LeftShift(thisExpression, otherExpression);
                        break;
                    case ExpressionType.RightShift:
                        operExpression = Expression.RightShift(thisExpression, otherExpression);
                        break;
                    case ExpressionType.And:
                        operExpression = Expression.And(thisExpression, otherExpression);
                        break;
                    case ExpressionType.Or:
                        operExpression = Expression.Or(thisExpression, otherExpression);
                        break;
                    case ExpressionType.ExclusiveOr:
                        operExpression = Expression.ExclusiveOr(thisExpression, otherExpression);
                        break;
                    case ExpressionType.Add:
                        operExpression = Expression.Add(thisExpression, otherExpression);
                        break;
                    case ExpressionType.Subtract:
                        operExpression = Expression.Subtract(thisExpression, otherExpression);
                        break;
                    case ExpressionType.Multiply:
                        operExpression = Expression.Multiply(thisExpression, otherExpression);
                        break;
                    case ExpressionType.Divide:
                        operExpression = Expression.Divide(thisExpression, otherExpression);
                        break;
                    case ExpressionType.Modulo:
                        operExpression = Expression.Modulo(thisExpression, otherExpression);
                        break;
                }
            }
            catch (InvalidOperationException ex)
            { 
                operExpression = Expression.Throw(Expression.Constant(ex), typeof(object));
            }

            return operExpression;
        }

        /// <summary>
        /// Returns an expression representing a 'throw' instruction based on the specified <see cref="OperationSupport"/> value.
        /// </summary>
        /// <param name="supportValue">The <see cref="OperationSupport"/> value.</param>
        /// <param name="operation">The operation type.</param>
        /// <param name="thisValue">The operation left operand.</param>
        /// <param name="operand">The operation right operand.</param>
        /// <returns>A <see cref="Expression"/> representing a 'throw' instruction.</returns>
        private static Expression GetOperationErrorExpression(OperationSupport supportValue, ExpressionType operation, JsonValue thisValue, object operand)
        {
            string exceptionMessage;
            string operandTypeName = operand != null ? operand.GetType().FullName : "<null>";

            switch (supportValue)
            {
                default:
                case OperationSupport.NotSupported:
                case OperationSupport.NotSupportedOnJsonType:
                case OperationSupport.NotSupportedOnValueType:
                    exceptionMessage = SG.GetString(SR.OperatorNotDefinedForJsonType, operation, thisValue.JsonType);
                    break;

                case OperationSupport.NotSupportedOnOperand:
                    exceptionMessage = SG.GetString(SR.OperatorNotAllowedOnOperands, operation, thisValue.GetType().FullName, operandTypeName);
                    break;
            }

            return Expression.Throw(Expression.Constant(new InvalidOperationException(exceptionMessage)), typeof(object));
        }

        /// <summary>
        /// Gets a <see cref="MethodInfo"/> instance for the specified method name in the specified type.
        /// </summary>
        /// <param name="extensionProviderType">The extension provider type.</param>
        /// <param name="methodName">The name of the method to get the info for.</param>
        /// <param name="argTypes">The types of the method arguments.</param>
        /// <returns>A <see cref="MethodInfo"/>instance or null if the method cannot be resolved.</returns>
        private static MethodInfo GetExtensionMethod(Type extensionProviderType, string methodName, Type[] argTypes)
        {
            MethodInfo methodInfo = null;
            MethodInfo[] methods = extensionProviderType.GetMethods();

            foreach (MethodInfo info in methods)
            {
                if (info.Name == methodName)
                {
                    methodInfo = info;

                    if (!info.IsGenericMethodDefinition)
                    {
                        bool paramsMatch = true;
                        ParameterInfo[] args = methodInfo.GetParameters();

                        if (args.Length == argTypes.Length)
                        {
                            for (int idx = 0; idx < args.Length; idx++)
                            {
                                if (!args[idx].ParameterType.IsAssignableFrom(argTypes[idx]))
                                {
                                    paramsMatch = false;
                                    break;
                                }
                            }

                            if (paramsMatch)
                            {
                                break;
                            }
                        }
                    }
                }
            }

            return methodInfo;
        }

        /// <summary>
        /// Attempts to get an expression for an index parameter.
        /// </summary>
        /// <param name="indexes">The operation indexes parameter.</param>
        /// <param name="expression">A <see cref="Expression"/> to be initialized to the index expression if the operation is successful, otherwise an error expression.</param>
        /// <returns>true the operation is successful, false otherwise.</returns>
        private static bool TryGetIndexExpression(DynamicMetaObject[] indexes, out Expression expression)
        {
            if (indexes.Length == 1 && indexes[0] != null && indexes[0].Value != null)
            {
                DynamicMetaObject index = indexes[0];
                Type indexType = indexes[0].Value.GetType();

                switch (Type.GetTypeCode(indexType))
                {
                    case TypeCode.Char:
                    case TypeCode.Int16:
                    case TypeCode.UInt16:
                    case TypeCode.Byte:
                    case TypeCode.SByte:
                        Expression argExp = Expression.Convert(index.Expression, typeof(object));
                        Expression typeExp = Expression.Constant(typeof(int));
                        expression = Expression.Convert(Expression.Call(ChangeTypeMethodInfo, new Expression[] { argExp, typeExp }), typeof(int));
                        return true;

                    case TypeCode.Int32:
                    case TypeCode.String:
                        expression = index.Expression;
                        return true;
                }

                expression = Expression.Throw(Expression.Constant(new ArgumentException(SG.GetString(SR.InvalidIndexType, indexType))), typeof(object));
                return false;
            }

            expression = Expression.Throw(Expression.Constant(new ArgumentException(SG.GetString(SR.NonSingleNonNullIndexNotSupported))), typeof(object));
            return false;
        }

        /// <summary>
        /// Attempts to coerce the operand types on a binary operation for some special type and value cases as treated by JsonValue:
        /// "true" and "false" can be converted to boolean.
        /// Guid, DateTime and other types can be converted to string.
        /// </summary>
        /// <param name="thisOperand">The first operand.</param>
        /// <param name="otherOperand">The second operand</param>
        /// <param name="coercedType">On success, this parameter contains the coerced type.</param>
        /// <returns>true if the coercion is performed, false otherwise.</returns>
        private static bool TryCoerceSpecialTypes(DynamicMetaObject thisOperand, DynamicMetaObject otherOperand, out Type coercedType)
        {
            JsonValue thisValue = thisOperand.Value as JsonValue;
            JsonValue otherValue = otherOperand.Value as JsonValue;

            if (thisValue is JsonPrimitive)
            {
                Type thisType = thisValue.Read().GetType();

                if (thisType != otherOperand.LimitType)
                {
                    if (otherOperand.LimitType == typeof(string) || (thisType == typeof(string) && otherValue == null))
                    {
                        object value;
                        if (thisValue.TryReadAs(otherOperand.LimitType, out value))
                        {
                            coercedType = otherOperand.LimitType;
                            return true;
                        }
                    }
                }
            }

            coercedType = default(Type);
            return false;
        }

        /// <summary>
        /// Attempts to coerce one of the specified types to the other if needed and if possible.
        /// </summary>
        /// <param name="operation">The operation for the type coercion.</param>
        /// <param name="thisType">The type of the first operand.</param>
        /// <param name="otherType">The type of the second operand.</param>
        /// <param name="coercedType">The coerced type.</param>
        /// <returns>true if the type is coerced, false otherwise.</returns>
        private static bool TryCoerceType(ExpressionType operation, Type thisType, Type otherType, out Type coercedType)
        {
            //// Supported coercion operators: +, -, *, /, %, ==, !=, >, <, >=, <=

            if (thisType != otherType)
            {
                switch (operation)
                {
                    case ExpressionType.Add:
                    case ExpressionType.Subtract:
                    case ExpressionType.Multiply:
                    case ExpressionType.Divide:
                    case ExpressionType.Modulo:
                    case ExpressionType.Equal:
                    case ExpressionType.NotEqual:
                    case ExpressionType.GreaterThan:
                    case ExpressionType.GreaterThanOrEqual:
                    case ExpressionType.LessThan:
                    case ExpressionType.LessThanOrEqual:

                        if (ImplicitNumericTypeConverter.TryGetCoercionType(thisType, otherType, out coercedType))
                        {
                            return true;
                        }

                        break;
                }
            }
            
            coercedType = default(Type);
            return false;
        }

        /// <summary>
        /// Gets the return type for unary operations.
        /// </summary>
        /// <param name="binder">The unary operation binder.</param>
        /// <returns>The type representing the operation return type.</returns>
        private Type GetUnaryOperationReturnType(UnaryOperationBinder binder)
        {
            JsonValue thisValue = this.Value as JsonValue;

            Type returnType = binder.ReturnType == typeof(object) ? thisValue.Read().GetType() : binder.ReturnType;

            //// The DLR sets the binder.ReturnType for the unary 'Not' operation as 'object' as opposed to 'bool', 
            //// we need to detect this case and fix up the type to enable boolean conversions from strings.

            if (returnType == typeof(string) && binder.Operation == ExpressionType.Not)
            {
                bool boolVal;
                if (thisValue.TryReadAs<bool>(out boolVal))
                {
                    returnType = typeof(bool);
                }
            }

            return returnType;
        }

        /// <summary>
        /// Gets a <see cref="DynamicMetaObject"/> for a method call.
        /// </summary>
        /// <param name="methodInfo">Info for the method to be performed.</param>
        /// <param name="args">expression array representing the method arguments</param>
        /// <returns>A meta object for the method call.</returns>
        private DynamicMetaObject GetMethodMetaObject(MethodInfo methodInfo, Expression[] args)
        {
            Expression instance = Expression.Convert(this.Expression, this.LimitType);
            Expression methodCall = Expression.Call(instance, methodInfo, args);
            BindingRestrictions restrictions = this.DefaultRestrictions;

            DynamicMetaObject metaObj = new DynamicMetaObject(methodCall, restrictions);

            return metaObj;
        }

        /// <summary>
        /// Helper class for numeric type coercion support.
        /// </summary>
        private static class ImplicitNumericTypeConverter
        {
            /// <summary>
            /// Table of implicit conversion types, the 'values' in the dictionary represent the type values the 'key' type
            /// can be converted to.
            /// </summary>
            /// <remarks>
            /// Observe that this table is not the full conversion table, for storage optimization some types have been factored
            /// out into a list; the algorithm that uses this table knows about this optimization.
            /// For reference see Implicit Conversion table in the MSDN http://msdn.microsoft.com/en-us/library/y5b434w4.aspx
            ///     sbyte   -> short, int, long, float, double, or decimal
            ///     byte    -> short, ushort, int, uint, long, ulong, float, double, or decimal
            ///     short   -> int, long, float, double, or decimal
            ///     ushort  -> int, uint, long, ulong, float, double, or decimal
            ///     int     -> long, float, double, or decimal
            ///     uint    -> long, ulong, float, double, or decimal
            ///     long    -> float, double, or decimal
            ///     char    -> ushort, int, uint, long, ulong, float, double, or decimal
            ///     float   -> double
            ///     ulong   -> float, double, or decimal
            /// </remarks>
            private static Dictionary<string, List<string>> partialConversionTable = new Dictionary<string, List<string>>
            {
                { typeof(sbyte).Name, new List<string>() { typeof(short).Name,  typeof(int).Name } },
                { typeof(byte).Name, new List<string>() { typeof(short).Name,  typeof(ushort).Name,  typeof(int).Name,  typeof(uint).Name,  typeof(ulong).Name } },
                { typeof(short).Name, new List<string>() { typeof(int).Name } },
                { typeof(ushort).Name, new List<string>() { typeof(int).Name,  typeof(uint).Name,  typeof(ulong).Name } },
                { typeof(int).Name,  new List<string>() { } },
                { typeof(uint).Name,  new List<string>() { typeof(ulong).Name } },
                { typeof(long).Name,  new List<string>() { } },
                { typeof(char).Name,  new List<string>() { typeof(ushort).Name,  typeof(int).Name,  typeof(uint).Name,  typeof(ulong).Name } },
                { typeof(ulong).Name,  new List<string>() { } },
                ////{ typeof(float).Name,  new List<string>() { typeof(double).Name }},
            };

            /// <summary>
            /// List of types most other types can be coerced to.
            /// </summary>
            private static List<string> universalCoercionTypes = new List<string> { typeof(long).Name,  typeof(float).Name, typeof(double).Name, typeof(decimal).Name };

            /// <summary>
            /// Attempts to coerce one type to another.
            /// </summary>
            /// <param name="thisType">The first type to be evaluated.</param>
            /// <param name="otherType">The second type to be evaluated.</param>
            /// <param name="coercedType">The coerced resulting type to be used.</param>
            /// <returns>true if the coercion exists, false otherwise.</returns>
            public static bool TryGetCoercionType(Type thisType, Type otherType, out Type coercedType)
            {
                //// checks covering for storage optimizations in the implicit numeric converstion table

                Type typeofLong = typeof(long);
                Type typeofULong = typeof(ulong);

                // special-case ulong type since it cannot be coerced to long which is part of the universal coercion list.
                if ((thisType == typeofULong && otherType == typeofLong) || (thisType == typeofLong && otherType == typeofULong))
                {
                    coercedType = default(Type);
                    return false;
                }

                Type typeofFloat = typeof(float);
                Type typeofDouble = typeof(double);
                
                // special-case float since it can be coerced to double only.
                if ((thisType == typeofFloat && otherType == typeofDouble) || (otherType == typeofFloat && thisType == typeofDouble))
                {
                    coercedType = typeofDouble;
                    return true;
                }

                if (partialConversionTable.ContainsKey(thisType.Name) && 
                    (partialConversionTable[thisType.Name].Contains(otherType.Name) || universalCoercionTypes.Contains(otherType.Name)))
                {
                    coercedType = otherType;
                    return true;
                }

                if (partialConversionTable.ContainsKey(otherType.Name) && 
                    (partialConversionTable[otherType.Name].Contains(thisType.Name) || universalCoercionTypes.Contains(thisType.Name)))
                {
                    coercedType = thisType;
                    return true;
                }

                coercedType = default(Type);
                return false;
            }
        }
    }
}
