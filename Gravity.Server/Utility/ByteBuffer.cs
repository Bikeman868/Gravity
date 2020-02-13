using System;

namespace Gravity.Server.Utility
{
    internal class ByteBuffer
    {
        /// <summary>
        /// The data in this buffer
        /// </summary>
        public byte[] Data;

        /// <summary>
        /// The index of the first byte used byte at the start of the buffer
        /// </summary>
        public int Start;

        /// <summary>
        /// The index of the first unused byte at the end of the buffer
        /// </summary>
        public int End;

        /// <summary>
        /// The number of bytes of data in the buffer
        /// </summary>
        public int Length => End - Start;

        /// <summary>
        /// The maximum number of bytes that can be stored
        /// </summary>
        public int Size => Data.Length;

        /// <summary>
        /// The number of unused bytes at the front of the buffer
        /// </summary>
        public int HeadSize => Start;

        /// <summary>
        /// The number of unused bytes at the back of the buffer
        /// </summary>
        public int TailSize => Data.Length - End;

        /// <summary>
        /// Returns true if this many bytes can be appended
        /// </summary>
        public bool CanAppend(int count) => count <= TailSize;

        /// <summary>
        /// Returns true if this many bytes can be prepended
        /// </summary>
        public bool CanPrepend(int count) => count <= HeadSize;

        public ByteBuffer(byte[] data, int? length = null)
        {
            Data = data;
            End = length ?? Data.Length;
        }

        /// <summary>
        /// Adds bytes to the end of the buffer
        /// </summary>
        public void Append(byte[] bytes, int offset, int count)
        {
            if (End == Start)
            {
                Array.Copy(bytes, offset, Data, 0, count);
                Start = 0;
                End = count;
            }
            else
            {
                Array.Copy(bytes, offset, Data, End, count);
                End += count;
            }
        }

        /// <summary>
        /// Adds bytes to the end of the buffer
        /// </summary>
        public void Prepend(byte[] bytes, int offset, int count)
        {
            if (End == Start)
            {
                End = Data.Length;
                Start = End - count;
            }
            else
            {
                Start -= count;
            }
            Array.Copy(bytes, offset, Data, Start, count);
        }

        /// <summary>
        /// Copies bytes out of the buffer
        /// </summary>
        public void Get(int offset, int count, byte[] destination, int destinationIndex)
        {
            if (count < 1) return;
            Array.Copy(Data, Start + offset, destination, destinationIndex, count);
        }

        /// <summary>
        /// Replaces a range of bytes from the data array with some new bytes.
        /// The new bytes can always be shorter than the bytes that they are replacing
        /// The new bytes can be longer if there are enough unused bytes at the end
        /// </summary>
        public void Replace(int offset, int count, byte[] replacementBytes, int replacementStart, int replacementCount)
        {
#if DEBUG
            if (HeadSize + offset < 0) throw new ArgumentException("Offset is before buffer start");
            if (Start + offset + count > Size) throw new ArgumentException("Offset+count is after buffer end");
#endif
            if (count == replacementCount)
            {
                Array.Copy(replacementBytes, replacementStart, Data, Start + offset, count);
                if (offset < 0) Start += offset;
                if (Start + offset + replacementCount > End) End = Start + offset + replacementCount;
            }
            else
            {
                var tailBytes = End - Start - offset - count;

                if (count < replacementCount)
                {
                    var extraBytes = replacementCount - count;
                    if (tailBytes > 0)
                    {
                        var tailOffset = End - tailBytes;
                        Array.Copy(Data, tailOffset, Data, tailOffset + extraBytes, tailBytes);
                    }
                    Array.Copy(replacementBytes, replacementStart, Data, Start + offset, replacementCount);
                    End += extraBytes;
                }
                else
                {
                    var deletedBytes = count - replacementCount;
                    Array.Copy(replacementBytes, replacementStart, Data, Start + offset, replacementCount);
                    if (tailBytes > 0)
                    {
                        var tailOffset = End - tailBytes;
                        Array.Copy(Data, tailOffset, Data, tailOffset - deletedBytes, tailBytes);
                    }
                    End -= deletedBytes;
                }
            }
        }
    }
}