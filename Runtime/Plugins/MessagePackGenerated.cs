// <auto-generated>
// THIS (.cs) FILE IS GENERATED BY MPC(MessagePack-CSharp). DO NOT CHANGE IT.
// </auto-generated>

#pragma warning disable 618
#pragma warning disable 612
#pragma warning disable 414
#pragma warning disable 168
#pragma warning disable CS1591 // document public APIs

#pragma warning disable SA1312 // Variable names should begin with lower-case letter
#pragma warning disable SA1649 // File name should match first type name

namespace MessagePack.Resolvers
{
    public class GeneratedResolver : global::MessagePack.IFormatterResolver
    {
        public static readonly global::MessagePack.IFormatterResolver Instance = new GeneratedResolver();

        private GeneratedResolver()
        {
        }

        public global::MessagePack.Formatters.IMessagePackFormatter<T> GetFormatter<T>()
        {
            return FormatterCache<T>.Formatter;
        }

        private static class FormatterCache<T>
        {
            internal static readonly global::MessagePack.Formatters.IMessagePackFormatter<T> Formatter;

            static FormatterCache()
            {
                var f = GeneratedResolverGetFormatterHelper.GetFormatter(typeof(T));
                if (f != null)
                {
                    Formatter = (global::MessagePack.Formatters.IMessagePackFormatter<T>)f;
                }
            }
        }
    }

    internal static class GeneratedResolverGetFormatterHelper
    {
        private static readonly global::System.Collections.Generic.Dictionary<global::System.Type, int> lookup;

        static GeneratedResolverGetFormatterHelper()
        {
            lookup = new global::System.Collections.Generic.Dictionary<global::System.Type, int>(12)
            {
                { typeof(global::System.Guid[]), 0 },
                { typeof(global::Elympics.Models.Matchmaking.WebSocket.ErrorBlame), 1 },
                { typeof(global::Elympics.Models.Matchmaking.WebSocket.MatchmakerStatusCodes), 2 },
                { typeof(global::Elympics.Models.Matchmaking.WebSocket.IFromLobby), 3 },
                { typeof(global::Elympics.Models.Matchmaking.WebSocket.IToLobby), 4 },
                { typeof(global::Elympics.Models.Matchmaking.WebSocket.GameData), 5 },
                { typeof(global::Elympics.Models.Matchmaking.WebSocket.JoinMatchmaker), 6 },
                { typeof(global::Elympics.Models.Matchmaking.WebSocket.MatchData), 7 },
                { typeof(global::Elympics.Models.Matchmaking.WebSocket.MatchFound), 8 },
                { typeof(global::Elympics.Models.Matchmaking.WebSocket.MatchmakingError), 9 },
                { typeof(global::Elympics.Models.Matchmaking.WebSocket.Ping), 10 },
                { typeof(global::Elympics.Models.Matchmaking.WebSocket.Pong), 11 },
            };
        }

        internal static object GetFormatter(global::System.Type t)
        {
            int key;
            if (!lookup.TryGetValue(t, out key))
            {
                return null;
            }

            switch (key)
            {
                case 0: return new global::MessagePack.Formatters.ArrayFormatter<global::System.Guid>();
                case 1: return new MessagePack.Formatters.Elympics.Models.Matchmaking.WebSocket.ErrorBlameFormatter();
                case 2: return new MessagePack.Formatters.Elympics.Models.Matchmaking.WebSocket.MatchmakerStatusCodesFormatter();
                case 3: return new MessagePack.Formatters.Elympics.Models.Matchmaking.WebSocket.IFromLobbyFormatter();
                case 4: return new MessagePack.Formatters.Elympics.Models.Matchmaking.WebSocket.IToLobbyFormatter();
                case 5: return new MessagePack.Formatters.Elympics.Models.Matchmaking.WebSocket.GameDataFormatter();
                case 6: return new MessagePack.Formatters.Elympics.Models.Matchmaking.WebSocket.JoinMatchmakerFormatter();
                case 7: return new MessagePack.Formatters.Elympics.Models.Matchmaking.WebSocket.MatchDataFormatter();
                case 8: return new MessagePack.Formatters.Elympics.Models.Matchmaking.WebSocket.MatchFoundFormatter();
                case 9: return new MessagePack.Formatters.Elympics.Models.Matchmaking.WebSocket.MatchmakingErrorFormatter();
                case 10: return new MessagePack.Formatters.Elympics.Models.Matchmaking.WebSocket.PingFormatter();
                case 11: return new MessagePack.Formatters.Elympics.Models.Matchmaking.WebSocket.PongFormatter();
                default: return null;
            }
        }
    }
}

#pragma warning restore 168
#pragma warning restore 414
#pragma warning restore 618
#pragma warning restore 612

#pragma warning restore SA1312 // Variable names should begin with lower-case letter
#pragma warning restore SA1649 // File name should match first type name


// <auto-generated>
// THIS (.cs) FILE IS GENERATED BY MPC(MessagePack-CSharp). DO NOT CHANGE IT.
// </auto-generated>

#pragma warning disable 618
#pragma warning disable 612
#pragma warning disable 414
#pragma warning disable 168
#pragma warning disable CS1591 // document public APIs

#pragma warning disable SA1403 // File may only contain a single namespace
#pragma warning disable SA1649 // File name should match first type name

namespace MessagePack.Formatters.Elympics.Models.Matchmaking.WebSocket
{

    public sealed class ErrorBlameFormatter : global::MessagePack.Formatters.IMessagePackFormatter<global::Elympics.Models.Matchmaking.WebSocket.ErrorBlame>
    {
        public void Serialize(ref global::MessagePack.MessagePackWriter writer, global::Elympics.Models.Matchmaking.WebSocket.ErrorBlame value, global::MessagePack.MessagePackSerializerOptions options)
        {
            writer.Write((global::System.Int32)value);
        }

        public global::Elympics.Models.Matchmaking.WebSocket.ErrorBlame Deserialize(ref global::MessagePack.MessagePackReader reader, global::MessagePack.MessagePackSerializerOptions options)
        {
            return (global::Elympics.Models.Matchmaking.WebSocket.ErrorBlame)reader.ReadInt32();
        }
    }

    public sealed class MatchmakerStatusCodesFormatter : global::MessagePack.Formatters.IMessagePackFormatter<global::Elympics.Models.Matchmaking.WebSocket.MatchmakerStatusCodes>
    {
        public void Serialize(ref global::MessagePack.MessagePackWriter writer, global::Elympics.Models.Matchmaking.WebSocket.MatchmakerStatusCodes value, global::MessagePack.MessagePackSerializerOptions options)
        {
            writer.Write((global::System.Int32)value);
        }

        public global::Elympics.Models.Matchmaking.WebSocket.MatchmakerStatusCodes Deserialize(ref global::MessagePack.MessagePackReader reader, global::MessagePack.MessagePackSerializerOptions options)
        {
            return (global::Elympics.Models.Matchmaking.WebSocket.MatchmakerStatusCodes)reader.ReadInt32();
        }
    }
}

#pragma warning restore 168
#pragma warning restore 414
#pragma warning restore 618
#pragma warning restore 612

#pragma warning restore SA1403 // File may only contain a single namespace
#pragma warning restore SA1649 // File name should match first type name


// <auto-generated>
// THIS (.cs) FILE IS GENERATED BY MPC(MessagePack-CSharp). DO NOT CHANGE IT.
// </auto-generated>

#pragma warning disable 618
#pragma warning disable 612
#pragma warning disable 414
#pragma warning disable 168
#pragma warning disable CS1591 // document public APIs

#pragma warning disable SA1403 // File may only contain a single namespace
#pragma warning disable SA1649 // File name should match first type name

namespace MessagePack.Formatters.Elympics.Models.Matchmaking.WebSocket
{
    public sealed class IFromLobbyFormatter : global::MessagePack.Formatters.IMessagePackFormatter<global::Elympics.Models.Matchmaking.WebSocket.IFromLobby>
    {
        private readonly global::System.Collections.Generic.Dictionary<global::System.RuntimeTypeHandle, global::System.Collections.Generic.KeyValuePair<int, int>> typeToKeyAndJumpMap;
        private readonly global::System.Collections.Generic.Dictionary<int, int> keyToJumpMap;

        public IFromLobbyFormatter()
        {
            this.typeToKeyAndJumpMap = new global::System.Collections.Generic.Dictionary<global::System.RuntimeTypeHandle, global::System.Collections.Generic.KeyValuePair<int, int>>(5, global::MessagePack.Internal.RuntimeTypeHandleEqualityComparer.Default)
            {
                { typeof(global::Elympics.Models.Matchmaking.WebSocket.Ping).TypeHandle, new global::System.Collections.Generic.KeyValuePair<int, int>(0, 0) },
                { typeof(global::Elympics.Models.Matchmaking.WebSocket.Pong).TypeHandle, new global::System.Collections.Generic.KeyValuePair<int, int>(1, 1) },
                { typeof(global::Elympics.Models.Matchmaking.WebSocket.MatchFound).TypeHandle, new global::System.Collections.Generic.KeyValuePair<int, int>(2, 2) },
                { typeof(global::Elympics.Models.Matchmaking.WebSocket.MatchData).TypeHandle, new global::System.Collections.Generic.KeyValuePair<int, int>(3, 3) },
                { typeof(global::Elympics.Models.Matchmaking.WebSocket.MatchmakingError).TypeHandle, new global::System.Collections.Generic.KeyValuePair<int, int>(4, 4) },
            };
            this.keyToJumpMap = new global::System.Collections.Generic.Dictionary<int, int>(5)
            {
                { 0, 0 },
                { 1, 1 },
                { 2, 2 },
                { 3, 3 },
                { 4, 4 },
            };
        }

        public void Serialize(ref global::MessagePack.MessagePackWriter writer, global::Elympics.Models.Matchmaking.WebSocket.IFromLobby value, global::MessagePack.MessagePackSerializerOptions options)
        {
            global::System.Collections.Generic.KeyValuePair<int, int> keyValuePair;
            if (value != null && this.typeToKeyAndJumpMap.TryGetValue(value.GetType().TypeHandle, out keyValuePair))
            {
                writer.WriteArrayHeader(2);
                writer.WriteInt32(keyValuePair.Key);
                switch (keyValuePair.Value)
                {
                    case 0:
                        options.Resolver.GetFormatterWithVerify<global::Elympics.Models.Matchmaking.WebSocket.Ping>().Serialize(ref writer, (global::Elympics.Models.Matchmaking.WebSocket.Ping)value, options);
                        break;
                    case 1:
                        options.Resolver.GetFormatterWithVerify<global::Elympics.Models.Matchmaking.WebSocket.Pong>().Serialize(ref writer, (global::Elympics.Models.Matchmaking.WebSocket.Pong)value, options);
                        break;
                    case 2:
                        options.Resolver.GetFormatterWithVerify<global::Elympics.Models.Matchmaking.WebSocket.MatchFound>().Serialize(ref writer, (global::Elympics.Models.Matchmaking.WebSocket.MatchFound)value, options);
                        break;
                    case 3:
                        options.Resolver.GetFormatterWithVerify<global::Elympics.Models.Matchmaking.WebSocket.MatchData>().Serialize(ref writer, (global::Elympics.Models.Matchmaking.WebSocket.MatchData)value, options);
                        break;
                    case 4:
                        options.Resolver.GetFormatterWithVerify<global::Elympics.Models.Matchmaking.WebSocket.MatchmakingError>().Serialize(ref writer, (global::Elympics.Models.Matchmaking.WebSocket.MatchmakingError)value, options);
                        break;
                    default:
                        break;
                }

                return;
            }

            writer.WriteNil();
        }

        public global::Elympics.Models.Matchmaking.WebSocket.IFromLobby Deserialize(ref global::MessagePack.MessagePackReader reader, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return null;
            }

            if (reader.ReadArrayHeader() != 2)
            {
                throw new global::System.InvalidOperationException("Invalid Union data was detected. Type:global::Elympics.Models.Matchmaking.WebSocket.IFromLobby");
            }

            options.Security.DepthStep(ref reader);
            var key = reader.ReadInt32();

            if (!this.keyToJumpMap.TryGetValue(key, out key))
            {
                key = -1;
            }

            global::Elympics.Models.Matchmaking.WebSocket.IFromLobby result = null;
            switch (key)
            {
                case 0:
                    result = (global::Elympics.Models.Matchmaking.WebSocket.IFromLobby)options.Resolver.GetFormatterWithVerify<global::Elympics.Models.Matchmaking.WebSocket.Ping>().Deserialize(ref reader, options);
                    break;
                case 1:
                    result = (global::Elympics.Models.Matchmaking.WebSocket.IFromLobby)options.Resolver.GetFormatterWithVerify<global::Elympics.Models.Matchmaking.WebSocket.Pong>().Deserialize(ref reader, options);
                    break;
                case 2:
                    result = (global::Elympics.Models.Matchmaking.WebSocket.IFromLobby)options.Resolver.GetFormatterWithVerify<global::Elympics.Models.Matchmaking.WebSocket.MatchFound>().Deserialize(ref reader, options);
                    break;
                case 3:
                    result = (global::Elympics.Models.Matchmaking.WebSocket.IFromLobby)options.Resolver.GetFormatterWithVerify<global::Elympics.Models.Matchmaking.WebSocket.MatchData>().Deserialize(ref reader, options);
                    break;
                case 4:
                    result = (global::Elympics.Models.Matchmaking.WebSocket.IFromLobby)options.Resolver.GetFormatterWithVerify<global::Elympics.Models.Matchmaking.WebSocket.MatchmakingError>().Deserialize(ref reader, options);
                    break;
                default:
                    reader.Skip();
                    break;
            }

            reader.Depth--;
            return result;
        }
    }

    public sealed class IToLobbyFormatter : global::MessagePack.Formatters.IMessagePackFormatter<global::Elympics.Models.Matchmaking.WebSocket.IToLobby>
    {
        private readonly global::System.Collections.Generic.Dictionary<global::System.RuntimeTypeHandle, global::System.Collections.Generic.KeyValuePair<int, int>> typeToKeyAndJumpMap;
        private readonly global::System.Collections.Generic.Dictionary<int, int> keyToJumpMap;

        public IToLobbyFormatter()
        {
            this.typeToKeyAndJumpMap = new global::System.Collections.Generic.Dictionary<global::System.RuntimeTypeHandle, global::System.Collections.Generic.KeyValuePair<int, int>>(4, global::MessagePack.Internal.RuntimeTypeHandleEqualityComparer.Default)
            {
                { typeof(global::Elympics.Models.Matchmaking.WebSocket.Ping).TypeHandle, new global::System.Collections.Generic.KeyValuePair<int, int>(0, 0) },
                { typeof(global::Elympics.Models.Matchmaking.WebSocket.Pong).TypeHandle, new global::System.Collections.Generic.KeyValuePair<int, int>(1, 1) },
                { typeof(global::Elympics.Models.Matchmaking.WebSocket.GameData).TypeHandle, new global::System.Collections.Generic.KeyValuePair<int, int>(2, 2) },
                { typeof(global::Elympics.Models.Matchmaking.WebSocket.JoinMatchmaker).TypeHandle, new global::System.Collections.Generic.KeyValuePair<int, int>(3, 3) },
            };
            this.keyToJumpMap = new global::System.Collections.Generic.Dictionary<int, int>(4)
            {
                { 0, 0 },
                { 1, 1 },
                { 2, 2 },
                { 3, 3 },
            };
        }

        public void Serialize(ref global::MessagePack.MessagePackWriter writer, global::Elympics.Models.Matchmaking.WebSocket.IToLobby value, global::MessagePack.MessagePackSerializerOptions options)
        {
            global::System.Collections.Generic.KeyValuePair<int, int> keyValuePair;
            if (value != null && this.typeToKeyAndJumpMap.TryGetValue(value.GetType().TypeHandle, out keyValuePair))
            {
                writer.WriteArrayHeader(2);
                writer.WriteInt32(keyValuePair.Key);
                switch (keyValuePair.Value)
                {
                    case 0:
                        options.Resolver.GetFormatterWithVerify<global::Elympics.Models.Matchmaking.WebSocket.Ping>().Serialize(ref writer, (global::Elympics.Models.Matchmaking.WebSocket.Ping)value, options);
                        break;
                    case 1:
                        options.Resolver.GetFormatterWithVerify<global::Elympics.Models.Matchmaking.WebSocket.Pong>().Serialize(ref writer, (global::Elympics.Models.Matchmaking.WebSocket.Pong)value, options);
                        break;
                    case 2:
                        options.Resolver.GetFormatterWithVerify<global::Elympics.Models.Matchmaking.WebSocket.GameData>().Serialize(ref writer, (global::Elympics.Models.Matchmaking.WebSocket.GameData)value, options);
                        break;
                    case 3:
                        options.Resolver.GetFormatterWithVerify<global::Elympics.Models.Matchmaking.WebSocket.JoinMatchmaker>().Serialize(ref writer, (global::Elympics.Models.Matchmaking.WebSocket.JoinMatchmaker)value, options);
                        break;
                    default:
                        break;
                }

                return;
            }

            writer.WriteNil();
        }

        public global::Elympics.Models.Matchmaking.WebSocket.IToLobby Deserialize(ref global::MessagePack.MessagePackReader reader, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return null;
            }

            if (reader.ReadArrayHeader() != 2)
            {
                throw new global::System.InvalidOperationException("Invalid Union data was detected. Type:global::Elympics.Models.Matchmaking.WebSocket.IToLobby");
            }

            options.Security.DepthStep(ref reader);
            var key = reader.ReadInt32();

            if (!this.keyToJumpMap.TryGetValue(key, out key))
            {
                key = -1;
            }

            global::Elympics.Models.Matchmaking.WebSocket.IToLobby result = null;
            switch (key)
            {
                case 0:
                    result = (global::Elympics.Models.Matchmaking.WebSocket.IToLobby)options.Resolver.GetFormatterWithVerify<global::Elympics.Models.Matchmaking.WebSocket.Ping>().Deserialize(ref reader, options);
                    break;
                case 1:
                    result = (global::Elympics.Models.Matchmaking.WebSocket.IToLobby)options.Resolver.GetFormatterWithVerify<global::Elympics.Models.Matchmaking.WebSocket.Pong>().Deserialize(ref reader, options);
                    break;
                case 2:
                    result = (global::Elympics.Models.Matchmaking.WebSocket.IToLobby)options.Resolver.GetFormatterWithVerify<global::Elympics.Models.Matchmaking.WebSocket.GameData>().Deserialize(ref reader, options);
                    break;
                case 3:
                    result = (global::Elympics.Models.Matchmaking.WebSocket.IToLobby)options.Resolver.GetFormatterWithVerify<global::Elympics.Models.Matchmaking.WebSocket.JoinMatchmaker>().Deserialize(ref reader, options);
                    break;
                default:
                    reader.Skip();
                    break;
            }

            reader.Depth--;
            return result;
        }
    }


}

#pragma warning restore 168
#pragma warning restore 414
#pragma warning restore 618
#pragma warning restore 612

#pragma warning restore SA1403 // File may only contain a single namespace
#pragma warning restore SA1649 // File name should match first type name


// <auto-generated>
// THIS (.cs) FILE IS GENERATED BY MPC(MessagePack-CSharp). DO NOT CHANGE IT.
// </auto-generated>

#pragma warning disable 618
#pragma warning disable 612
#pragma warning disable 414
#pragma warning disable 168
#pragma warning disable CS1591 // document public APIs

#pragma warning disable SA1129 // Do not use default value type constructor
#pragma warning disable SA1309 // Field names should not begin with underscore
#pragma warning disable SA1312 // Variable names should begin with lower-case letter
#pragma warning disable SA1403 // File may only contain a single namespace
#pragma warning disable SA1649 // File name should match first type name

namespace MessagePack.Formatters.Elympics.Models.Matchmaking.WebSocket
{
    public sealed class GameDataFormatter : global::MessagePack.Formatters.IMessagePackFormatter<global::Elympics.Models.Matchmaking.WebSocket.GameData>
    {

        public void Serialize(ref global::MessagePack.MessagePackWriter writer, global::Elympics.Models.Matchmaking.WebSocket.GameData value, global::MessagePack.MessagePackSerializerOptions options)
        {
            global::MessagePack.IFormatterResolver formatterResolver = options.Resolver;
            writer.WriteArrayHeader(3);
            global::MessagePack.FormatterResolverExtensions.GetFormatterWithVerify<string>(formatterResolver).Serialize(ref writer, value.SdkVersion, options);
            global::MessagePack.FormatterResolverExtensions.GetFormatterWithVerify<global::System.Guid>(formatterResolver).Serialize(ref writer, value.GameId, options);
            global::MessagePack.FormatterResolverExtensions.GetFormatterWithVerify<string>(formatterResolver).Serialize(ref writer, value.GameVersion, options);
        }

        public global::Elympics.Models.Matchmaking.WebSocket.GameData Deserialize(ref global::MessagePack.MessagePackReader reader, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                throw new global::System.InvalidOperationException("typecode is null, struct not supported");
            }

            options.Security.DepthStep(ref reader);
            global::MessagePack.IFormatterResolver formatterResolver = options.Resolver;
            var length = reader.ReadArrayHeader();
            var __SdkVersion__ = default(string);
            var __GameId__ = default(global::System.Guid);
            var __GameVersion__ = default(string);

            for (int i = 0; i < length; i++)
            {
                switch (i)
                {
                    case 0:
                        __SdkVersion__ = global::MessagePack.FormatterResolverExtensions.GetFormatterWithVerify<string>(formatterResolver).Deserialize(ref reader, options);
                        break;
                    case 1:
                        __GameId__ = global::MessagePack.FormatterResolverExtensions.GetFormatterWithVerify<global::System.Guid>(formatterResolver).Deserialize(ref reader, options);
                        break;
                    case 2:
                        __GameVersion__ = global::MessagePack.FormatterResolverExtensions.GetFormatterWithVerify<string>(formatterResolver).Deserialize(ref reader, options);
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            var ____result = new global::Elympics.Models.Matchmaking.WebSocket.GameData(__SdkVersion__, __GameId__, __GameVersion__);
            reader.Depth--;
            return ____result;
        }
    }

    public sealed class JoinMatchmakerFormatter : global::MessagePack.Formatters.IMessagePackFormatter<global::Elympics.Models.Matchmaking.WebSocket.JoinMatchmaker>
    {

        public void Serialize(ref global::MessagePack.MessagePackWriter writer, global::Elympics.Models.Matchmaking.WebSocket.JoinMatchmaker value, global::MessagePack.MessagePackSerializerOptions options)
        {
            global::MessagePack.IFormatterResolver formatterResolver = options.Resolver;
            writer.WriteArrayHeader(4);
            global::MessagePack.FormatterResolverExtensions.GetFormatterWithVerify<string>(formatterResolver).Serialize(ref writer, value.QueueName, options);
            global::MessagePack.FormatterResolverExtensions.GetFormatterWithVerify<string>(formatterResolver).Serialize(ref writer, value.RegionName, options);
            writer.Write(value.GameEngineData);
            global::MessagePack.FormatterResolverExtensions.GetFormatterWithVerify<float[]>(formatterResolver).Serialize(ref writer, value.MatchmakerData, options);
        }

        public global::Elympics.Models.Matchmaking.WebSocket.JoinMatchmaker Deserialize(ref global::MessagePack.MessagePackReader reader, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                throw new global::System.InvalidOperationException("typecode is null, struct not supported");
            }

            options.Security.DepthStep(ref reader);
            global::MessagePack.IFormatterResolver formatterResolver = options.Resolver;
            var length = reader.ReadArrayHeader();
            var __QueueName__ = default(string);
            var __RegionName__ = default(string);
            var __GameEngineData__ = default(byte[]);
            var __MatchmakerData__ = default(float[]);

            for (int i = 0; i < length; i++)
            {
                switch (i)
                {
                    case 0:
                        __QueueName__ = global::MessagePack.FormatterResolverExtensions.GetFormatterWithVerify<string>(formatterResolver).Deserialize(ref reader, options);
                        break;
                    case 1:
                        __RegionName__ = global::MessagePack.FormatterResolverExtensions.GetFormatterWithVerify<string>(formatterResolver).Deserialize(ref reader, options);
                        break;
                    case 2:
                        __GameEngineData__ = global::MessagePack.Internal.CodeGenHelpers.GetArrayFromNullableSequence(reader.ReadBytes());
                        break;
                    case 3:
                        __MatchmakerData__ = global::MessagePack.FormatterResolverExtensions.GetFormatterWithVerify<float[]>(formatterResolver).Deserialize(ref reader, options);
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            var ____result = new global::Elympics.Models.Matchmaking.WebSocket.JoinMatchmaker(__QueueName__, __RegionName__, __GameEngineData__, __MatchmakerData__);
            reader.Depth--;
            return ____result;
        }
    }

    public sealed class MatchDataFormatter : global::MessagePack.Formatters.IMessagePackFormatter<global::Elympics.Models.Matchmaking.WebSocket.MatchData>
    {

        public void Serialize(ref global::MessagePack.MessagePackWriter writer, global::Elympics.Models.Matchmaking.WebSocket.MatchData value, global::MessagePack.MessagePackSerializerOptions options)
        {
            global::MessagePack.IFormatterResolver formatterResolver = options.Resolver;
            writer.WriteArrayHeader(9);
            global::MessagePack.FormatterResolverExtensions.GetFormatterWithVerify<global::System.Guid>(formatterResolver).Serialize(ref writer, value.MatchId, options);
            global::MessagePack.FormatterResolverExtensions.GetFormatterWithVerify<string>(formatterResolver).Serialize(ref writer, value.UserSecret, options);
            global::MessagePack.FormatterResolverExtensions.GetFormatterWithVerify<string>(formatterResolver).Serialize(ref writer, value.QueueName, options);
            global::MessagePack.FormatterResolverExtensions.GetFormatterWithVerify<string>(formatterResolver).Serialize(ref writer, value.RegionName, options);
            writer.Write(value.GameEngineData);
            global::MessagePack.FormatterResolverExtensions.GetFormatterWithVerify<float[]>(formatterResolver).Serialize(ref writer, value.MatchmakerData, options);
            global::MessagePack.FormatterResolverExtensions.GetFormatterWithVerify<string>(formatterResolver).Serialize(ref writer, value.TcpUdpServerAddress, options);
            global::MessagePack.FormatterResolverExtensions.GetFormatterWithVerify<string>(formatterResolver).Serialize(ref writer, value.WebServerAddress, options);
            global::MessagePack.FormatterResolverExtensions.GetFormatterWithVerify<global::System.Guid[]>(formatterResolver).Serialize(ref writer, value.MatchedPlayersId, options);
        }

        public global::Elympics.Models.Matchmaking.WebSocket.MatchData Deserialize(ref global::MessagePack.MessagePackReader reader, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                throw new global::System.InvalidOperationException("typecode is null, struct not supported");
            }

            options.Security.DepthStep(ref reader);
            global::MessagePack.IFormatterResolver formatterResolver = options.Resolver;
            var length = reader.ReadArrayHeader();
            var __MatchId__ = default(global::System.Guid);
            var __UserSecret__ = default(string);
            var __QueueName__ = default(string);
            var __RegionName__ = default(string);
            var __GameEngineData__ = default(byte[]);
            var __MatchmakerData__ = default(float[]);
            var __TcpUdpServerAddress__ = default(string);
            var __WebServerAddress__ = default(string);
            var __MatchedPlayersId__ = default(global::System.Guid[]);

            for (int i = 0; i < length; i++)
            {
                switch (i)
                {
                    case 0:
                        __MatchId__ = global::MessagePack.FormatterResolverExtensions.GetFormatterWithVerify<global::System.Guid>(formatterResolver).Deserialize(ref reader, options);
                        break;
                    case 1:
                        __UserSecret__ = global::MessagePack.FormatterResolverExtensions.GetFormatterWithVerify<string>(formatterResolver).Deserialize(ref reader, options);
                        break;
                    case 2:
                        __QueueName__ = global::MessagePack.FormatterResolverExtensions.GetFormatterWithVerify<string>(formatterResolver).Deserialize(ref reader, options);
                        break;
                    case 3:
                        __RegionName__ = global::MessagePack.FormatterResolverExtensions.GetFormatterWithVerify<string>(formatterResolver).Deserialize(ref reader, options);
                        break;
                    case 4:
                        __GameEngineData__ = global::MessagePack.Internal.CodeGenHelpers.GetArrayFromNullableSequence(reader.ReadBytes());
                        break;
                    case 5:
                        __MatchmakerData__ = global::MessagePack.FormatterResolverExtensions.GetFormatterWithVerify<float[]>(formatterResolver).Deserialize(ref reader, options);
                        break;
                    case 6:
                        __TcpUdpServerAddress__ = global::MessagePack.FormatterResolverExtensions.GetFormatterWithVerify<string>(formatterResolver).Deserialize(ref reader, options);
                        break;
                    case 7:
                        __WebServerAddress__ = global::MessagePack.FormatterResolverExtensions.GetFormatterWithVerify<string>(formatterResolver).Deserialize(ref reader, options);
                        break;
                    case 8:
                        __MatchedPlayersId__ = global::MessagePack.FormatterResolverExtensions.GetFormatterWithVerify<global::System.Guid[]>(formatterResolver).Deserialize(ref reader, options);
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            var ____result = new global::Elympics.Models.Matchmaking.WebSocket.MatchData(__MatchId__, __UserSecret__, __QueueName__, __RegionName__, __GameEngineData__, __MatchmakerData__, __TcpUdpServerAddress__, __WebServerAddress__, __MatchedPlayersId__);
            reader.Depth--;
            return ____result;
        }
    }

    public sealed class MatchFoundFormatter : global::MessagePack.Formatters.IMessagePackFormatter<global::Elympics.Models.Matchmaking.WebSocket.MatchFound>
    {

        public void Serialize(ref global::MessagePack.MessagePackWriter writer, global::Elympics.Models.Matchmaking.WebSocket.MatchFound value, global::MessagePack.MessagePackSerializerOptions options)
        {
            global::MessagePack.IFormatterResolver formatterResolver = options.Resolver;
            writer.WriteArrayHeader(1);
            global::MessagePack.FormatterResolverExtensions.GetFormatterWithVerify<global::System.Guid>(formatterResolver).Serialize(ref writer, value.MatchId, options);
        }

        public global::Elympics.Models.Matchmaking.WebSocket.MatchFound Deserialize(ref global::MessagePack.MessagePackReader reader, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                throw new global::System.InvalidOperationException("typecode is null, struct not supported");
            }

            options.Security.DepthStep(ref reader);
            global::MessagePack.IFormatterResolver formatterResolver = options.Resolver;
            var length = reader.ReadArrayHeader();
            var __MatchId__ = default(global::System.Guid);

            for (int i = 0; i < length; i++)
            {
                switch (i)
                {
                    case 0:
                        __MatchId__ = global::MessagePack.FormatterResolverExtensions.GetFormatterWithVerify<global::System.Guid>(formatterResolver).Deserialize(ref reader, options);
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            var ____result = new global::Elympics.Models.Matchmaking.WebSocket.MatchFound(__MatchId__);
            reader.Depth--;
            return ____result;
        }
    }

    public sealed class MatchmakingErrorFormatter : global::MessagePack.Formatters.IMessagePackFormatter<global::Elympics.Models.Matchmaking.WebSocket.MatchmakingError>
    {

        public void Serialize(ref global::MessagePack.MessagePackWriter writer, global::Elympics.Models.Matchmaking.WebSocket.MatchmakingError value, global::MessagePack.MessagePackSerializerOptions options)
        {
            global::MessagePack.IFormatterResolver formatterResolver = options.Resolver;
            writer.WriteArrayHeader(2);
            global::MessagePack.FormatterResolverExtensions.GetFormatterWithVerify<global::Elympics.Models.Matchmaking.WebSocket.ErrorBlame>(formatterResolver).Serialize(ref writer, value.ErrorBlame, options);
            global::MessagePack.FormatterResolverExtensions.GetFormatterWithVerify<global::Elympics.Models.Matchmaking.WebSocket.MatchmakerStatusCodes>(formatterResolver).Serialize(ref writer, value.StatusCode, options);
        }

        public global::Elympics.Models.Matchmaking.WebSocket.MatchmakingError Deserialize(ref global::MessagePack.MessagePackReader reader, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                throw new global::System.InvalidOperationException("typecode is null, struct not supported");
            }

            options.Security.DepthStep(ref reader);
            global::MessagePack.IFormatterResolver formatterResolver = options.Resolver;
            var length = reader.ReadArrayHeader();
            var __ErrorBlame__ = default(global::Elympics.Models.Matchmaking.WebSocket.ErrorBlame);
            var __StatusCode__ = default(global::Elympics.Models.Matchmaking.WebSocket.MatchmakerStatusCodes);

            for (int i = 0; i < length; i++)
            {
                switch (i)
                {
                    case 0:
                        __ErrorBlame__ = global::MessagePack.FormatterResolverExtensions.GetFormatterWithVerify<global::Elympics.Models.Matchmaking.WebSocket.ErrorBlame>(formatterResolver).Deserialize(ref reader, options);
                        break;
                    case 1:
                        __StatusCode__ = global::MessagePack.FormatterResolverExtensions.GetFormatterWithVerify<global::Elympics.Models.Matchmaking.WebSocket.MatchmakerStatusCodes>(formatterResolver).Deserialize(ref reader, options);
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            var ____result = new global::Elympics.Models.Matchmaking.WebSocket.MatchmakingError(__ErrorBlame__, __StatusCode__);
            reader.Depth--;
            return ____result;
        }
    }

    public sealed class PingFormatter : global::MessagePack.Formatters.IMessagePackFormatter<global::Elympics.Models.Matchmaking.WebSocket.Ping>
    {

        public void Serialize(ref global::MessagePack.MessagePackWriter writer, global::Elympics.Models.Matchmaking.WebSocket.Ping value, global::MessagePack.MessagePackSerializerOptions options)
        {
            writer.WriteArrayHeader(0);
        }

        public global::Elympics.Models.Matchmaking.WebSocket.Ping Deserialize(ref global::MessagePack.MessagePackReader reader, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                throw new global::System.InvalidOperationException("typecode is null, struct not supported");
            }

            reader.Skip();
            return new global::Elympics.Models.Matchmaking.WebSocket.Ping();
        }
    }

    public sealed class PongFormatter : global::MessagePack.Formatters.IMessagePackFormatter<global::Elympics.Models.Matchmaking.WebSocket.Pong>
    {

        public void Serialize(ref global::MessagePack.MessagePackWriter writer, global::Elympics.Models.Matchmaking.WebSocket.Pong value, global::MessagePack.MessagePackSerializerOptions options)
        {
            writer.WriteArrayHeader(0);
        }

        public global::Elympics.Models.Matchmaking.WebSocket.Pong Deserialize(ref global::MessagePack.MessagePackReader reader, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                throw new global::System.InvalidOperationException("typecode is null, struct not supported");
            }

            reader.Skip();
            return new global::Elympics.Models.Matchmaking.WebSocket.Pong();
        }
    }

}

#pragma warning restore 168
#pragma warning restore 414
#pragma warning restore 618
#pragma warning restore 612

#pragma warning restore SA1129 // Do not use default value type constructor
#pragma warning restore SA1309 // Field names should not begin with underscore
#pragma warning restore SA1312 // Variable names should begin with lower-case letter
#pragma warning restore SA1403 // File may only contain a single namespace
#pragma warning restore SA1649 // File name should match first type name
