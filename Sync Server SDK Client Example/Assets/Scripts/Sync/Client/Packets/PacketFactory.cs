using FlatBuffers;
using UnityEngine;

namespace Servers
{
    /// <summary>
    /// Provides convenience methods to create <see cref="Packet"/>s and/or
    /// their byte array representations.
    /// </summary>
    public static class PacketFactory
    {
        private const int DefaultBufferBytes = 8;
        private class MessageBuilder : FlatBufferBuilder {
            public MessageBuilder() : base(DefaultBufferBytes)
            {
                this.ForceDefaults = true;
            }
        }

        public static Packet BytesToPacket(byte[] bytes)
        {
            return Packet.GetRootAsPacket(new ByteBuffer(bytes));
        }

        public static byte[] MakeConnectBuffer(long userId, string matchId, string matchToken)
        {
            var builder = new MessageBuilder();

            var matchIdOffset = builder.CreateString(matchId);
            var matchTokenOffset = builder.CreateString(matchToken);

            Connect.StartConnect(builder);

            Connect.AddOpcode(builder, (sbyte) Opcode.Connect);
            Connect.AddUserId(builder, userId);
            Connect.AddMatchId(builder, matchIdOffset);
            Connect.AddExternalToken(builder, matchTokenOffset);

            var connectPacketOffset = Connect.EndConnect(builder);
            builder.Finish(connectPacketOffset.Value);

            return builder.SizedByteArray();
        }
      
        public static byte[] MakeForfeitBuffer()
        {
            var builder = new MessageBuilder();

            ForfeitMatch.StartForfeitMatch(builder);

            ForfeitMatch.AddOpcode(builder, new ForfeitMatch().Opcode);

            var offset = ForfeitMatch.EndForfeitMatch(builder);

            builder.Finish(offset.Value);

            return builder.SizedByteArray();
        }
      
        public static byte[] MakeKeepAliveBuffer()
        {
            var builder = new MessageBuilder();

            KeepAlive.StartKeepAlive(builder);

            KeepAlive.AddOpcode(builder, (sbyte)Opcode.KeepAlive);

            var offset = KeepAlive.EndKeepAlive(builder);

            builder.Finish(offset.Value);

            return builder.SizedByteArray();
        }
      
        public static byte[] MakeAppPauseedBuffer()
        {
            var builder = new MessageBuilder();

            AppPaused.StartAppPaused(builder);

            AppPaused.AddOpcode(builder, (sbyte)Opcode.AppPaused);

            var offset = AppPaused.EndAppPaused(builder);

            builder.Finish(offset.Value);

            return builder.SizedByteArray();
        }
      
        public static byte[] MakeAppResumedBuffer()
        {
            var builder = new MessageBuilder();

            AppResumed.StartAppResumed(builder);

            AppResumed.AddOpcode(builder, (sbyte)Opcode.AppResumed);

            var offset = AppResumed.EndAppResumed(builder);

            builder.Finish(offset.Value);

            return builder.SizedByteArray();
        }
      
        public static byte[] MakePlayerInputBuffer(int newScore)
        {
            var builder = new MessageBuilder();

            PlayerInput.StartPlayerInput(builder);

            PlayerInput.AddOpcode(builder, (sbyte)Opcode.PlayerInput);
            PlayerInput.AddNewScore(builder, newScore);

            var offset = PlayerInput.EndPlayerInput(builder);

            builder.Finish(offset.Value);

            return builder.SizedByteArray();
        }
      
        public static byte[] MakeChatBuffer(int chatId)
        {
            var builder = new MessageBuilder();

            Chat.StartChat(builder);

            Chat.AddOpcode(builder, (sbyte)Opcode.Chat);
            Chat.AddChatId(builder, (short) chatId);

            var offset = Chat.EndChat(builder);

            builder.Finish(offset.Value);

            return builder.SizedByteArray();
        }
    }
}