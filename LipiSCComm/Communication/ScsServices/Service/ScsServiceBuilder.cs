﻿using Lipi.Communication.Scs.Communication.EndPoints;
using Lipi.Communication.Scs.Server;

namespace Lipi.Communication.ScsServices.Service
{
    /// <summary>
    /// This class is used to build ScsService applications.
    /// </summary>
    public static class ScsServiceBuilder
    {
        /// <summary>
        /// Creates a new SCS Service application using an EndPoint.
        /// </summary>
        /// <param name="endPoint">EndPoint that represents address of the service</param>
        /// <returns>Created SCS service application</returns>
        public static IScsServiceApplication CreateService(ScsEndPoint endPoint)
        {
            return new ScsServiceApplication(ScsServerFactory.CreateServer(endPoint));
        }
    }
}
