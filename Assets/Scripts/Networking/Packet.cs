using System.IO;
using System.Net;

namespace Networking
{
	public class Packet : GenericPoolableObject {

		private static readonly GenericPool<Packet> Pool = new GenericPool<Packet>();
		private static readonly System.Object PoolLock = new System.Object();

		private const int BufferCapacity = 1024 * 1024;

		public readonly BitBuffer Buffer = new BitBuffer(new MemoryStream(BufferCapacity));
		public IPEndPoint FromEndPoint;

		public static Packet Obtain() {
			Packet packet = null;
			lock (PoolLock) {
				packet = Pool.Obtain();
			}
			return packet;
		}

		public void Reset() {
			Buffer.Clear();
		}

		public void Free() {
			lock (PoolLock) {
				Pool.Free(this);
			}
		}
	}
}