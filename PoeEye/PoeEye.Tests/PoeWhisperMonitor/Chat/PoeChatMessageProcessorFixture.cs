using System;
using System.Collections.Generic;
using NUnit.Framework;
using PoeWhisperMonitor.Chat;
using Shouldly;

namespace PoeEye.Tests.PoeWhisperMonitor.Chat
{
    [TestFixture]
    public class PoeChatMessageProcessorFixture
    {
        private IEnumerable<TestCaseData> ShouldParseCases()
        {
            yield return new TestCaseData(
                "2017/03/27 22:51:03 717898921 951 [INFO Client 28572] @To OmgSoMainstream: Hi, I would like to buy your Storm Eye Paua Ring listed for 300 chaos in Legacy (stash tab \"~b / o 2 chaos\"; position: left 2, top 2)",
                new PoeMessage
                {
                    Message =
                        "Hi, I would like to buy your Storm Eye Paua Ring listed for 300 chaos in Legacy (stash tab \"~b / o 2 chaos\"; position: left 2, top 2)",
                    MessageType = PoeMessageType.WhisperOutgoing,
                    Timestamp = new DateTime(2017, 03, 27, 22, 51, 03),
                    Name = "OmgSoMainstream"
                });
            yield return new TestCaseData(
                "2017/03/27 23:18:28 719543421 951 [INFO Client 28572] @To RKP: Hi, I would like to buy your Golem Nails Gripped Gloves listed for 1.5 exalted in Legacy (stash tab \"$\"; position: left 0, top 0)",
                new PoeMessage
                {
                    Message =
                        "Hi, I would like to buy your Golem Nails Gripped Gloves listed for 1.5 exalted in Legacy (stash tab \"$\"; position: left 0, top 0)",
                    MessageType = PoeMessageType.WhisperOutgoing,
                    Timestamp = new DateTime(2017, 03, 27, 23, 18, 28),
                    Name = "RKP"
                });
            yield return new TestCaseData(
                "2017/04/06 20:02:10 1571768968 951 [INFO Client 25180] &<(ROA)> FnkAe: xab gb ty",
                new PoeMessage
                {
                    Message = "xab gb ty",
                    MessageType = PoeMessageType.Guild,
                    Name = "FnkAe"
                });
            yield return new TestCaseData(
                "2017/05/01 10:38:00 909686234 951 [INFO Client 62140] <(ROA)> OmgSoMainstream: Hi, I would like to buy your Lingering Remnants listed for 3 chaos in Legacy (stash tab \"T\"; position: left 12, top 2)",
                new PoeMessage
                {
                    Message = "Hi, I would like to buy your Lingering Remnants listed for 3 chaos in Legacy (stash tab \"T\"; position: left 12, top 2)",
                    MessageType = PoeMessageType.Local,
                    Name = "OmgSoMainstream"
                });
            yield return new TestCaseData(
                "2017/04/10 00:59:37 1848815875 951 [INFO Client 32972] %Eragod: ty",
                new PoeMessage
                {
                    Message = "ty",
                    MessageType = PoeMessageType.Party,
                    Name = "Eragod"
                });
            yield return new TestCaseData(
                "2017/03/27 22:51:03 717898921 951 [INFO Client 28572] : AFK mode is now OFF.",
                new PoeMessage
                {
                    Message = "AFK mode is now OFF.",
                    MessageType = PoeMessageType.System
                });
            yield return new TestCaseData(
                "2017/04/10 00:27:00 1846859296 951 [INFO Client 32972] The Shaper: Failure. Ambivalence. Death. A final abyss of unending darkness.",
                new PoeMessage
                {
                    Name = "The Shaper",
                    Message = "Failure. Ambivalence. Death. A final abyss of unending darkness.",
                    MessageType = PoeMessageType.Local
                });
            yield return new TestCaseData(
                "2017/03/27 22:14:11 715687062 70 [INFO Client 28572] Doodad hash: 0",
                new PoeMessage
                {
                    Name = "Doodad hash",
                    Message = "0",
                    MessageType = PoeMessageType.Local
                });
            yield return new TestCaseData(
                "2017/03/29 21:21:44 885339781 6e7 [DEBUG Client 28572] Joined guild named roa with 21 members",
                new PoeMessage
                {
                    Message = "Joined guild named roa with 21 members",
                    MessageType = PoeMessageType.System
                });
            yield return new TestCaseData(
                "2017/03/29 21:22:15 885370453 951 [INFO Client 28572] @From <(ROA)> FnkAe: ty",
                new PoeMessage
                {
                    Message = "ty",
                    MessageType = PoeMessageType.WhisperIncoming,
                    Name = "FnkAe"
                });
        }

        private PoeChatMessageProcessor CreateInstance()
        {
            return new PoeChatMessageProcessor();
        }

        [Test]
        [TestCaseSource(nameof(ShouldParseCases))]
        public void ShouldParse(string message, PoeMessage expected)
        {
            //Given
            var instance = CreateInstance();

            PoeMessage parsedMessage;

            //When
            var result = instance.TryParse(message, out parsedMessage);

            //Then
            result.ShouldBe(true);
            if (expected.Timestamp != default(DateTime))
            {
                parsedMessage.Timestamp.ShouldBe(expected.Timestamp);
            }

            parsedMessage.Name.ShouldBe(expected.Name);
            parsedMessage.MessageType.ShouldBe(expected.MessageType);
            parsedMessage.Message.ShouldBe(expected.Message);
        }
    }
}