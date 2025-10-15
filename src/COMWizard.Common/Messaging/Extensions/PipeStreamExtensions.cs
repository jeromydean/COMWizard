using System;
using System.Buffers;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;

namespace COMWizard.Common.Messaging.Extensions
{
  public static class PipeStreamExtensions
  {
    private const int MaxMessageLength = 10 * 1024 * 1024;
    private const int CopyBufferSize = 81920;

    private static readonly JsonSerializerSettings _serializerSettings = new JsonSerializerSettings
    {
      TypeNameHandling = TypeNameHandling.All
    };

    public static async Task WriteMessageAsync(this PipeStream pipe, MessageBase message, CancellationToken cancellationToken = default)
    {
      using (MemoryStream messageStream = new MemoryStream())
      {
        using (BsonDataWriter writer = new BsonDataWriter(messageStream) { CloseOutput = false })
        {
          JsonSerializer serializer = JsonSerializer.CreateDefault(_serializerSettings);

          serializer.Serialize(writer, message);
          await writer.FlushAsync(cancellationToken).ConfigureAwait(false);
        }

        int messageLength = checked((int)messageStream.Length);

        byte[] header = BitConverter.GetBytes(messageLength);
        if (!BitConverter.IsLittleEndian)
        {
          Array.Reverse(header);
        }

        await pipe.WriteAsync(header, 0, header.Length, cancellationToken).ConfigureAwait(false);

        messageStream.Position = 0;
        await messageStream.CopyToAsync(pipe, CopyBufferSize, cancellationToken).ConfigureAwait(false);
        await pipe.FlushAsync(cancellationToken).ConfigureAwait(false);
      }
    }

    //uncomment and delete existing when we don't have to support .net framework
    //public static async Task WriteMessageAsync(this PipeStream pipe, MessageBase message, CancellationToken cancellationToken)
    //{
    //  using (MemoryStream messageStream = new MemoryStream())
    //  {
    //    using (BsonDataWriter writer = new BsonDataWriter(messageStream)
    //    {
    //      CloseOutput = false
    //    })
    //    {
    //      JsonSerializer serializer = JsonSerializer.CreateDefault(_serializerSettings);
    //      serializer.Serialize(writer, message);
    //      await writer.FlushAsync(cancellationToken);
    //    }

    //    int messageLength = checked((int)messageStream.Length);
    //    byte[] header = new byte[4];
    //    BinaryPrimitives.WriteInt32LittleEndian(header, messageLength);
    //    await pipe.WriteAsync(header.AsMemory(0, 4), cancellationToken).ConfigureAwait(false);

    //    messageStream.Position = 0;
    //    await messageStream.CopyToAsync(pipe, CopyBufferSize, cancellationToken).ConfigureAwait(false);
    //    await pipe.FlushAsync(cancellationToken).ConfigureAwait(false);
    //  }
    //}

    public static async Task<MessageBase> ReadMessageAsync(this PipeStream pipe, CancellationToken cancellationToken = default)
    {
      byte[] header = new byte[4];
      await ReadExactlyAsync(pipe, header, 0, 4, cancellationToken).ConfigureAwait(false);

      int payloadLen = BitConverter.ToInt32(header, 0);
      if (payloadLen < 0 || payloadLen > MaxMessageLength)
      {
        throw new InvalidDataException($"Invalid payload length {payloadLen}.");
      }

      byte[] rented = ArrayPool<byte>.Shared.Rent(payloadLen);
      try
      {
        await ReadExactlyAsync(pipe, rented, 0, payloadLen, cancellationToken).ConfigureAwait(false);

        using (MemoryStream ms = new MemoryStream(rented, 0, payloadLen, writable: false, publiclyVisible: true))
        {
          using (BsonDataReader reader = new BsonDataReader(ms))
          {
            JsonSerializer serializer = JsonSerializer.CreateDefault(_serializerSettings);

            MessageBase? msg = serializer.Deserialize<MessageBase>(reader);
            if (msg == null)
            {
              throw new InvalidDataException("Deserialization returned null.");
            }
            return msg;
          }
        }
      }
      finally
      {
        ArrayPool<byte>.Shared.Return(rented, clearArray: true);
      }
    }

    //uncomment and delete existing when we don't have to support .net framework
    //public static async Task<MessageBase> ReadMessageAsync(this PipeStream pipe, CancellationToken cancellationToken)
    //{
    //  byte[] header = GC.AllocateUninitializedArray<byte>(4);
    //  await pipe.ReadExactlyAsync(header.AsMemory(0, 4), cancellationToken).ConfigureAwait(false);

    //  int payloadLen = BinaryPrimitives.ReadInt32LittleEndian(header);
    //  if (payloadLen < 0 || payloadLen > MaxMessageLength)
    //  {
    //    throw new InvalidDataException($"Invalid payload length {payloadLen}.");
    //  }

    //  byte[] rented = ArrayPool<byte>.Shared.Rent(payloadLen);
    //  try
    //  {
    //    Memory<byte> slice = rented.AsMemory(0, payloadLen);
    //    await pipe.ReadExactlyAsync(slice, cancellationToken).ConfigureAwait(false);

    //    using (MemoryStream ms = new MemoryStream(rented, 0, payloadLen, writable: false, publiclyVisible: true))
    //    {
    //      using (BsonDataReader reader = new BsonDataReader(ms))
    //      {
    //        JsonSerializer serializer = JsonSerializer.CreateDefault(_serializerSettings);
    //        MessageBase? msg = serializer.Deserialize<MessageBase>(reader);
    //        if (msg == null)
    //        {
    //          throw new InvalidDataException("Deserialization returned null.");
    //        }
    //        return msg;
    //      }
    //    }
    //  }
    //  finally
    //  {
    //    ArrayPool<byte>.Shared.Return(rented);
    //  }
    //}

    private static async Task ReadExactlyAsync(Stream stream, byte[] buffer, int offset, int count, CancellationToken ct)
    {
      int readTotal = 0;
      while (readTotal < count)
      {
        int read = await stream.ReadAsync(buffer, offset + readTotal, count - readTotal, ct).ConfigureAwait(false);
        if (read == 0)
        {
          throw new EndOfStreamException("Pipe closed before expected bytes were read.");
        }

        readTotal += read;
      }
    }
  }
}