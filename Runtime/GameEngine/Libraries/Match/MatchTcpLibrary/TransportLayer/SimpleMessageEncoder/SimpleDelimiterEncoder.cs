using System.Collections.Generic;
using MatchTcpLibrary.TransportLayer.Interfaces;

namespace MatchTcpLibrary.TransportLayer.SimpleMessageEncoder
{
	public class SimpleDelimiterEncoder : IMessageEncoder
	{
		private readonly SimpleMessageEncoderConfig _config;

		public SimpleDelimiterEncoder(SimpleMessageEncoderConfig config)
		{
			_config = config;
		}

		public List<List<byte>> ExtractCompleteMessages(List<byte> payloadQueue)
		{
			return SplitToDelimitedParts(payloadQueue);
		}

		public byte[] EncodePayload(byte[] payload)
		{
			var lstByte = new List<byte>();
			lstByte.AddRange(payload);

			// Append delimiter
			for (var i = 0; i < _config.DelimiterRepeated; i++)
				lstByte.Add(_config.Delimiter);

			var arrByte = lstByte.ToArray();
			return arrByte;
		}

		private List<List<byte>> SplitToDelimitedParts(List<byte> data)
		{
			var lstOuter   = new List<List<byte>>();
			var lstInner   = new List<byte>();
			var delimCount = 0;

			foreach (byte bt in data)
			{
				if (bt == _config.Delimiter)
				{
					if (++delimCount != _config.DelimiterRepeated)
						continue;

					if (lstInner.Count > 0)
					{
						lstOuter.Add(new List<byte>(lstInner));
						lstInner.Clear();
					}

					delimCount = 0;
				}
				else
				{
					AddDelimsToInnerList(lstInner, ref delimCount);
					lstInner.Add(bt);
				}
			}

			AddDelimsToInnerList(lstInner, ref delimCount);

			data.Clear();
			if (lstInner.Count > 0)
				data.AddRange(lstInner);

			return lstOuter;
		}

		private void AddDelimsToInnerList(List<byte> lstInner, ref int delimCount)
		{
			if (delimCount <= 0) return;

			for (var i = 0; i < delimCount; i++)
				lstInner.Add(_config.Delimiter);

			delimCount = 0;
		}
	}
}
