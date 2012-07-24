﻿// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Common.Test
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class ValueTypeTestData<T> : TestData<T> where T : struct
    {
        private static readonly Type OpenNullableType = typeof(Nullable<>);
        private T[] testData;

        public ValueTypeTestData(params T[] testData)
            : base()
        {
            this.testData = testData;

            Type[] typeParams = new Type[] { this.Type };
            this.RegisterTestDataVariation(TestDataVariations.AsNullable, OpenNullableType.MakeGenericType(typeParams), GetTestDataAsNullable);
        }

        public IEnumerable<Nullable<T>> GetTestDataAsNullable()
        {
            return this.GetTypedTestData().Select(d => new Nullable<T>(d));
        }

        protected override IEnumerable<T> GetTypedTestData()
        {
            return this.testData;
        }
    }
}
