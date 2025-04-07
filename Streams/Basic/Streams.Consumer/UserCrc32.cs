using RabbitMQ.Stream.Client;

namespace Streams.Consumer;

public class UserCrc32 : ICrc32
{
    public byte[] Hash(byte[] data)
    {
        return System.IO.Hashing.Crc32.Hash(data);
    }
}