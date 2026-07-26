namespace ryzen_smu_cli;

internal readonly record struct OperationResult(bool Success, string? Error)
{
    public static OperationResult Ok() => new(true, null);

    public static OperationResult Fail(string error) => new(false, error);
}

internal readonly record struct OperationResult<T>(bool Success, T? Value, string? Error)
{
    public static OperationResult<T> Ok(T value) => new(true, value, null);

    public static OperationResult<T> Fail(string error) => new(false, default, error);
}

internal sealed record DowncoreOperationResult(
    bool Success,
    IReadOnlyList<byte> DisableMasks,
    string? Error)
{
    public static DowncoreOperationResult Ok(IReadOnlyList<byte> disableMasks) =>
        new(true, disableMasks, null);

    public static DowncoreOperationResult Fail(string error) =>
        new(false, [], error);
}
