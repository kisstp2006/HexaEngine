﻿namespace HexaEngine.D3D11
{
    using System;
    using System.Runtime.InteropServices;

    public class D3D11Exception : Exception
    {
        public D3D11Exception(ResultCode code) : base((Marshal.GetExceptionForHR((int)code) ?? new Exception("Unable to get exception from HRESULT")).Message)
        {
        }
    }

    public static class D3D11ExceptionExtensions
    {
        public static string GetMessage(this HResult result)
        {
            return (Marshal.GetExceptionForHR(result.Code) ?? new Exception("Unable to get exception from HRESULT")).Message;
        }
    }
}