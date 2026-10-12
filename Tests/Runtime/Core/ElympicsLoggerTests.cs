using System;
using System.Globalization;
using Elympics.Core.Logger;
using Elympics.Core.Logger.Builder;
using NUnit.Framework;
using UnityEngine;

#nullable enable

namespace Elympics.Tests
{
    public class ElympicsLoggerTests
    {
        [Test]
        public void UnityConsoleLoggerOutputShouldBeValid()
        {
            var unityConsoleLogger = new UnityConsoleOutlet(LogOutput);
            const string time = "2026-06-06T06:06:06.6666667Z";
            const string message = "Expected message!";
            const string version = "1.2.3";
            var state = new ApplicationState(version);
            var sessionId = state.SessionId;
            var config = new LoggerConfig().WithMonitoringEnabled();
            string? actualOutput = null;
            var expectedOutput = "[ERR] [ElympicsSdk] " + message + "\n"
                + "\n"
                + "=== Current application state ===\n"
                + "[SdkState] sessionId: " + sessionId + " | sdkVersion: " + version + "\n";

            unityConsoleLogger.Log(LogCategory.Error,
                DateTime.ParseExact(time, "o", CultureInfo.InvariantCulture),
                message,
                null,
                state,
                config);

            Assert.That(actualOutput, Is.EqualTo(expectedOutput));

            void LogOutput(
                LogType logType,
                LogOption logOptions,
                UnityEngine.Object? context,
                string format,
                params object[] args)
            {
                actualOutput = string.Format(format, args);
            }
        }

        [Test]
        public void PlayPadBreadcrumbLoggerOutputShouldBeValid()
        {
            var playpadLogger = new PlaypadBreadcrumbOutlet(LogOutput);
            const string time = "2026-06-06T06:06:06.6666667Z";
            const string message = "Expected message!";
            const string version = "1.2.3";
            var state = new ApplicationState(version);
            var sessionId = state.SessionId;
            var config = new LoggerConfig().WithMonitoringEnabled();
            LoggerOutputOuter? actualOutput = null;
            var expectedOutput = new LoggerOutputOuter
            {
                level = (int)LogLevel.Error,
                message = message,
                data = new LoggerOutputInner
                {
                    time = time,
                    sdkVersion = version,
                    sessionId = sessionId,
                },
            };

            playpadLogger.Log(LogCategory.Error,
                DateTime.ParseExact(time, "o", CultureInfo.InvariantCulture),
                message,
                null,
                state,
                config);

            Assert.That(actualOutput, Is.EqualTo(expectedOutput));

            void LogOutput(LogLevel logLevel, string isoTimestamp, string messageJson)
            {
                Debug.Log(messageJson);
                actualOutput = JsonUtility.FromJson<LoggerOutputOuter>(messageJson);
            }
        }

        [Serializable]
        private struct LoggerOutputOuter : IEquatable<LoggerOutputOuter>
        {
            public int level;
            public string? message;
            public LoggerOutputInner data;

            public bool Equals(LoggerOutputOuter other) =>
                level == other.level
                && message == other.message
                && data.Equals(other.data);

            public override string ToString() => JsonUtility.ToJson(this);
        }

        [Serializable]
        private struct LoggerOutputInner : IEquatable<LoggerOutputInner>
        {
            public string? time;
            public string? sdkVersion;
            public string? sessionId;
            public string? serviceName;
            public string? versionName;
            public string? gameId;
            public string? userId;
            public string? authType;
            public string? nickname;
            public string? walletAddress;
            public string? ip;
            public string? fleetId;
            public string? region;
            public string? apiUrl;
            public string? roomId;
            public string? queueName;
            public string? matchId;
            public string? serverAddress;
            public string? capabilities;
            public string? tournamentId;
            public string? featureAccess;
            public string? className;
            public string? methodName;

            public bool Equals(LoggerOutputInner other) =>
                time == other.time
                && sdkVersion == other.sdkVersion
                && sessionId == other.sessionId
                && serviceName == other.serviceName
                && versionName == other.versionName
                && gameId == other.gameId
                && userId == other.userId
                && authType == other.authType
                && nickname == other.nickname
                && walletAddress == other.walletAddress
                && ip == other.ip
                && fleetId == other.fleetId
                && region == other.region
                && apiUrl == other.apiUrl
                && roomId == other.roomId
                && queueName == other.queueName
                && matchId == other.matchId
                && serverAddress == other.serverAddress
                && capabilities == other.capabilities
                && tournamentId == other.tournamentId
                && featureAccess == other.featureAccess
                && className == other.className
                && methodName == other.methodName;
        }
    }
}
