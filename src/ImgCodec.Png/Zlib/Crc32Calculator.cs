// Crc32.cs
// ------------------------------------------------------------------
//
// Copyright (c) 2006-2009 Dino Chiesa and Microsoft Corporation.
// All rights reserved.
//
// This code module is part of DotNetZip, a zipfile class library.
//
// ------------------------------------------------------------------
//
// This code is licensed under the Microsoft Public License.
// See the file License.txt for the license details.
// More info on: http://dotnetzip.codeplex.com
//
// ------------------------------------------------------------------
//
// last saved (in emacs):
// Time-stamp: <2010-January-16 13:16:27>
//
// ------------------------------------------------------------------
//
// Implements the CRC algorithm, which is used in zip files.  The zip format calls for
// the zipfile to contain a CRC for the unencrypted byte stream of each file.
//
// It is based on example source code published at
//    http://www.vbaccelerator.com/home/net/code/libraries/CRC32/Crc32_zip_CRC32_CRC32_cs.asp
//
// This implementation adds a tweak of that code for use within zip creation.  While
// computing the CRC we also compress the byte stream, in the same read loop. This
// avoids the need to read through the uncompressed stream twice - once to compute CRC
// and another time to compress.
//
// ------------------------------------------------------------------



using System;
using Interop = System.Runtime.InteropServices;


namespace Ionic.Zlib
{
    /// <summary>
    /// Calculates a 32bit Cyclic Redundancy Checksum (CRC) using the same polynomial
    /// used by Zip. This type is used internally by DotNetZip; it is generally not used
    /// directly by applications wishing to create, read, or manipulate zip archive
    /// files.
    /// </summary>

    //    //[Interop.GuidAttribute("ebc25cf6-9120-4283-b972-0e5520d0000C")]
    //    //[Interop.ComVisible(true)]
    //#if !NETCF
    //    [Interop.ClassInterface(Interop.ClassInterfaceType.AutoDispatch)]
    //#endif
    public class CRC32Calculator
    {
        public CRC32Calculator()
        {

        }
        /// <summary>
        /// indicates the total number of bytes read on the CRC stream.
        /// This is used when writing the ZipDirEntry when compressing files.
        /// </summary>
        public Int64 TotalBytesRead
        {
            get
            {
                return _TotalBytesRead;
            }
        }

        /// <summary>
        /// Indicates the current CRC for all blocks slurped in.
        /// </summary>
        public Int32 Crc32Result
        {
            get
            {
                // return one's complement of the running result
                return unchecked((Int32)(~_RunningCrc32Result));
            }
        }

        ///// <summary>
        ///// Returns the CRC32 for the specified stream.
        ///// </summary>
        ///// <param name="input">The stream over which to calculate the CRC32</param>
        ///// <returns>the CRC32 calculation</returns>
        //public static Int32 GetCrc32(System.IO.Stream input)
        //{
        //    internalUseOnly.Reset();
        //    return internalUseOnly.GetCrc32AndCopy(input, null);
        //}

        //static CRC32Calculator internalUseOnly = new CRC32Calculator();
        /// <summary>
        /// reset, before reuse
        /// </summary>
        public void Reset()
        {
            //reset
            this._RunningCrc32Result = 0xFFFFFFFF;
            this._TotalBytesRead = 0;
        }
        ///// <summary>
        ///// Returns the CRC32 for the specified stream, and writes the input into the
        ///// output stream.
        ///// </summary>
        ///// <param name="input">The stream over which to calculate the CRC32</param>
        ///// <param name="output">The stream into which to deflate the input</param>
        ///// <returns>the CRC32 calculation</returns>
        //Int32 GetCrc32AndCopy(System.IO.Stream input, System.IO.Stream output)
        //{
        //    if (input == null)
        //        throw new ZlibException("The input stream must not be null.");

        //    unchecked
        //    {
        //        //UInt32 crc32Result;
        //        //crc32Result = 0xFFFFFFFF;
        //        byte[] buffer = new byte[BUFFER_SIZE];
        //        int readSize = BUFFER_SIZE;

        //        int count = input.Read(buffer, 0, readSize);
        //        int tmpTotalBytesRead = count;
        //        if (output != null)
        //        {
        //            output.Write(buffer, 0, count);
        //        }


        //        while (count > 0)
        //        {
        //            SlurpBlock(buffer, 0, count);
        //            count = input.Read(buffer, 0, readSize);
        //            if (output != null)
        //            {
        //                output.Write(buffer, 0, count);
        //            }
        //            tmpTotalBytesRead += count;
        //        }


        //        this._TotalBytesRead = tmpTotalBytesRead;
        //        return (Int32)(~_RunningCrc32Result);
        //    }
        //}


        /// <summary>
        /// Get the CRC32 for the given (word,byte) combo.  This is a computation
        /// defined by PKzip.
        /// </summary>
        /// <param name="W">The word to start with.</param>
        /// <param name="B">The byte to combine it with.</param>
        /// <returns>The CRC-ized result.</returns>
        public static Int32 ComputeCrc32(Int32 W, byte B)
        {
            return _InternalComputeCrc32((UInt32)W, B);
        }

        internal static Int32 _InternalComputeCrc32(UInt32 W, byte B)
        {
            return (Int32)(crc32Table[(W ^ B) & 0xFF] ^ (W >> 8));
        }

        /// <summary>
        /// Update the value for the running CRC32 using the given block of bytes.
        /// This is useful when using the CRC32() class in a Stream.
        /// </summary>
        /// <param name="block">block of bytes to slurp</param>
        /// <param name="offset">starting point in the block</param>
        /// <param name="count">how many bytes within the block to slurp</param>
        public void SlurpBlock(byte[] block, int offset, int count)
        {
            if (block == null)
            {
                throw new ZlibException("The data buffer must not be null.");
            }

           // UInt32 tmpRunningCRC32Result = this._RunningCrc32Result;
            for (int i = 0; i < count; i++)
            {
                 int x = offset + i;
                 _RunningCrc32Result = ((_RunningCrc32Result) >> 8) ^ crc32Table[(block[x]) ^ ((_RunningCrc32Result) & 0x000000FF)];
                //tmpRunningCRC32Result = ((tmpRunningCRC32Result) >> 8) ^ crc32Table[(block[offset + i]) ^ ((tmpRunningCRC32Result) & 0x000000FF)];
            }

            this._TotalBytesRead += count;
            //this._RunningCrc32Result = tmpRunningCRC32Result;
        }


        // pre-initialize the crc table for speed of lookup.
        static CRC32Calculator()
        {
            unchecked
            {
                // PKZip specifies CRC32 with a polynomial of 0xEDB88320;
                // This is also the CRC-32 polynomial used bby Ethernet, FDDI,
                // bzip2, gzip, and others.
                // Often the polynomial is shown reversed as 0x04C11DB7.
                // For more details, see http://en.wikipedia.org/wiki/Cyclic_redundancy_check
                UInt32 dwPolynomial = 0xEDB88320;


                crc32Table = new UInt32[256];
                UInt32 dwCrc;
                for (uint i = 0; i < 256; i++)
                {
                    dwCrc = i;
                    for (uint j = 8; j > 0; j--)
                    {
                        if ((dwCrc & 1) == 1)
                        {
                            dwCrc = (dwCrc >> 1) ^ dwPolynomial;
                        }
                        else
                        {
                            dwCrc >>= 1;
                        }
                    }
                    crc32Table[i] = dwCrc;
                }
            }
        }




        static uint gf2_matrix_times(uint[] matrix, uint vec)
        {
            uint sum = 0;
            int i = 0;
            while (vec != 0)
            {
                if ((vec & 0x01) == 0x01)
                {
                    sum ^= matrix[i];
                }
                vec >>= 1;
                i++;
            }
            return sum;
        }

        static void gf2_matrix_square(uint[] square, uint[] mat)
        {
            for (int i = 0; i < 32; i++)
            {
                square[i] = gf2_matrix_times(mat, mat[i]);
            }
        }



        /// <summary>
        /// Combines the given CRC32 value with the current running total.
        /// </summary>
        /// <remarks>
        /// This is useful when using a divide-and-conquer approach to calculating a CRC.
        /// Multiple threads can each calculate a CRC32 on a segment of the data, and then
        /// combine the individual CRC32 values at the end.
        /// </remarks>
        /// <param name="crc">the crc value to be combined with this one</param>
        /// <param name="length">the length of data the CRC value was calculated on</param>
        public void Combine(int crc, int length)
        {
            uint[] even = new uint[32];     // even-power-of-two zeros operator
            uint[] odd = new uint[32];      // odd-power-of-two zeros operator

            if (length == 0)
                return;

            uint crc1 = ~_RunningCrc32Result;
            uint crc2 = (uint)crc;

            // put operator for one zero bit in odd
            odd[0] = 0xEDB88320;  // the CRC-32 polynomial
            uint row = 1;
            for (int i = 1; i < 32; i++)
            {
                odd[i] = row;
                row <<= 1;
            }

            // put operator for two zero bits in even
            gf2_matrix_square(even, odd);

            // put operator for four zero bits in odd
            gf2_matrix_square(odd, even);

            uint len2 = (uint)length;

            // apply len2 zeros to crc1 (first square will put the operator for one
            // zero byte, eight zero bits, in even)
            do
            {
                // apply zeros operator for this bit of len2
                gf2_matrix_square(even, odd);

                if ((len2 & 1) == 1)
                    crc1 = gf2_matrix_times(even, crc1);
                len2 >>= 1;

                if (len2 == 0)
                    break;

                // another iteration of the loop with odd and even swapped
                gf2_matrix_square(odd, even);
                if ((len2 & 1) == 1)
                    crc1 = gf2_matrix_times(odd, crc1);
                len2 >>= 1;


            } while (len2 != 0);

            crc1 ^= crc2;

            _RunningCrc32Result = ~crc1;

            //return (int) crc1;
            return;
        }



        // private member vars
        private Int64 _TotalBytesRead;
        private UInt32 _RunningCrc32Result = 0xFFFFFFFF;

        private const int BUFFER_SIZE = 8192;
        private static readonly UInt32[] crc32Table;
    }




    ///// <summary>
    ///// A Stream that calculates a CRC32 (a checksum) on all bytes read,
    ///// or on all bytes written.
    ///// </summary>
    /////
    ///// <remarks>
    ///// <para>
    ///// This class can be used to verify the CRC of a ZipEntry when
    ///// reading from a stream, or to calculate a CRC when writing to a
    ///// stream.  The stream should be used to either read, or write, but
    ///// not both.  If you intermix reads and writes, the results are not
    ///// defined.
    ///// </para>
    /////
    ///// <para>
    ///// This class is intended primarily for use internally by the
    ///// DotNetZip library.
    ///// </para>
    ///// </remarks>
    //public class CrcCalculatorStream : System.IO.Stream, System.IDisposable
    //{
    //    private static readonly Int64 UnsetLengthLimit = -99;

    //    internal System.IO.Stream _innerStream;
    //    private CRC32 _Crc32;
    //    private Int64 _lengthLimit = -99;
    //    private bool _leaveOpen;

    //    /// <summary>
    //    /// Gets the total number of bytes run through the CRC32 calculator.
    //    /// </summary>
    //    ///
    //    /// <remarks>
    //    /// This is either the total number of bytes read, or the total number of bytes
    //    /// written, depending on the direction of this stream.
    //    /// </remarks>
    //    public Int64 TotalBytesSlurped
    //    {
    //        get { return _Crc32.TotalBytesRead; }
    //    }


    //    /// <summary>
    //    /// The default constructor.
    //    /// </summary>
    //    /// <remarks>
    //    /// Instances returned from this constructor will leave the underlying stream
    //    /// open upon Close().
    //    /// </remarks>
    //    /// <param name="stream">The underlying stream</param>
    //    public CrcCalculatorStream(System.IO.Stream stream)
    //        : this(true, CrcCalculatorStream.UnsetLengthLimit, stream)
    //    {
    //    }


    //    /// <summary>
    //    /// The constructor allows the caller to specify how to handle the underlying
    //    /// stream at close.
    //    /// </summary>
    //    /// <param name="stream">The underlying stream</param>
    //    /// <param name="leaveOpen">true to leave the underlying stream
    //    /// open upon close of the CrcCalculatorStream.; false otherwise.</param>
    //    public CrcCalculatorStream(System.IO.Stream stream, bool leaveOpen)
    //        : this(leaveOpen, CrcCalculatorStream.UnsetLengthLimit, stream)
    //    {
    //    }


    //    /// <summary>
    //    /// A constructor allowing the specification of the length of the stream to read.
    //    /// </summary>
    //    /// <remarks>
    //    /// Instances returned from this constructor will leave the underlying stream open
    //    /// upon Close().
    //    /// </remarks>
    //    /// <param name="stream">The underlying stream</param>
    //    /// <param name="length">The length of the stream to slurp</param>
    //    public CrcCalculatorStream(System.IO.Stream stream, Int64 length)
    //        : this(true, length, stream)
    //    {
    //        if (length < 0)
    //            throw new ArgumentException("length");
    //    }

    //    ///// <summary>
    //    ///// A constructor allowing the specification of the length of the stream to
    //    ///// read, as well as whether to keep the underlying stream open upon Close().
    //    ///// </summary>
    //    ///// <param name="stream">The underlying stream</param>
    //    ///// <param name="length">The length of the stream to slurp</param>
    //    ///// <param name="leaveOpen">true to leave the underlying stream
    //    ///// open upon close of the CrcCalculatorStream.; false otherwise.</param>
    //    //public CrcCalculatorStream(System.IO.Stream stream, Int64 length, bool leaveOpen)
    //    //    : this(leaveOpen, length, stream)
    //    //{
    //    //    if (length < 0)
    //    //        throw new ArgumentException("length");
    //    //}


    //    // This ctor is private - no validation is done here.  This is to allow the use
    //    // of a (specific) negative value for the _lengthLimit, to indicate that there
    //    // is no length set.  So we validate the length limit in those ctors that use an
    //    // explicit param, otherwise we don't validate, because it could be our special
    //    // value.
    //    private CrcCalculatorStream(bool leaveOpen, Int64 length, System.IO.Stream stream)
    //        : base()
    //    {
    //        _innerStream = stream;
    //        _Crc32 = new CRC32();
    //        _lengthLimit = length;
    //        _leaveOpen = leaveOpen;
    //    }

    //    /// <summary>
    //    /// Provides the current CRC for all blocks slurped in.
    //    /// </summary>
    //    public Int32 Crc
    //    {
    //        get { return _Crc32.Crc32Result; }
    //    }

    //    ///// <summary>
    //    ///// Indicates whether the underlying stream will be left open when the
    //    ///// CrcCalculatorStream is Closed.
    //    ///// </summary>
    //    //public bool LeaveOpen
    //    //{
    //    //    get { return _leaveOpen; }
    //    //    set { _leaveOpen = value; }
    //    //}

    //    /// <summary>
    //    /// Read from the stream
    //    /// </summary>
    //    /// <param name="buffer">the buffer to read</param>
    //    /// <param name="offset">the offset at which to start</param>
    //    /// <param name="count">the number of bytes to read</param>
    //    /// <returns>the number of bytes actually read</returns>
    //    public override int Read(byte[] buffer, int offset, int count)
    //    {
    //        int bytesToRead = count;

    //        // Need to limit the # of bytes returned, if the stream is intended to have
    //        // a definite length.  This is especially useful when returning a stream for
    //        // the uncompressed data directly to the application.  The app won't
    //        // necessarily read only the UncompressedSize number of bytes.  For example
    //        // wrapping the stream returned from OpenReader() into a StreadReader() and
    //        // calling ReadToEnd() on it, We can "over-read" the zip data and get a
    //        // corrupt string.  The length limits that, prevents that problem.

    //        if (_lengthLimit != CrcCalculatorStream.UnsetLengthLimit)
    //        {
    //            if (_Crc32.TotalBytesRead >= _lengthLimit) return 0; // EOF
    //            Int64 bytesRemaining = _lengthLimit - _Crc32.TotalBytesRead;
    //            if (bytesRemaining < count) bytesToRead = (int)bytesRemaining;
    //        }
    //        int n = _innerStream.Read(buffer, offset, bytesToRead);
    //        if (n > 0) _Crc32.SlurpBlock(buffer, offset, n);
    //        return n;
    //    }

    //    /// <summary>
    //    /// Write to the stream.
    //    /// </summary>
    //    /// <param name="buffer">the buffer from which to write</param>
    //    /// <param name="offset">the offset at which to start writing</param>
    //    /// <param name="count">the number of bytes to write</param>
    //    public override void Write(byte[] buffer, int offset, int count)
    //    {
    //        if (count > 0) _Crc32.SlurpBlock(buffer, offset, count);
    //        _innerStream.Write(buffer, offset, count);
    //    }

    //    /// <summary>
    //    /// Indicates whether the stream supports reading.
    //    /// </summary>
    //    public override bool CanRead
    //    {
    //        get { return _innerStream.CanRead; }
    //    }

    //    /// <summary>
    //    /// Indicates whether the stream supports seeking.
    //    /// </summary>
    //    public override bool CanSeek
    //    {
    //        get { return _innerStream.CanSeek; }
    //    }

    //    /// <summary>
    //    /// Indicates whether the stream supports writing.
    //    /// </summary>
    //    public override bool CanWrite
    //    {
    //        get { return _innerStream.CanWrite; }
    //    }

    //    /// <summary>
    //    /// Flush the stream.
    //    /// </summary>
    //    public override void Flush()
    //    {
    //        _innerStream.Flush();
    //    }

    //    /// <summary>
    //    /// Not implemented.
    //    /// </summary>
    //    public override long Length
    //    {
    //        get
    //        {
    //            if (_lengthLimit == CrcCalculatorStream.UnsetLengthLimit)
    //            {
    //                return _innerStream.Length;
    //            }
    //            else
    //            {
    //                return _lengthLimit;
    //            }
    //        }
    //    }

    //    /// <summary>
    //    /// Not implemented.
    //    /// </summary>
    //    public override long Position
    //    {
    //        get { return _Crc32.TotalBytesRead; }
    //        set { throw new NotImplementedException(); }
    //    }

    //    /// <summary>
    //    /// Not implemented.
    //    /// </summary>
    //    /// <param name="offset">N/A</param>
    //    /// <param name="origin">N/A</param>
    //    /// <returns>N/A</returns>
    //    public override long Seek(long offset, System.IO.SeekOrigin origin)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    /// <summary>
    //    /// Not implemented.
    //    /// </summary>
    //    /// <param name="value">N/A</param>
    //    public override void SetLength(long value)
    //    {
    //        throw new NotImplementedException();
    //    }


    //    void IDisposable.Dispose()
    //    {
    //        Close();
    //    }

    //    /// <summary>
    //    /// Closes the stream.
    //    /// </summary>
    //    public override void Close()
    //    {
    //        base.Close();
    //        if (!_leaveOpen)
    //            _innerStream.Close();
    //    }

    //}
}
