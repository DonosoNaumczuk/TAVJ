using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace Commons.Networking
{
	public class BitBuffer {

		private readonly MemoryStream _buffer;
		private int _availableByteCount;
		private long _bits;
		private int _currentBitCount;

		private static readonly byte[] TempArray = new byte[4];

		public BitBuffer(MemoryStream buffer) {
			_buffer = buffer;
			_availableByteCount = buffer.Capacity;
			buffer.SetLength(buffer.Capacity);
		}

		public MemoryStream GetBuffer() {
			return _buffer;
		}

		public int GetAvailableByteCount() {
			return _availableByteCount;
		}

		public void SetAvailableByteCount(int availableByteCount) {
			this._availableByteCount = availableByteCount;
		}

		public int GetCurrentBitCount() {
			return _currentBitCount;
		}

		private static int GetBitsRequired(long value) {
			int bitsRequired = 0;
			while (value > 0) {
				bitsRequired++;
				value >>= 1;
			}
			return bitsRequired;
		}

		public void PutBit(bool value) {
			long longValue = value ? 1L : 0L;
			_bits |= longValue << _currentBitCount;
			_currentBitCount++;
			WriteIfNecessary();
		}

		private void PutBitsInternal(long value, int bitCount) {
			if (bitCount < 1 || bitCount > 32) {
				throw new ArgumentException(bitCount + " should be >= 1 and <= 32");
			}
			if (value < 0 || value >= (1L << bitCount)) {
				throw new ArgumentException(value + " out of range for " + bitCount + " bits");
			}
			_bits |= value << _currentBitCount;
			_currentBitCount += bitCount;
			WriteIfNecessary();
		}

		public void PutBits(int value, int minValue, int maxValue) {
			if (value < minValue || value > maxValue) {
				throw new ArgumentException(value + " should be >= " + minValue + " and <= " + maxValue);
			}
			if (minValue >= maxValue) {
				throw new ArgumentException(minValue + " should be < " + maxValue);
			}
			long diff = (long) value - (long) minValue;
			long range = (long) maxValue - (long) minValue;
			int bitsRequired = GetBitsRequired(range);
			PutBitsInternal(diff, bitsRequired);
		}

		public void PutByte(int value) {
			// use sbyte to match java signed byte behavior
			PutBits(value, sbyte.MinValue, sbyte.MaxValue);
		}

		public void PutInt(int value) {
			PutBits(value, int.MinValue, int.MaxValue);
		}
		
		public void PutLong(long value) {
			PutBits((int) ((value >> 32) & 0x00000000ffffffffL), int.MinValue, int.MaxValue);
			PutBits((int) (value & 0x00000000ffffffffL), int.MinValue, int.MaxValue);
		}

		public void PutFloat(float value) {
			byte[] bytes = BitConverter.GetBytes(value);
			PutBits(bytes[0], byte.MinValue, byte.MaxValue);
			PutBits(bytes[1], byte.MinValue, byte.MaxValue);
			PutBits(bytes[2], byte.MinValue, byte.MaxValue);
			PutBits(bytes[3], byte.MinValue, byte.MaxValue);
		}

		public void PutQuantizedFloat(float value, float minValue, float maxValue, float resolution) {
			if (value < minValue || value > maxValue) {
				throw new ArgumentException(value + " should be >= " + minValue + " and <= " + maxValue);
			}
			if (minValue >= maxValue) {
				throw new ArgumentException(minValue + " should be < " + maxValue);
			}
			long longValue = (long) ((value - minValue) / resolution);
			float range = maxValue - minValue;
			long valueCount = (long) (range / resolution);
			int bitsRequired = GetBitsRequired(valueCount);
			PutBitsInternal(longValue, bitsRequired);
		}

		public void PutString(string str) {
			byte[] bytes = Encoding.UTF8.GetBytes(str);
			if (bytes.Length > 0xffff) {
				throw new ArgumentException("String length should be <= 0xffff");
			}
			PutBits(bytes.Length, 0, 0xffff);
			foreach (byte b in bytes) {
				PutByte(b);
			}
		}

		public void PutEnum(Enum enumValue, int enumCount) {
			PutBits(Convert.ToInt32(enumValue), 0, Mathf.Max(1, enumCount - 1));
		}

		void WritePadding() {
			PutBitsInternal(0, 32 - _currentBitCount);
		}

		private void WriteIfNecessary() {
			if (_currentBitCount >= 32) {
				if (_buffer.Position + 4 > _buffer.Capacity) {
					throw new InvalidOperationException("write buffer overflow");
				}
				int word = (int) _bits;
				byte a = (byte) (word);
				byte b = (byte) (word >> 8);
				byte c = (byte) (word >> 16);
				byte d = (byte) (word >> 24);
				_buffer.WriteByte(d);
				_buffer.WriteByte(c);
				_buffer.WriteByte(b);
				_buffer.WriteByte(a);
				_bits >>= 32;
				_currentBitCount -= 32;
			}
		}

		public bool GetBit() {
			ReadIfNecessary(1);
			bool value = (_bits & 1L) == 1;
			_bits >>= 1;
			_currentBitCount--;
			return value;
		}

		private long GetBitsInternal(int bitCount) { 
			ReadIfNecessary(bitCount);
			long mask = (1L << bitCount) - 1;
			long value = (long) (_bits & mask);
			_bits >>= bitCount;
			_currentBitCount -= bitCount;
			return value;
		}

		public int GetBits(int minValue, int maxValue) {
			if (minValue >= maxValue) {
				throw new ArgumentException(minValue + " should be < " + maxValue);
			}
			long range = (long) maxValue - (long) minValue;
			int bitsRequired = GetBitsRequired(range);
			long value = GetBitsInternal(bitsRequired);
			return (int) (minValue + value);
		}

		public int GetByte() {
			// use sbyte to match java signed byte behavior
			return (int)GetBits(sbyte.MinValue, sbyte.MaxValue);
		}

		public int GetInt() {
			return (int)GetBits(int.MinValue, int.MaxValue);
		}

		public long GetLong() {
			long leftValue = GetBits(int.MinValue, int.MaxValue);
			long rightValue = GetBits(int.MinValue, int.MaxValue);
			return (leftValue << 32) | rightValue;
		}

		public float GetFloat() {
			long value = GetBitsInternal(32);
			byte[] result = UIntToByteArray((uint)value);
			return BitConverter.ToSingle(result, 0);
		}

		public float GetAngle() {
			// convert angle from bullet to unity
			return GetFloat() - 90;
		}

		public float GetQuantizedFloat(float minValue, float maxValue, float resolution) {
			if (minValue >= maxValue) {
				throw new ArgumentException(minValue + " should be < " + maxValue);
			}
			float range = maxValue - minValue;
			long valueCount = (long) (range / resolution);
			int bitsRequired = GetBitsRequired(valueCount);
			long longValue = GetBitsInternal(bitsRequired);
			return minValue + longValue * resolution;
		}

		public float GetSnappedQuantizedFloat(float minValue, float maxValue, float resolution) {
			if (minValue >= maxValue) {
				throw new ArgumentException(minValue + " should be < " + maxValue);
			}
			float range = maxValue - minValue;
			long valueCount = (long) (range / resolution);
			int bitsRequired = GetBitsRequired(valueCount);
			long longValue = GetBitsInternal(bitsRequired);
			return longValue == valueCount ? maxValue : minValue + longValue * resolution;
		}

		public float GetQuantizedAngle(float minValue, float maxValue, float resolution) {
			// convert angle from bullet to unity
			return GetQuantizedFloat(minValue, maxValue, resolution) - 90;
		}

		public string GetString() {
			byte[] bytes = new byte[GetBits(0, 0xffff)];
			for (int i = 0; i < bytes.Length; i++) {
				bytes[i] = (byte) GetByte();
			}
			return Encoding.UTF8.GetString(bytes);
		}

		public T GetEnum<T>(int enumCount) where T : struct, IConvertible {
			return (T) (object) GetBits(0, Mathf.Max(1, enumCount - 1));
		}

		public void ReadPadding() {
			GetBitsInternal(_currentBitCount);
		}

		public void CopyTo(BitBuffer dest, int byteCount) {
			if (_currentBitCount != 0) {
				throw new InvalidOperationException("Source buffer must be aligned");
			}
			if (dest._currentBitCount != 0) {
				throw new InvalidOperationException("Dest buffer must be aligned");
			}
			_buffer.Read(dest._buffer.GetBuffer(), (int) dest._buffer.Position, byteCount);
			dest._buffer.Position += byteCount;
		}

		private void ReadIfNecessary(int bitCount) {
			if (_currentBitCount < bitCount) {
				if (_buffer.Position + 4 > _availableByteCount) {
					throw new InvalidOperationException("read buffer overflow");
				}
				byte a = (byte) _buffer.ReadByte();
				byte b = (byte) _buffer.ReadByte();
				byte c = (byte) _buffer.ReadByte();
				byte d = (byte) _buffer.ReadByte();
				long word = 0;
				word |= (uint) (a << 24);
				word |= (uint) (b << 16);
				word |= (uint) (c << 8);
				word |= (uint) d;
				_bits |= word << _currentBitCount;
				_currentBitCount += 32;
			}
		}

		public void Flush() {
			if (_currentBitCount > 0) {
				int word = (int) _bits;
				byte a = (byte) (word);
				byte b = (byte) (word >> 8);
				byte c = (byte) (word >> 16);
				byte d = (byte) (word >> 24);
				_buffer.WriteByte(d);
				_buffer.WriteByte(c);
				_buffer.WriteByte(b);
				_buffer.WriteByte(a);
			}
			_availableByteCount = (int) _buffer.Position;
			_buffer.Position = 0;
			_bits = 0;
			_currentBitCount = 0;
		}

		public void Clear() {
			_availableByteCount = 0;
			_buffer.Position = 0;
			_bits = 0;
			_currentBitCount = 0;
		}

		public bool HasRemaining() {
			return _buffer.Position < _availableByteCount || _currentBitCount > 0;
		}

		private static byte[] UIntToByteArray(uint data) {
			TempArray[0] = (byte) (data & 0x000000ff);
			TempArray[1] = (byte) ((data & 0x0000ff00) >> 8);
			TempArray[2] = (byte) ((data & 0x00ff0000) >> 16);
			TempArray[3] = (byte) ((data & 0xff000000) >> 24);
			return TempArray;
		}
	}
}

