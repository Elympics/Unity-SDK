namespace Elympics
{
	public abstract class Result<TValue, TError>
	{
		public abstract bool IsSuccess { get; }
		public bool IsFailure => !IsSuccess;

		public abstract TValue Value { get; }
		public abstract TError Error { get; }

		public static Result<TValue, TError> Success(TValue value) => new ResultSuccess(value);
		public static Result<TValue, TError> Failure(TError error) => new ResultFailure(error);

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
