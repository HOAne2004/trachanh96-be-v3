namespace Shared.Application.Models;

public class Result
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public string Error { get; }

    protected Result(bool isSuccess, string error)
    {
        // Rào chắn bảo vệ logic: Đã Success thì không được có Error, và ngược lại
        if (isSuccess && !string.IsNullOrEmpty(error))
            throw new InvalidOperationException("Một Result thành công không thể chứa thông báo lỗi.");
        if (!isSuccess && string.IsNullOrEmpty(error))
            throw new InvalidOperationException("Một Result thất bại bắt buộc phải có thông báo lỗi.");

        IsSuccess = isSuccess;
        Error = error;
    }

    public static Result Success() => new(true, string.Empty);
    public static Result Failure(string error) => new(false, error);
}

// Class generic cho các Command/Query CÓ trả về dữ liệu (như trả về Id, List...)
public class Result<T> : Result
{
    private readonly T? _value;

    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Không thể lấy dữ liệu (Value) từ một Result thất bại.");

    protected internal Result(T? value, bool isSuccess, string error)
        : base(isSuccess, error)
    {
        _value = value;
    }

    public static Result<T> Success(T value) => new(value, true, string.Empty);
    public new static Result<T> Failure(string error) => new(default, false, error);
}