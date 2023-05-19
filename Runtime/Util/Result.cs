namespace Elympics
{
	// TODO: internalize, probably rethink the implementation ~dsygocki, 2023-04-28
	public abstract class Result<TValue, TError>
	{
		public abstract bool IsSuccess { get; }
		public bool IsFailure => !IsSuccess;

		public abstract TValue Value { get; }
		public abstract TError Error { get; }

		public static Result<TValue, TError> Success(TValue value) => new ResultSuccess(value);
		public static Result<TValue, TError> Failure(TError error) => new ResultFailure(error);

		public static Result<TValue, TError> Generalize<TValueDerived, TErrorDerived>(Result<TValueDerived, TErrorDerived> result)
			where TValueDerived : TValue
			where TErrorDerived : TError
		{
			return result.IsSuccess ? Success(result.Value) : Failure(result.Error);
		}

		private class ResultSuccess : Result<TValue, TError>
		{
			public override bool IsSuccess => true;
			public override TValue Value { get; }
			public override TError Error => default;

			public ResultSuccess(TValue value) => Value = value;
		}

		private class ResultFailure : Result<TValue, TError>
		{
			public override bool IsSuccess => false;
			public override TValue Value => default;
			public override TError Error { get; }

			public ResultFailure(TError error) => Error = error;
		}
	}
}
