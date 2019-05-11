﻿namespace EhwarSoft.AvroConvert.Write
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using Constants;
    using Exceptions;
    using Schema;

    public class Encoder
    {
        private Schema _schema;
        private Codec _codec;
        private Stream _stream;
        private MemoryStream _blockStream;
        private IWriter _encoder, _blockEncoder;
        private AbstractEncoder _writer;

        private byte[] _syncData;
        private bool _isOpen;
        private bool _headerWritten;
        private int _blockCount;
        private int _syncInterval;
        private IDictionary<string, byte[]> _metaData;


        public static Encoder OpenWriter(AbstractEncoder writer, string path)
        {
            return OpenWriter(writer, new FileStream(path, FileMode.Create), Codec.CreateCodec(Codec.Type.Null));
        }

        public static Encoder OpenWriter(AbstractEncoder writer, Stream outStream)
        {
            return OpenWriter(writer, outStream, Codec.CreateCodec(Codec.Type.Null));
        }


        public static Encoder OpenWriter(AbstractEncoder writer, string path, Codec codec)
        {
            return OpenWriter(writer, new FileStream(path, FileMode.Create), codec);
        }

        /// <summary>
        /// Open a new writer instance to write
        /// to an output stream with a specified codec
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="outStream"></param>
        /// <param name="codec"></param>
        /// <returns></returns>
        public static Encoder OpenWriter(AbstractEncoder writer, Stream outStream, Codec codec)
        {
            return new Encoder(writer).Create(writer._schema, outStream, codec);
        }

        Encoder(AbstractEncoder writer)
        {
            _writer = writer;
            _syncInterval = DataFileConstants.DefaultSyncInterval;
        }

        public bool IsReservedMeta(string key)
        {
            return key.StartsWith(DataFileConstants.MetaDataReserved);
        }

        public void SetMeta(String key, byte[] value)
        {
            if (IsReservedMeta(key))
            {
                throw new AvroRuntimeException("Cannot set reserved meta key: " + key);
            }
            _metaData.Add(key, value);
        }

        public void SetMeta(String key, long value)
        {
            try
            {
                SetMeta(key, GetByteValue(value.ToString(CultureInfo.InvariantCulture)));
            }
            catch (Exception e)
            {
                throw new AvroRuntimeException(e.Message, e);
            }
        }

        public void SetMeta(String key, string value)
        {
            try
            {
                SetMeta(key, GetByteValue(value));
            }
            catch (Exception e)
            {
                throw new AvroRuntimeException(e.Message, e);
            }
        }

        public void SetSyncInterval(int syncInterval)
        {
            if (syncInterval < 32 || syncInterval > (1 << 30))
            {
                throw new AvroRuntimeException("Invalid sync interval value: " + syncInterval);
            }
            _syncInterval = syncInterval;
        }

        public void Append(object datum)
        {
            AssertOpen();
            EnsureHeader();

            long usedBuffer = _blockStream.Position;

            try
            {
                _writer.Write(datum, _blockEncoder);
            }
            catch (Exception e)
            {
                _blockStream.Position = usedBuffer;
                throw new AvroRuntimeException("Error appending datum to writer", e);
            }
            _blockCount++;
            WriteIfBlockFull();
        }

        private void EnsureHeader()
        {
            if (!_headerWritten)
            {
                WriteHeader();
                _headerWritten = true;
            }
        }

        public void Flush()
        {
            EnsureHeader();
            Sync();
        }

        public long Sync()
        {
            AssertOpen();
            WriteBlock();
            return _stream.Position;
        }

        public void Close()
        {
            EnsureHeader();
            Flush();
            _stream.Flush();
            _stream.Dispose();
            _isOpen = false;
        }

        private void WriteHeader()
        {
            _encoder.WriteFixed(DataFileConstants.AvroHeader);
            WriteMetaData();
            WriteSyncData();
        }

        private void Init()
        {
            _blockCount = 0;
            _encoder = new Writer(_stream);
            _blockStream = new MemoryStream();
            _blockEncoder = new Writer(_blockStream);

            if (_codec == null)
                _codec = Codec.CreateCodec(Codec.Type.Null);

            _isOpen = true;
        }

        private void AssertOpen()
        {
            if (!_isOpen) throw new AvroRuntimeException("Cannot complete operation: avro file/stream not open");
        }

        private Encoder Create(Schema schema, Stream outStream, Codec codec)
        {
            _codec = codec;
            _stream = outStream;
            _metaData = new Dictionary<string, byte[]>();
            _schema = schema;

            Init();

            return this;
        }

        private void WriteMetaData()
        {
            // Add sync, code & schema to metadata
            GenerateSyncData();
            //SetMetaInternal(DataFileConstants.MetaDataSync, _syncData); - Avro 1.5.4 C
            SetMetaInternal(DataFileConstants.CodecMetadataKey, GetByteValue(_codec.GetName()));
            SetMetaInternal(DataFileConstants.SchemaMetadataKey, GetByteValue(_schema.ToString()));

            // write metadata 
            int size = _metaData.Count;
            _encoder.WriteInt(size);

            foreach (KeyValuePair<String, byte[]> metaPair in _metaData)
            {
                _encoder.WriteString(metaPair.Key);
                _encoder.WriteBytes(metaPair.Value);
            }
            _encoder.WriteMapEnd();
        }

        private void WriteIfBlockFull()
        {
            if (BufferInUse() >= _syncInterval)
                WriteBlock();
        }

        private long BufferInUse()
        {
            return _blockStream.Position;
        }

        private void WriteBlock()
        {
            if (_blockCount > 0)
            {
                byte[] dataToWrite = _blockStream.ToArray();

                // write count 
                _encoder.WriteLong(_blockCount);

                // write data 
                _encoder.WriteBytes(_codec.Compress(dataToWrite));

                // write sync marker 
                _encoder.WriteFixed(_syncData);

                // reset / re-init block
                _blockCount = 0;
                _blockStream = new MemoryStream();
                _blockEncoder = new Writer(_blockStream);
            }
        }

        private void WriteSyncData()
        {
            _encoder.WriteFixed(_syncData);
        }

        private void GenerateSyncData()
        {
            _syncData = new byte[16];

            Random random = new Random();
            random.NextBytes(_syncData);
        }

        private void SetMetaInternal(string key, byte[] value)
        {
            _metaData.Add(key, value);
        }

        private byte[] GetByteValue(string value)
        {
            return System.Text.Encoding.UTF8.GetBytes(value);
        }

        public void Dispose()
        {
            Close();
        }
    }
}