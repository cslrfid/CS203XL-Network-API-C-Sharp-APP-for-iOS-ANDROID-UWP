using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Threading.Tasks;

namespace CSLRFIDCommunicationAPI
{
    /// <summary>
    /// CSL RFID Communication Packet API for processing RFID data packets
    /// Uses System.IO.Pipelines for fastest FIFO buffer operations
    /// Packet header mark: 0xa7, second mark: 0xb3, header size: 8 bytes
    /// </summary>
    public sealed class CSLRFIDCommunicationPacketAPI
    {
        // High-speed FIFO buffer using System.IO.Pipelines (fastest Microsoft API)
        private readonly Pipe _communicationBuffer;
        private readonly PipeWriter _writer;
        private readonly PipeReader _reader;
        private const byte PACKET_HEADER_MARK = 0xa7; // packet header mark is 0xa7
        private const byte PACKET_HEADER_SECOND_MARK = 0xb3; // packet header second mark is 0xb3
        private const int PACKET_HEADER_SIZE = 8; // packet header size is 8

        /// <summary>
        /// Initialize the CSL RFID Communication Packet API with 100k high-speed FIFO buffer
        /// Uses System.IO.Pipelines for maximum performance with communicationBuffer
        /// </summary>
        public CSLRFIDCommunicationPacketAPI(int MAX_BUFFER_SIZE = 100000)
        {
            var options = new PipeOptions(
                minimumSegmentSize: 4096,
                pauseWriterThreshold: MAX_BUFFER_SIZE,
                resumeWriterThreshold: MAX_BUFFER_SIZE / 2,
                useSynchronizationContext: false
            );

            _communicationBuffer = new Pipe(options);
            _writer = _communicationBuffer.Writer;
            _reader = _communicationBuffer.Reader;
        }

        public async Task AddData(byte[] data, int dataLength)
        {
            var memory = _writer.GetMemory(dataLength);
            data.AsSpan(0, dataLength).CopyTo(memory.Span);
            _writer.Advance(dataLength);
            await _writer.FlushAsync();
        }

        /// <summary>
        /// Process RFID packet from communicationBuffer using optimized high-speed operations
        /// Streamlined algorithm steps:
        /// 1. Find first 0xa7 byte from communicationBuffer, skip all data before it
        /// 2. Check if communicationBuffer has at least 8 bytes, return null if less
        /// 3. Validate second byte (0xb3) and packet length, retry if invalid
        /// 4. Read and remove complete packet data from communicationBuffer
        /// 5. Return complete packet data
        /// </summary>
        /// <param name="data">The data buffer to process</param>
        /// <param name="dataLength">The actual length of data to process from the buffer</param>
        /// <returns>Complete RFID packet or null if no valid packet available in communicationBuffer</returns>
        public Task<byte[]> ProcessPacketAsync()
        {
            while (true)
            {
                // Step 1: Find first 0xa7 from communicationBuffer and skip data before it
                if (!SkipToPacketHeaderSync())
                {
                    return Task.FromResult<byte[]>(null); // No 0xa7 found in communicationBuffer
                }

                // Step 2: Read buffer data after positioned at 0xa7
                _reader.TryRead(out var readResult);
                var buffer = readResult.Buffer;

                // Step 3: Check if communicationBuffer content has at least 8 bytes
                if (buffer.Length < PACKET_HEADER_SIZE)
                {
                    _reader.AdvanceTo(buffer.Start, buffer.End);
                    return Task.FromResult<byte[]>(null); // communicationBuffer has less than 8 bytes
                }

                // Step 4: Validate packet header and calculate required length
                var bufferArray = buffer.ToArray();
                byte firstByte = bufferArray[0]; // Should be 0xa7
                byte secondByte = bufferArray[1];
                byte thirdByte = bufferArray[2];

                if (secondByte != PACKET_HEADER_SECOND_MARK)
                {
                    // Invalid second byte, skip current 0xa7 and continue
                    var slicedBuffer = buffer.Slice(1);
                    _reader.AdvanceTo(slicedBuffer.Start, buffer.End);
                    continue;
                }

                int requiredLength = PACKET_HEADER_SIZE + thirdByte;

                // Check if communicationBuffer has enough data for complete packet
                if (buffer.Length < requiredLength)
                {
                    _reader.AdvanceTo(buffer.Start, buffer.End);
                    return Task.FromResult<byte[]>(null); // communicationBuffer has insufficient data
                }

                // Step 5: Read and remove complete packet data from communicationBuffer
                var packetData = new byte[requiredLength];
                buffer.Slice(0, requiredLength).CopyTo(packetData);
                var remainingBuffer = buffer.Slice(requiredLength);
                _reader.AdvanceTo(remainingBuffer.Start, buffer.End);

                // Step 6: Return complete packet content
                return Task.FromResult(packetData);
            }
        }

        /// <summary>
        /// Skip all data in communicationBuffer until first 0xa7 byte is found at buffer start
        /// Uses built-in buffer operations for optimal performance
        /// Non-blocking operation - returns immediately if no data available
        /// </summary>
        /// <returns>True if 0xa7 found in communicationBuffer, false if no 0xa7 in communicationBuffer or no data available</returns>
        private Task<bool> SkipToPacketHeaderAsync()
        {
            if (!_reader.TryRead(out var readResult))
            {
                return Task.FromResult(false); // No data available, return immediately
            }

            var buffer = readResult.Buffer;

            // Use built-in buffer search for 0xa7 in communicationBuffer
            var bufferArray = buffer.ToArray();
            for (int i = 0; i < bufferArray.Length; i++)
            {
                if (bufferArray[i] == PACKET_HEADER_MARK)
                {
                    if (i == 0)
                    {
                        // Header is already at the start, no need to advance
                        _reader.AdvanceTo(buffer.Start, buffer.End);
                    }
                    else
                    {
                        // Skip to the position where 0xa7 is found
                        var slice = buffer.Slice(i);
                        _reader.AdvanceTo(slice.Start, buffer.End);
                    }

                    return Task.FromResult(true);
                }
            }

            // No 0xa7 found, consume all current data from communicationBuffer
            _reader.AdvanceTo(buffer.End);
            return Task.FromResult(false);
        }

        /// <summary>
        /// Synchronous version of SkipToPacketHeaderAsync for use within lock
        /// Skip all data in communicationBuffer until first 0xa7 byte is found at buffer start
        /// </summary>
        /// <returns>True if 0xa7 found in communicationBuffer, false if no 0xa7 in communicationBuffer or no data available</returns>
        private bool SkipToPacketHeaderSync()
        {
            if (!_reader.TryRead(out var readResult))
                return false; // No data available, return immediately

            var buffer = readResult.Buffer;

            // Use built-in buffer search for 0xa7 in communicationBuffer
            var bufferArray = buffer.ToArray();
            for (int i = 0; i < bufferArray.Length; i++)
            {
                int cnt = 0;
                if (bufferArray[i] == PACKET_HEADER_MARK)
                {
                    // Found 0xa7, advance to this position and return

                    bool error = false;
                    do 
                    try
                    {
                        error = false;
                        var foundPosition = buffer.GetPosition(i, buffer.Start);
                        _reader.AdvanceTo(foundPosition);
                    }
                    catch (Exception ex)
                    {
                        error = true;
                        cnt++;
                        // Handle any exceptions that may occur during buffer advancement
                        CSLibrary.Debug.WriteLine($"Error advancing buffer position: {ex.Message}");
                    }
                    while (error && cnt < 10);

                    if (cnt > 1)
                        throw new Exception("Error advancing buffer position" + cnt.ToString());
                    return true;
                }
            }

            // No 0xa7 found, consume all current data from communicationBuffer
            _reader.AdvanceTo(buffer.End);
            return false;
        }

        /// <summary>
        /// Clear all data from communicationBuffer - Public API
        /// High-speed operation for manual buffer cleanup
        /// Non-blocking operation - returns immediately if no data available
        /// </summary>
        /// <returns>Task representing the asynchronous clear operation</returns>
        public Task ClearBufferAsync()
        {
            if (_reader.TryRead(out var readResult))
                _reader.AdvanceTo(readResult.Buffer.End);

            return Task.CompletedTask;
        }
    }
}
