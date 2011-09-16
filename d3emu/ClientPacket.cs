﻿using System;
using Google.ProtocolBuffers;

namespace d3emu
{
    class ClientPacket
    {
        private readonly byte m_service;

        public byte Service
        {
            get { return m_service; }
        }

        private readonly int m_method;

        public int Method
        {
            get { return m_method; }
        }

        private readonly short m_requestId;

        public short RequestId
        {
            get { return m_requestId; }
        }

        private readonly long m_unk1;

        public long Unk1
        {
            get { return m_unk1; }
        }

        private readonly CodedInputStream m_stream;

        public ClientPacket(byte[] data)
        {
            m_stream = CodedInputStream.CreateInstance(data);

            // Read header
            m_service = m_stream.ReadRawByte();
            m_method = m_stream.ReadInt32();
            m_requestId = m_stream.ReadInt16();
            m_unk1 = 0;

            if (m_service != 0xFE)
                m_unk1 = m_stream.ReadInt64();
        }

        public TMessage ReadMessage<TMessage, TBuilder>(IBuilder<TMessage, TBuilder> builder)
            where TMessage : IMessage<TMessage, TBuilder>
            where TBuilder : IBuilder<TMessage, TBuilder>
        {
            m_stream.ReadMessage(builder, ExtensionRegistry.Empty);

            if (!m_stream.IsAtEnd)
                throw new Exception("Packet under read!");

            return builder.Build();
        }

        public IMessage ReadMessage(IBuilder builder)
        {
            m_stream.ReadMessage(builder, ExtensionRegistry.Empty);

            if (!m_stream.IsAtEnd)
                throw new Exception("Packet under read!");

            return builder.WeakBuild();
        }
    }
}