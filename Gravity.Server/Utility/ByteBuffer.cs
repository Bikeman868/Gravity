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
        public bool CanPrepend(int count) => End == Start ? count <= Size : count <= HeadSize;

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
        /// Replaces a range of bytes from the data array with some new byte and
        /// updates the input parameters to allow the replacement to continue in the
        /// next buffer
        /// </summary>
        /// <param name="replacementBytes">The array of bytes to write into this buffer. Can be
        /// null or zero length to simply delete bytes from the buffer</param>
        /// <param name="offset">The offset into this buffer to start overwritng. Returns
        /// zero because this is where you would start overwriting in the next buffer</param>
        /// <param name="count">The number of bytes to overwrite. Can be 0 to insert bytes
        /// without overwriting existing ones. Returns the number of bytes remaining to
        /// be overwritten in the next buffer</param>
        /// <param name="replacementStart">The offset into replacementBytes to start copying.
        /// This routine increments this offset by the number of bytes copied from replacementBytes</param>
        /// <param name="replacementCount">The number of bytes to copy from replacementBytes. Can
        /// be zero to delete bytes from the buffer without replacing them</param>
        public void Replace(
            byte[] replacementBytes, 
            ref int offset, 
            ref int count, 
            ref int replacementStart, 
            ref int replacementCount)
        {
#if DEBUG
            if (HeadSize + offset < 0) throw new ArgumentException("Offset is before buffer start");
#endif
            if (count == replacementCount && offset >= 0)
            {
                // Straight overwrite with no moving bytes around

                var bytesToCopy = Start + offset + count > End 
                    ? End - Start - offset
                    : count;

                Array.Copy(replacementBytes, replacementStart, Data, Start + offset, bytesToCopy);

                offset = 0;
                count -= bytesToCopy;
                replacementStart += bytesToCopy;
                replacementCount -= bytesToCopy;

                return;
            }

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