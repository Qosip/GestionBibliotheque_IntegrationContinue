namespace Library.Application.Results
{
    public sealed class ReceiveTransferResult
    {
        public bool Success { get; }
        public string? ErrorCode { get; }

        private ReceiveTransferResult(bool success, string? errorCode)
        {
            Success = success;
            ErrorCode = errorCode;
        }

        public static ReceiveTransferResult Ok() =>
            new ReceiveTransferResult(true, null);

        public static ReceiveTransferResult Fail(string code) =>
            new ReceiveTransferResult(false, code);
    }
}
