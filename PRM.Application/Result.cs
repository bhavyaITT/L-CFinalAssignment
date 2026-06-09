using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PRM.Application
{
    /// <summary>
    /// Wraps every use case response.
    /// Instead of throwing exceptions for business failures (e.g. "user already exists"),
    /// we return a Result with IsSuccess=false and an Error message.
    /// This is the Fail Fast principle — callers check the result before proceeding.
    /// </summary>
    public class Result<T>
    {
        public bool IsSuccess { get; private set; }
        public T? Data { get; private set; }
        public string? Error { get; private set; }

        private Result() { }

        public static Result<T> Success(T data) => new() { IsSuccess = true, Data = data };
        public static Result<T> Failure(string error) => new() { IsSuccess = false, Error = error };
    }

    public class Result
    {
        public bool IsSuccess { get; private set; }
        public string? Error { get; private set; }

        private Result() { }

        public static Result Success() => new() { IsSuccess = true };
        public static Result Failure(string error) => new() { IsSuccess = false, Error = error };
    }
}
