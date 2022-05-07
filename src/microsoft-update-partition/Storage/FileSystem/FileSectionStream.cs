// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IO;

namespace Microsoft.PackageGraph.Storage.Local
{
    class FileSectionStream : Stream
    {
        public override bool CanRead => true;

        public override bool CanSeek => true;

        public override bool CanWrite => false;

        public override long Length => _SectionLength;

        public override long Position
        {
            get => _UnderlyingStream.Position - _StartOffset;
            set
            {
                if (value > _SectionLength)
                {
                    throw new IndexOutOfRangeException();
                }

                _UnderlyingStream.Position = value + _StartOffset;
            }
        }

        private readonly FileStream _UnderlyingStream;

        private readonly long _SectionLength;
        private readonly long _StartOffset;

        public FileSectionStream(FileStream baseStream, long startOffset, long sectionLength)
        {
            _UnderlyingStream = baseStream;
            _SectionLength = sectionLength;
            _StartOffset = startOffset;
            _UnderlyingStream.Seek(startOffset, SeekOrigin.Begin);

            if (sectionLength > _UnderlyingStream.Length - startOffset)
            {
                throw new Exception("Section length is larger than the length of the stream");
            }

        }

        public override void Flush()
        {
            _UnderlyingStream.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var availableBytes = Length - Position;
            if (availableBytes == 0)
            {
                return 0;
            }
            else if (count > availableBytes)
            {
                return _UnderlyingStream.Read(buffer, offset, (int) availableBytes);
            }
            else
            {
                return _UnderlyingStream.Read(buffer, offset, count);
            }
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            if (origin == SeekOrigin.Begin)
            {
                if (offset < 0 || offset > _SectionLength)
                {
                    throw new IndexOutOfRangeException();
                }

                return _UnderlyingStream.Seek(offset + _StartOffset, origin);
            }
            else if (origin == SeekOrigin.End)
            {
                if (offset > 0 || Math.Abs(offset) > _SectionLength)
                {
                    throw new IndexOutOfRangeException();
                }

                var sectionEndOffset = _UnderlyingStream.Length - (_StartOffset + _SectionLength);
                return _UnderlyingStream.Seek(sectionEndOffset + offset, origin);
            }
            else if (origin == SeekOrigin.Current)
            {
                var endPosition = Position + offset ;
                if (endPosition < 0 || endPosition > _SectionLength )
                {
                    throw new IndexOutOfRangeException();
                }

                return _UnderlyingStream.Seek(offset, origin);
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        public override void Close()
        {
            _UnderlyingStream.Close();
        }
    }
}
