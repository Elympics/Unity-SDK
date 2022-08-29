using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine;

namespace MatchEvents
{
	public static class Serializer
	{
		private static void Print(string s) => Debug.Log(s);

		public static void PrintCall([CallerMemberName] string caller = "") => Print($"{caller} called!");

		public static void PrintCall(Dictionary<string, object> parameters, [CallerMemberName] string caller = "")
		{
			var serializedParameters = BuildString('{', '}', parameters,
				parameter => $" {parameter.Key}: {Serialize(parameter.Value)},");
			Print($"{caller} called with: {serializedParameters}");
		}

		private static string Serialize(object obj)
		{
			if (obj is null)
				return "null";

			var type = obj.GetType();

			if (type.IsPrimitive || obj is IFormattable)
				return $"{obj}";
			if (obj is string s)
				return $"@\"{s.Replace("\"", "\"\"")}\"";
			if (obj is byte[] byteArray)
				return $"\"{Convert.ToBase64String(byteArray)}\"";
			if (obj is IEnumerable enumerable)
				return BuildString('[', ']', enumerable.Cast<object>(),
					element => $" {Serialize(element)},");

			var objSb = new SerializationBuilder('{', '}');
			objSb.AppendElements(type.GetProperties(BindingFlags.Instance | BindingFlags.Public),
				property => $" {property.Name}: {Serialize(property.GetValue(obj))},");
			objSb.AppendElements(type.GetFields(BindingFlags.Instance | BindingFlags.Public),
				property => $" {property.Name}: {Serialize(property.GetValue(obj))},");
			objSb.Close();
			return objSb.ToString();
		}

		private static string BuildString<T>(char opening, char closing, IEnumerable<T> elements, Func<T, string> elementSerializer)
		{
			var sb = new SerializationBuilder(opening, closing);
			sb.AppendElements(elements, elementSerializer);
			sb.Close();
			return sb.ToString();
		}

		private class SerializationBuilder
		{
			private readonly StringBuilder _sb;
			private readonly char _closingChar;

			private bool _closed;

			public SerializationBuilder(char openingChar, char closingChar) =>
				(_sb, _closingChar) = (new StringBuilder($"{openingChar}"), closingChar);

			public void AppendElements<T>(IEnumerable<T> elements, Func<T, string> elementSerializer)
			{
				if (_closed)
					return;
				foreach (var property in elements)
					_sb.Append(elementSerializer(property));
			}

			public void Close()
			{
				if (_sb[_sb.Length - 1] == ',')
					_sb.Remove(_sb.Length - 1, 1);
				_sb.Append($" {_closingChar}");
				_closed = true;
			}

			public override string ToString() => _sb.ToString();
		}
	}
}
