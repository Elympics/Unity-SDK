namespace GameLogic.NewTicTacToe
{
	public readonly struct Input
	{
		public readonly int FieldIndex;

		public Input(int fieldIndex)
		{
			FieldIndex = fieldIndex;
		}

		public static readonly Input Empty = new Input(-1);
	}
}
