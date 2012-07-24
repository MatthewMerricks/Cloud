// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Common.Test.Services
{
    using System;
    using System.Collections.Generic;
    using System.ServiceModel;
    using System.ServiceModel.Dispatcher;
    using System.ServiceModel.Web;

    [ServiceContract]
    public class WebGetService
    {
        // 0 Input, 0 Output
        [WebGet]
        public void Operation1()
        {
            throw new NotImplementedException();
        }

        // 0 Input, 1 Output
        [WebGet]
        public int Operation2()
        {
            throw new NotImplementedException();
        }

        [WebGet]
        public void Operation3(out int out1)
        {
            throw new NotImplementedException();
        }

        // 1 Input, 0 Output
        [WebGet]
        public void Operation4(string in1)
        {
            throw new NotImplementedException();
        }

        // 1 Input, 1 Output
        [WebGet]
        public int Operation5(string in1)
        {
            throw new NotImplementedException();
        }

        [WebGet]
        public void Operation6(string in1, out int out1)
        {
            throw new NotImplementedException();
        }

        // 0 Input, 2 Output
        [WebGet]
        public int Operation7(out int out1)
        {
            throw new NotImplementedException();
        }

        [WebGet]
        public void Operation8(out int out1, out int out2)
        {
            throw new NotImplementedException();
        }

        // 2 Input, 0 Output
        [WebGet]
        public void Operation9(string in1, string in2)
        {
            throw new NotImplementedException();
        }

        // 1 Input, 2 Output
        [WebGet]
        public int Operation10(string in1, out int out1)
        {
            throw new NotImplementedException();
        }

        [WebGet]
        public void Operation11(string in1, out int out1, out int out2)
        {
            throw new NotImplementedException();
        }

        // 2 Input, 1 Output
        [WebGet]
        public void Operation12(string in1, string in2, out int out1)
        {
            throw new NotImplementedException();
        }

        [WebGet]
        public int Operation13(string in1, string in2)
        {
            throw new NotImplementedException();
        }
    }
}
