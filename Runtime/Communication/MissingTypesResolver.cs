using System;
using System.Collections.Generic;
using MessagePack;
using MessagePack.Formatters;

#nullable enable

namespace Elympics.Resolvers
{
    public sealed class MissingTypesResolver : IFormatterResolver
    {
        public static readonly MissingTypesResolver Instance = new();

        private MissingTypesResolver()
        { }

        public IMessagePackFormatter<T>? GetFormatter<T>() => FormatterCache<T>.Formatter;

        private static class FormatterCache<T>
        {
            public static readonly IMessagePackFormatter<T>? Formatter;

            static FormatterCache() =>
                Formatter = (IMessagePackFormatter<T>?)MissingTypesResolverGetFormatterHelper.GetFormatter(typeof(T));
        }
    }

    internal static class MissingTypesResolverGetFormatterHelper
    {
        private static readonly Dictionary<Type, object> FormatterMap = new()
        {
            { typeof(decimal[]), new ArrayFormatter<decimal>() },
        };

        internal static object? GetFormatter(Type t)
        {
            if (FormatterMap.TryGetValue(t, out var formatter))
                return formatter;

            if (typeof(Type).IsAssignableFrom(t))
                return typeof(TypeFormatter<>).MakeGenericType(t).GetField(nameof(TypeFormatter<Type>.Instance))!.GetValue(null);

            return null;
        }
    }
}
