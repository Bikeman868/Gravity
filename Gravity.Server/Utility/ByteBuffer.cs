using Gravity.Server.Interfaces;
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
        /// Tests whether the Replace method can be used to replace bytes in this
        /// buffer. Note that this method returnd True for the case where the
        /// replacement continues into subsequent buffers, i.e. the whole replacement
        /// operation is not confined to just this buffer.
        /// </summary>
        /// <param name="offset">The offset into this buffer to start replacing</param>
        /// <param name="count">The number of bytes to replace</param>
        /// <param name="replacementCount">The number of bytes to replace them with</param>
        /// <returns></returns>
        public bool CanReplace(int offset, int count, int replacementCount)
        {
            var additionalBytes = replacementCount - count;
            if (additionalBytes <= 0) return true;

            if (Start + offset + count >= End) return true;
            return additionalBytes <= TailSize;
        }

        /// <summary>
        /// Replaces a range of bytes from the data array with some new byte and
        /// updates the input parameters to allow the replacement to continue in the
        /// next buffer
        /// </summary>
        /// <param name="replacementBytes">The array of bytes to write into this buffer. Can be
        /// null or zero length to simply delete bytes from the buffer</param>
        /// <param name="offset">The offset into this buffer to start overwritng. Returns
        /// the offset where you would write the next chunk or 0 if this buffer is full</param>
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
            if (count == replacementCount)
            {
                ReplaceOwerwrite(replacementBytes, ref offset, ref count, ref replacementStart, ref replacementCount);
                return;
            }

            var tailBytes = End - Start - offset - count;
            var headBytes = offset;

            if (count < replacementCount)
            {
                ReplaceInsert(replacementBytes, ref offset, ref count, ref replacementStart, ref replacementCount, tailBytes, headBytes);
            }
            else
            {
                ReplaceDelete(replacementBytes, ref offset, ref count, ref replacementStart, ref replacementCount, tailBytes, headBytes);
            }
        }

        /// <summary>
        /// Splits this buffer into two buffers and inserts a large number of bytes
        /// in the middle of the data
        /// </summary>
        /// <param name="offset">The offset of the first byte to copy into the new buffer</param>
        /// <param name="bytesToDelete">The number of bytes to delete starting at offset</param>
        /// <param name="bufferPool">This will be used to make the new byte buffer</param>
        /// <param name="newBytes">The bytes to insert</param>
        /// <param name="newBytesStart">The offset into newBytes to start copying</param>
        /// <param name="newBytesCount">The number of bytes to insert from newBytes</param>
        /// <returns>A new ByteBuffer containing the second half of the data</returns>
        public ByteBuffer Insert(
            int offset, 
            int bytesToDelete,
            IBufferPool bufferPool,
            byte[] newBytes,
            int newBytesStart,
            int newBytesCount)
        {
            var moveStartIndex = HeadSize + offset + bytesToDelete;
#if DEBUG
            if (moveStartIndex < 0) throw new ArgumentException("Offset is before buffer start");
#endif
            var bytesToMove = End - moveStartIndex;
#if DEBUG
            if (bytesToMove < 0) throw new ArgumentException("Deleting more bytes than the buffer contains");
#endif
            if (newBytesCount <= TailSize + bytesToDelete)
            {
                if (bytesToMove > 0)
                    Array.Copy(Data, moveStartIndex, Data, HeadSize + offset + newBytesCount, bytesToMove);

                Array.Copy(newBytes, newBytesStart, Data, HeadSize + offset, newBytesCount);
                End += newBytesCount - bytesToDelete;
                return null;
            }
            
            var result = new ByteBuffer(bufferPool.GetAtLeast(newBytesCount + bytesToMove), 0);
            result.Append(newBytes, newBytesStart, newBytesCount);

            if (bytesToMove > 0)
                result.Append(Data, moveStartIndex, bytesToMove);

            End -= bytesToMove + bytesToDelete;

            return result;
        }

        /// <summary>
        /// Deletes some bytes from the buffer
        /// </summary>
        /// <param name="offset">Offset into the buffer to start deleting. Returns 0, which
        /// is where the deleting should contine in the next buffer</param>
        /// <param name="count">The number of bytes to delete. Returns with the number of 
        /// bytes to delete from the next buffer</param>
        public void Delete(
            ref int offset,
            ref int count)
        {
            var replacementStart = 0;
            var replacementCount = 0;
            Replace(null, ref offset, ref count, ref replacementStart, ref replacementCount);
        }

        /// <summary>
        /// Handles the case where the replacement bytes are longer and
        /// therefore need to be inserted into the buffer
        /// </summary>
        private void ReplaceInsert(
            byte[] replacementBytes, 
            ref int offset, 
            ref int count, 
            ref int replacementStart, 
            ref int replacementCount, 
            int tailBytes, 
            int headBytes)
        {
            int insertStart = Start + offset;
            int insertCount = count;

            void moveHead(
                ref int o,
                ref int rs,
                ref int rc)
            {
                var extraSpace = rc - insertCount;
                if (extraSpace <= 0 || HeadSize == 0) return;

                if (extraSpace > HeadSize)
                    extraSpace = HeadSize;

                if (headBytes > 0)
                    Array.Copy(Data, Start, Data, Start - extraSpace, headBytes);

                Start -= extraSpace;
                insertCount += extraSpace;
                insertStart -= extraSpace;
                o -= extraSpace;
            };

            void moveTail(
                ref int o,
                ref int rs,
                ref int rc)
            {
                var extraSpace = rc - insertCount;
                if (extraSpace <= 0 || TailSize == 0) return;

                if (extraSpace > TailSize)
                    extraSpace = TailSize;

                if (tailBytes > 0)
                {
                    var tailOffset = End - tailBytes;
                    Array.Copy(Data, tailOffset, Data, tailOffset + extraSpace, tailBytes);
                }

                End += extraSpace;
                insertCount += extraSpace;
            };

            if (tailBytes > 0 && headBytes < tailBytes)
            {
                moveHead(ref offset, ref replacementStart, ref replacementCount);
                moveTail(ref offset, ref replacementStart, ref replacementCount);
            }
            else
            {
                moveTail(ref offset, ref replacementStart, ref replacementCount);
                moveHead(ref offset, ref replacementStart, ref replacementCount);
            }

            if (tailBytes > 0 && insertCount < replacementCount)
                throw new Exception("Not enough room to insert this many bytes, use the Insert() method instead");

            Array.Copy(replacementBytes, replacementStart, Data, insertStart, insertCount);
            replacementStart += insertCount;
            replacementCount -= insertCount;
        }

        private void ReplaceDelete(
            byte[] replacementBytes, 
            ref int offset, 
            ref int count, 
            ref int replacementStart, 
            ref int replacementCount, 
            int tailBytes, 
            int headBytes)
        {
            var bytesToDelete = count;

            if (Start + offset + count > End)
                bytesToDelete = End - Start - offset;

            if (tailBytes > 0 && headBytes < tailBytes)
            {
                var availableSpace = Start + offset + bytesToDelete;

                if (headBytes > 0)
                    availableSpace -= headBytes;

                var bytesToReplace = replacementCount;

                if (bytesToReplace > availableSpace)
                    bytesToReplace = availableSpace;

                var start = Start + offset + bytesToDelete - bytesToReplace;

                if (headBytes > 0)
                {
                    start -= headBytes;
                    Array.Copy(Data, Start, Data, start, headBytes);
                }

                if (bytesToReplace > 0)
                {
                    Array.Copy(replacementBytes, replacementStart, Data, Start + offset, bytesToReplace);
                    replacementStart += bytesToReplace;
                    replacementCount -= bytesToReplace;
                }

                Start = start;
            }
            else
            {
                var availableSpace = Size - offset;

                if (tailBytes > 0)
                    availableSpace -= tailBytes;

                var bytesToReplace = replacementCount;

                if (bytesToReplace > availableSpace)
                    bytesToReplace = availableSpace;

                var end  = Start + offset + bytesToReplace;

                if (tailBytes > 0)
                {
                    Array.Copy(Data, End - tailBytes, Data, end, tailBytes);
                    end += tailBytes;
                }

                if (bytesToReplace > 0)
                {
                    Array.Copy(replacementBytes, replacementStart, Data, Start + offset, bytesToReplace);
                    replacementStart += bytesToReplace;
                    replacementCount -= bytesToReplace;
                }

                End = end;
            }

            count -= bytesToDelete;
            offset = 0;
        }

        private void ReplaceOwerwrite(
            byte[] replacementBytes, 
            ref int offset, 
            ref int count, 
            ref int replacementStart, 
            ref int replacementCount)
        {
            var bytesToCopy = Start + offset + count > End
                ? End - Start - offset
                : count;

            Array.Copy(replacementBytes, replacementStart, Data, Start + offset, bytesToCopy);

            offset += bytesToCopy;
            count -= bytesToCopy;
            replacementStart += bytesToCopy;
            replacementCount -= bytesToCopy;

            if (offset + Start >= End)
                offset = 0;

            return;
        }

    }
}