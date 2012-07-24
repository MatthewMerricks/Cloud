// <copyright>
// Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Http.CIT.Scenario.Common
{
    using System.ServiceModel;
    using System.ServiceModel.Channels;

    [ServiceContract]
    public interface ITestServiceContract
    {
        [OperationContract(Action = "*", ReplyAction = "*")]
        Message HandleMessage(Message request);
    }
}
